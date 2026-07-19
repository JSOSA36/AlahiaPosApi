using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    public class ContabilidadLibrosService : IContabilidadLibrosService
    {
        private readonly AlahiaPosContext _context;

        public ContabilidadLibrosService(AlahiaPosContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<LibroDiarioLineaDto>> GetLibroDiarioAsync(
            int idEmpresa,
            DateTime desde,
            DateTime hasta)
        {
            var fin = hasta.Date.AddDays(1).AddTicks(-1);

            var query =
                from detalle in _context.AsientosContablesDetalle
                join asiento in _context.AsientosContables
                    on detalle.IdAsientoContable equals asiento.IdAsientoContable
                join cuenta in _context.CuentasContables
                    on detalle.IdCuentaContable equals cuenta.IdCuentaContable
                where asiento.IdEmpresa == idEmpresa
                    && asiento.Fecha >= desde.Date
                    && asiento.Fecha <= fin
                    && asiento.Estado != ContabilidadConstantes.EstadoAsientoAnulado
                orderby asiento.Fecha, asiento.Numero, detalle.IdAsientoContableDetalle
                select new LibroDiarioLineaDto
                {
                    IdAsientoContable = asiento.IdAsientoContable,
                    Numero = asiento.Numero,
                    Fecha = asiento.Fecha,
                    Concepto = asiento.Concepto,
                    Estado = asiento.Estado,
                    CodigoCuenta = cuenta.Codigo,
                    NombreCuenta = cuenta.Nombre,
                    Debito = detalle.Debito,
                    Credito = detalle.Credito,
                    Referencia = detalle.Referencia,
                    OrigenModulo = asiento.OrigenModulo
                };

            return await query.ToListAsync();
        }

        public async Task<MayorGeneralResumenDto?> GetMayorGeneralAsync(
            int idEmpresa,
            int idCuentaContable,
            DateTime desde,
            DateTime hasta)
        {
            var cuenta = await _context.CuentasContables
                .FirstOrDefaultAsync(c => c.IdCuentaContable == idCuentaContable && c.IdEmpresa == idEmpresa);

            if (cuenta == null)
                return null;

            var fin = hasta.Date.AddDays(1).AddTicks(-1);

            var movimientos = await (
                from detalle in _context.AsientosContablesDetalle
                join asiento in _context.AsientosContables
                    on detalle.IdAsientoContable equals asiento.IdAsientoContable
                where detalle.IdCuentaContable == idCuentaContable
                    && asiento.IdEmpresa == idEmpresa
                    && asiento.Fecha >= desde.Date
                    && asiento.Fecha <= fin
                    && asiento.Estado != ContabilidadConstantes.EstadoAsientoAnulado
                orderby asiento.Fecha, asiento.Numero
                select new
                {
                    asiento.Fecha,
                    asiento.Numero,
                    asiento.Concepto,
                    detalle.Referencia,
                    detalle.Debito,
                    detalle.Credito
                }).ToListAsync();

            decimal saldo = 0;
            var lineas = new List<MayorGeneralLineaDto>();

            foreach (var mov in movimientos)
            {
                saldo += mov.Debito - mov.Credito;
                lineas.Add(new MayorGeneralLineaDto
                {
                    Fecha = mov.Fecha,
                    NumeroAsiento = mov.Numero,
                    Concepto = mov.Concepto,
                    Referencia = mov.Referencia,
                    Debito = mov.Debito,
                    Credito = mov.Credito,
                    Saldo = saldo
                });
            }

            return new MayorGeneralResumenDto
            {
                IdCuentaContable = cuenta.IdCuentaContable,
                CodigoCuenta = cuenta.Codigo,
                NombreCuenta = cuenta.Nombre,
                TipoCuenta = cuenta.TipoCuenta,
                TotalDebito = movimientos.Sum(m => m.Debito),
                TotalCredito = movimientos.Sum(m => m.Credito),
                Saldo = saldo,
                Movimientos = lineas
            };
        }

        public async Task<IEnumerable<MayorGeneralResumenDto>> GetMayorGeneralResumenAsync(
            int idEmpresa,
            DateTime desde,
            DateTime hasta)
        {
            var cuentas = await _context.CuentasContables
                .Where(c => c.IdEmpresa == idEmpresa && c.PermiteMovimiento && c.Activa)
                .OrderBy(c => c.Codigo)
                .ToListAsync();

            var resultado = new List<MayorGeneralResumenDto>();

            foreach (var cuenta in cuentas)
            {
                var mayor = await GetMayorGeneralAsync(idEmpresa, cuenta.IdCuentaContable, desde, hasta);
                if (mayor != null && (mayor.TotalDebito > 0 || mayor.TotalCredito > 0))
                    resultado.Add(mayor);
            }

            return resultado;
        }
    }
}
