using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Dto.Fiscal;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios.FacturacionElectronica
{
    public static class EcfXmlStore
    {
        public static async Task GuardarAsync(
            AlahiaPosContext ctx,
            int idEcf,
            FiscalEnvioResultado resultado,
            CancellationToken ct = default)
        {
            if (idEcf <= 0 || resultado == null) return;
            if (string.IsNullOrWhiteSpace(resultado.XmlFirmado)
                && string.IsNullOrWhiteSpace(resultado.XmlSinFirmar))
                return;

            var sin = resultado.XmlSinFirmar ?? "";
            var firmado = resultado.XmlFirmado;

            await ctx.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE dbo.ECFXml
SET XmlSinFirmar = COALESCE({sin}, XmlSinFirmar),
    XmlFirmado = COALESCE({firmado}, XmlFirmado)
WHERE IdECF = {idEcf};
IF @@ROWCOUNT = 0
INSERT INTO dbo.ECFXml (IdECF, XmlSinFirmar, XmlFirmado, FechaGeneracion)
VALUES ({idEcf}, {sin}, {firmado}, GETDATE());
", ct);
        }

        public static async Task<bool> TieneFirmadoAsync(AlahiaPosContext ctx, int idEcf, CancellationToken ct = default)
        {
            var conn = ctx.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ECFXml WHERE IdECF = @id AND NULLIF(XmlFirmado, N'') IS NOT NULL) THEN 1 ELSE 0 END";
            var p = cmd.CreateParameter();
            p.ParameterName = "@id";
            p.Value = idEcf;
            cmd.Parameters.Add(p);
            var result = await cmd.ExecuteScalarAsync(ct);
            return result != null && result != System.DBNull.Value && System.Convert.ToInt32(result) == 1;
        }

        public static string? TextoDgii(FiscalEnvioResultado resultado, bool aceptado)
        {
            var texto = resultado.Mensajes == null
                ? null
                : string.Join("; ", resultado.Mensajes.Where(m => !string.IsNullOrWhiteSpace(m)));
            if (!string.IsNullOrWhiteSpace(texto))
                return texto;
            if (aceptado || string.IsNullOrWhiteSpace(resultado.XmlRespuesta))
                return null;
            var body = resultado.XmlRespuesta.Trim();
            return body.Length > 2000 ? body.Substring(0, 2000) : body;
        }
    }
}
