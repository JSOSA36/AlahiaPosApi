using System.Globalization;
using System.IO.Compression;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;
using AlahiaBackup.Configuration;
using AlahiaBackup.Interfaces;
using AlahiaBackup.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace AlahiaBackup.Services
{
    public sealed class SqlBackupService : ISqlBackupService
    {
        private static readonly Regex NombreSeguro = new("^[A-Za-z0-9_]+$", RegexOptions.Compiled);

        private readonly ILogger<SqlBackupService> _logger;
        private readonly BackupSettings _settings;
        private readonly string _connectionString;

        public SqlBackupService(
            ILogger<SqlBackupService> logger,
            IOptions<BackupSettings> settings,
            IConfiguration configuration)
        {
            _logger = logger;
            _settings = settings.Value;
            _connectionString = configuration.GetConnectionString("SqlServer")
                ?? throw new InvalidOperationException("Falta ConnectionStrings:SqlServer.");
        }

        public async Task<BackupArtifact> RealizarBackupAsync(
            string baseDeDatos,
            DateOnly fecha,
            CancellationToken cancellationToken)
        {
            if (!NombreSeguro.IsMatch(baseDeDatos))
                throw new InvalidOperationException($"Nombre de base no permitido: {baseDeDatos}");

            Directory.CreateDirectory(_settings.RutaBackup);
            ConcederAccesoSql(_settings.RutaBackup);

            var carpeta = Path.Combine(_settings.RutaBackup, baseDeDatos, fecha.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(carpeta);
            ConcederAccesoSql(carpeta);

            var bakPath = Path.Combine(carpeta, $"{baseDeDatos}_{fecha:yyyyMMdd}.bak");
            var archivoFinal = _settings.Comprimir
                ? Path.Combine(carpeta, $"{baseDeDatos}_{fecha:yyyyMMdd}.zip")
                : bakPath;

            if (File.Exists(archivoFinal) && new FileInfo(archivoFinal).Length > 0)
            {
                _logger.LogInformation("Ya existe el respaldo de {Base} para {Fecha}: {Archivo}", baseDeDatos, fecha, archivoFinal);
                return await ArtefactoAsync(baseDeDatos, archivoFinal, cancellationToken);
            }

            if (File.Exists(bakPath))
                File.Delete(bakPath);

            _logger.LogInformation("Iniciando BACKUP DATABASE {Base} hacia {Archivo}", baseDeDatos, bakPath);

            await using var conn = new SqlConnection(_connectionString);
            conn.InfoMessage += (_, e) => _logger.LogInformation("SQL: {Mensaje}", e.Message);
            await conn.OpenAsync(cancellationToken);

            await using var cmd = conn.CreateCommand();
            cmd.CommandTimeout = 0;
            cmd.CommandText = $"BACKUP DATABASE [{baseDeDatos}] TO DISK = @path WITH INIT, CHECKSUM, STATS = 10";
            cmd.Parameters.Add(new SqlParameter("@path", System.Data.SqlDbType.NVarChar, 400) { Value = bakPath });
            await cmd.ExecuteNonQueryAsync(cancellationToken);

            var bak = new FileInfo(bakPath);
            if (!bak.Exists || bak.Length == 0)
                throw new InvalidOperationException($"SQL no dejó el archivo de respaldo: {bakPath}");

            _logger.LogInformation("Respaldo SQL listo: {Archivo} ({Mb:N1} MB)", bakPath, bak.Length / 1024d / 1024d);

            if (!_settings.Comprimir)
                return await ArtefactoAsync(baseDeDatos, bakPath, cancellationToken);

            if (File.Exists(archivoFinal))
                File.Delete(archivoFinal);

            _logger.LogInformation("Comprimiendo {Bak}", bakPath);
            using (var zip = ZipFile.Open(archivoFinal, ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(bakPath, Path.GetFileName(bakPath), CompressionLevel.Optimal);
            }

            File.Delete(bakPath);
            return await ArtefactoAsync(baseDeDatos, archivoFinal, cancellationToken);
        }

        public void EliminarAntiguos(int diasRetencion)
        {
            if (diasRetencion < 1 || !Directory.Exists(_settings.RutaBackup))
                return;

            var limite = DateOnly.FromDateTime(DateTime.Today).AddDays(-diasRetencion);
            foreach (var baseDir in Directory.GetDirectories(_settings.RutaBackup))
            {
                if (string.Equals(Path.GetFileName(baseDir), "_state", StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var diaDir in Directory.GetDirectories(baseDir))
                {
                    var nombre = Path.GetFileName(diaDir);
                    if (!DateOnly.TryParseExact(nombre, "yyyy-MM-dd", CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var fecha))
                        continue;
                    if (fecha >= limite)
                        continue;

                    Directory.Delete(diaDir, recursive: true);
                    _logger.LogInformation("Eliminado respaldo antiguo {Carpeta}", diaDir);
                }
            }
        }

        private static async Task<BackupArtifact> ArtefactoAsync(
            string baseDeDatos,
            string archivo,
            CancellationToken cancellationToken)
        {
            var info = new FileInfo(archivo);
            await using var stream = new FileStream(archivo, FileMode.Open, FileAccess.Read, FileShare.Read);
            var hash = await System.Security.Cryptography.SHA256.HashDataAsync(stream, cancellationToken);
            return new BackupArtifact(baseDeDatos, archivo, info.Length, Convert.ToHexString(hash));
        }

        private void ConcederAccesoSql(string carpeta)
        {
            if (!OperatingSystem.IsWindows())
                return;

            try
            {
                var info = new DirectoryInfo(carpeta);
                var security = info.GetAccessControl();
                var herencia = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
                security.AddAccessRule(new FileSystemAccessRule(
                    new NTAccount(@"NT SERVICE\MSSQL$SQLEXPRESS"),
                    FileSystemRights.Modify,
                    herencia,
                    PropagationFlags.None,
                    AccessControlType.Allow));
                info.SetAccessControl(security);
            }
            catch (Exception ex) when (ex is IdentityNotMappedException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                _logger.LogWarning(ex, "No se pudo dar permiso de escritura a SQL Server en {Carpeta}. El BACKUP puede fallar si la cuenta del servicio no puede escribir ahí.", carpeta);
            }
        }
    }
}
