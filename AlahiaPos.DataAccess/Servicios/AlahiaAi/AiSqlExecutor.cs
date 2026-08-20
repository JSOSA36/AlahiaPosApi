using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using AlahiaPos.Entities.Dto.AlahiaAi;
using AlahiaPos.Entities.Interfaces.AlahiaAi;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlahiaPos.DataAccess.Servicios.AlahiaAi
{
    public class AiSqlOptions
    {
        public bool Enabled { get; set; }
        public string ConnectionString { get; set; } = string.Empty;
        public int CommandTimeoutSeconds { get; set; } = 15;
        public int MaxRows { get; set; } = 200;
    }

    public class AiSqlExecutor : IAiSqlExecutor
    {
        private static readonly Regex Dangerous = new(
            @"\b(INSERT|UPDATE|DELETE|MERGE|DROP|ALTER|CREATE|TRUNCATE|EXEC|EXECUTE|GRANT|REVOKE|DENY|BACKUP|RESTORE|SHUTDOWN|XP_|SP_OA|OPENROWSET|OPENDATASOURCE|INTO\s+#|INTO\s+\[?#)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex OnlySelect = new(
            @"^\s*(WITH\b[\s\S]+?\bSELECT\b|SELECT\b)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private readonly AiSqlOptions _opt;
        private readonly ILogger<AiSqlExecutor> _log;

        public AiSqlExecutor(IOptions<AlahiaAiOptions> options, ILogger<AiSqlExecutor> log)
        {
            _opt = options.Value.Sql ?? new AiSqlOptions();
            _log = log;
        }

        public bool IsEnabled =>
            _opt.Enabled && !string.IsNullOrWhiteSpace(_opt.ConnectionString);

        public async Task<string> GetCatalogAsync(CancellationToken ct = default)
        {
            if (!IsEnabled) return "[]";
            // Catálogo no requiere IdEmpresa; usamos 0 y la vista v_Catalogo no filtra por empresa
            var result = await ExecuteAsync(
                idEmpresa: 0,
                sql: "SELECT Vista, Descripcion FROM ai.v_Catalogo ORDER BY Vista",
                requireTenantContext: false,
                ct);
            return result.Success ? result.JsonRows : "[]";
        }

        public async Task<AiSqlExecutionResult> ExecuteAsync(
            int idEmpresa,
            string sql,
            bool requireTenantContext = true,
            CancellationToken ct = default)
        {
            var result = new AiSqlExecutionResult();
            if (!IsEnabled)
            {
                result.Error = "SQL tools deshabilitados.";
                return result;
            }

            if (idEmpresa <= 0 && requireTenantContext)
            {
                result.Error = "IdEmpresa inválido.";
                return result;
            }

            var validationError = ValidateSelectOnly(sql);
            if (validationError != null)
            {
                result.Error = validationError;
                return result;
            }

            try
            {
                await using var conn = new SqlConnection(_opt.ConnectionString);
                await conn.OpenAsync(ct);

                if (requireTenantContext)
                {
                    await using (var ctx = conn.CreateCommand())
                    {
                        ctx.CommandText = "EXEC sp_set_session_context @key=N'IdEmpresa', @value=@id, @read_only=1;";
                        ctx.Parameters.AddWithValue("@id", idEmpresa);
                        ctx.CommandTimeout = _opt.CommandTimeoutSeconds;
                        await ctx.ExecuteNonQueryAsync(ct);
                    }
                }

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.CommandTimeout = _opt.CommandTimeoutSeconds;
                cmd.CommandType = CommandType.Text;

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                var rows = new List<Dictionary<string, object?>>();
                var max = Math.Max(1, _opt.MaxRows);
                while (await reader.ReadAsync(ct) && rows.Count < max)
                {
                    var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        var name = reader.GetName(i);
                        var val = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        if (val is DateTime dt) val = dt.ToString("o");
                        if (val is byte[] bytes) val = Convert.ToBase64String(bytes);
                        row[name] = val;
                    }
                    rows.Add(row);
                }

                result.Success = true;
                result.RowCount = rows.Count;
                result.Truncated = !reader.IsClosed && await reader.ReadAsync(ct);
                result.JsonRows = JsonSerializer.Serialize(rows);
                result.SqlExecuted = sql;
                return result;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "AiSqlExecutor falló IdEmpresa={IdEmpresa}", idEmpresa);
                result.Error = "No se pudo ejecutar la consulta de solo lectura.";
                result.Detail = ex.Message;
                return result;
            }
        }

        public static string? ValidateSelectOnly(string? sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return "SQL vacío.";

            var text = sql.Trim().TrimEnd(';');
            if (text.Contains(';'))
                return "Solo se permite una sentencia SQL.";

            if (!OnlySelect.IsMatch(text))
                return "Solo se permiten consultas SELECT.";

            if (Dangerous.IsMatch(text))
                return "La consulta contiene operaciones no permitidas.";

            // Forzar que consulte schema ai (evita dbo aunque tuviera permiso)
            if (!Regex.IsMatch(text, @"\bai\s*\.\s*v_", RegexOptions.IgnoreCase))
                return "Solo se permiten vistas del schema ai (ej. ai.v_FacturaHeaders).";

            return null;
        }
    }
}
