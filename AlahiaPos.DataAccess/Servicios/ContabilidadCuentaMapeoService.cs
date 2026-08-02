using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    public class ContabilidadCuentaMapeoService : IContabilidadCuentaMapeoService
    {
        private readonly AlahiaPosContext _context;

        public ContabilidadCuentaMapeoService(AlahiaPosContext context)
        {
            _context = context;
        }

        public async Task<int?> ResolverAsync(int idEmpresa, string codigoConcepto)
        {
            if (string.IsNullOrWhiteSpace(codigoConcepto))
                return null;

            var mapeo = await _context.ContabilidadCuentaMapeo
                .AsNoTracking()
                .FirstOrDefaultAsync(m =>
                    m.IdEmpresa == idEmpresa
                    && m.CodigoConcepto == codigoConcepto
                    && m.Activo);

            return mapeo?.IdCuentaContable;
        }

        public async Task<int> ResolverRequeridoAsync(int idEmpresa, string codigoConcepto)
        {
            var id = await ResolverAsync(idEmpresa, codigoConcepto);
            if (!id.HasValue || id.Value <= 0)
            {
                throw new InvalidOperationException(
                    $"Falta mapeo contable para el concepto '{codigoConcepto}'. Configure ContabilidadCuentaMapeo.");
            }

            return id.Value;
        }

        public async Task<int> ResolverTesoreriaAsync(
            int idEmpresa,
            string? formaPago = null,
            string? tipoCuentaFinanciera = null)
        {
            var concepto = EsBanco(tipoCuentaFinanciera, formaPago)
                ? ContabilidadConceptosMapeo.Banco
                : ContabilidadConceptosMapeo.Caja;

            return await ResolverRequeridoAsync(idEmpresa, concepto);
        }

        public async Task<int> ResolverTesoreriaPorCuentaAsync(
            int idEmpresa,
            int? idCuentaFinanciera,
            string? formaPago = null,
            string? tipoCuentaFinanciera = null)
        {
            if (idCuentaFinanciera is > 0)
            {
                var mapeoPrincipal = await _context.TesoreriaCuentaContableMapeo
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m =>
                        m.IdEmpresa == idEmpresa
                        && m.IdCuentaFinanciera == idCuentaFinanciera.Value
                        && m.Activo
                        && m.TipoMapeo == "PRINCIPAL");

                if (mapeoPrincipal?.IdCuentaContable is > 0)
                    return mapeoPrincipal.IdCuentaContable;

                var cuenta = await _context.CuentaFinanciera
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c =>
                        c.IdCuentaFinanciera == idCuentaFinanciera.Value
                        && c.IdEmpresa == idEmpresa);

                if (cuenta?.IdCuentaContable is > 0)
                    return cuenta.IdCuentaContable.Value;

                if (string.IsNullOrWhiteSpace(tipoCuentaFinanciera))
                    tipoCuentaFinanciera = cuenta?.TipoCuenta;
            }

            return await ResolverTesoreriaAsync(idEmpresa, formaPago, tipoCuentaFinanciera);
        }

        public async Task EnsureMapeoDefaultAsync(int idEmpresa)
        {
            var cuentas = await _context.CuentasContables
                .AsNoTracking()
                .Where(c => c.IdEmpresa == idEmpresa && c.Activa)
                .ToListAsync();

            if (cuentas.Count == 0)
                return;

            var porCodigo = cuentas
                .GroupBy(c => c.Codigo)
                .ToDictionary(g => g.Key, g => g.First().IdCuentaContable, StringComparer.OrdinalIgnoreCase);

            var existentes = await _context.ContabilidadCuentaMapeo
                .Where(m => m.IdEmpresa == idEmpresa)
                .ToListAsync();

            foreach (var (concepto, codigoCuenta) in ContabilidadConceptosMapeo.Defaults)
            {
                if (!porCodigo.TryGetValue(codigoCuenta, out var idCuenta))
                    continue;

                var actual = existentes.FirstOrDefault(m =>
                    string.Equals(m.CodigoConcepto, concepto, StringComparison.OrdinalIgnoreCase));

                if (actual == null)
                {
                    _context.ContabilidadCuentaMapeo.Add(new ContabilidadCuentaMapeo
                    {
                        IdEmpresa = idEmpresa,
                        CodigoConcepto = concepto,
                        IdCuentaContable = idCuenta,
                        Activo = true
                    });
                }
                else if (!actual.Activo || actual.IdCuentaContable != idCuenta)
                {
                    actual.IdCuentaContable = idCuenta;
                    actual.Activo = true;
                    _context.ContabilidadCuentaMapeo.Update(actual);
                }
            }

            await _context.SaveChangesAsync();
        }

        private static bool EsBanco(string? tipoCuentaFinanciera, string? formaPago)
        {
            var tipo = (tipoCuentaFinanciera ?? string.Empty).Trim().ToUpperInvariant();
            if (tipo is "BANCO" or "TARJETA" or "TRANSFERENCIA")
                return true;

            var pago = (formaPago ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(pago))
                return false;

            // Bancos / canales digitales RD usados como MetodoPagoCuenta
            if (pago is "POPULAR" or "BHD" or "BILLET BHD" or "BANRESERVAS" or "RESERVAS"
                or "APAP" or "SCOTIABANK" or "SANTA CRUZ" or "ASOCIACION POPULAR"
                or "ASOCIACION CIBAO" or "LA NACIONAL" or "QIK" or "PAYPAL")
                return true;

            return pago.Contains("BANCO")
                || pago.Contains("TRANSFEREN")
                || pago.Contains("TARJETA")
                || pago.Contains("CHEQUE")
                || pago.Contains("DEPOSITO")
                || pago.Contains("DEPÓSITO")
                || pago.Contains("RESERVA")
                || pago.Contains("POPULAR")
                || pago.Contains("BHD");
        }
    }
}
