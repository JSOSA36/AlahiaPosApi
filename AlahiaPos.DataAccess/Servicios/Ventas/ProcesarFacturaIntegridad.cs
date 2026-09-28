using AlahiaPos.DataAccess.Data;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios.Ventas
{
    /// <summary>
    /// Consultas de integridad para ProcesarFactura.
    /// No cubre la transmisión HTTP a DGII: solo persistencia local + outbox.
    /// </summary>
    public static class ProcesarFacturaIntegridad
    {
        public static string ClaveTesoreria(int idFactura, string metodo)
            => $"VENTA-{idFactura}-{metodo}";

        public static string ReferenciaInventario(int idFactura)
            => $"Factura #{idFactura}";

        public static string NormalizarClaveIdempotencia(string? clave)
            => string.IsNullOrWhiteSpace(clave) ? "" : clave.Trim();

        /// <summary>
        /// Una factura tipo 1 no cancelada ya se considera venta confirmada:
        /// reintentos no deben volver a cobrar, descontar stock ni mover tesorería.
        /// </summary>
        public static async Task<bool> VentaYaConfirmadaAsync(
            AlahiaPosContext ctx,
            int idFactura,
            CancellationToken cancellationToken = default)
        {
            if (idFactura <= 0 || ctx == null)
                return false;

            var header = await ctx.FacturaHeaders.AsNoTracking()
                .FirstOrDefaultAsync(
                    h => h.IdFacturaHeader == idFactura,
                    cancellationToken);

            if (header == null)
                return false;

            if (header.EstaCancelada)
                return false;

            return header.IdTipoDocumentos == 1;
        }

        public static async Task<int?> BuscarFacturaPorClaveIdempotenciaAsync(
            AlahiaPosContext ctx,
            int idEmpresa,
            string clave,
            CancellationToken cancellationToken = default)
        {
            clave = NormalizarClaveIdempotencia(clave);
            if (ctx == null || idEmpresa <= 0 || clave.Length == 0)
                return null;

            var id = await ctx.FacturaHeaders.AsNoTracking()
                .Where(h => h.IdEmpresa == idEmpresa
                    && h.ClaveIdempotenciaVenta == clave
                    && !h.EstaCancelada)
                .Select(h => (int?)h.IdFacturaHeader)
                .FirstOrDefaultAsync(cancellationToken);

            return id;
        }

        public static async Task BloquearFacturaAsync(
            AlahiaPosContext ctx,
            int idFactura,
            CancellationToken cancellationToken = default)
        {
            if (ctx == null || idFactura <= 0)
                return;

            await ctx.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT IdFacturaHeader FROM FacturaHeaders WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE IdFacturaHeader = {idFactura}",
                cancellationToken);
        }
    }
}
