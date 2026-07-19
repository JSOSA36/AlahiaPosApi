using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    public class ContabilidadReportesService : IContabilidadReportesService
    {
        private readonly AlahiaPosContext _context;

        public ContabilidadReportesService(AlahiaPosContext context)
        {
            _context = context;
        }

        public async Task<BalanceComprobacionResumenDto> GetBalanceComprobacionAsync(
            int idEmpresa, DateTime desde, DateTime hasta)
        {
            var fin = hasta.Date.AddDays(1).AddTicks(-1);
            var cuentas = await _context.CuentasContables
                .Where(c => c.IdEmpresa == idEmpresa && c.PermiteMovimiento && c.Activa)
                .OrderBy(c => c.Codigo)
                .ToListAsync();

            var movimientos = await (
                from detalle in _context.AsientosContablesDetalle
                join asiento in _context.AsientosContables
                    on detalle.IdAsientoContable equals asiento.IdAsientoContable
                where asiento.IdEmpresa == idEmpresa
                    && asiento.Estado != ContabilidadConstantes.EstadoAsientoAnulado
                select new
                {
                    detalle.IdCuentaContable,
                    asiento.Fecha,
                    detalle.Debito,
                    detalle.Credito
                }).ToListAsync();

            var lineas = new List<BalanceComprobacionLineaDto>();

            foreach (var cuenta in cuentas)
            {
                var movsCuenta = movimientos.Where(m => m.IdCuentaContable == cuenta.IdCuentaContable).ToList();

                var saldoInicial = movsCuenta
                    .Where(m => m.Fecha < desde.Date)
                    .Sum(m => m.Debito - m.Credito);

                var enPeriodo = movsCuenta
                    .Where(m => m.Fecha >= desde.Date && m.Fecha <= fin)
                    .ToList();

                var totalDebito = enPeriodo.Sum(m => m.Debito);
                var totalCredito = enPeriodo.Sum(m => m.Credito);
                var saldoFinal = saldoInicial + totalDebito - totalCredito;

                if (saldoInicial == 0 && totalDebito == 0 && totalCredito == 0)
                    continue;

                lineas.Add(new BalanceComprobacionLineaDto
                {
                    IdCuentaContable = cuenta.IdCuentaContable,
                    CodigoCuenta = cuenta.Codigo,
                    NombreCuenta = cuenta.Nombre,
                    TipoCuenta = cuenta.TipoCuenta,
                    SaldoInicial = saldoInicial,
                    TotalDebito = totalDebito,
                    TotalCredito = totalCredito,
                    SaldoFinal = saldoFinal
                });
            }

            return new BalanceComprobacionResumenDto
            {
                Desde = desde.Date,
                Hasta = hasta.Date,
                Lineas = lineas,
                TotalDebitos = lineas.Sum(l => l.TotalDebito),
                TotalCreditos = lineas.Sum(l => l.TotalCredito)
            };
        }

        public async Task<EstadoResultadosDto> GetEstadoResultadosAsync(
            int idEmpresa, DateTime desde, DateTime hasta)
        {
            var fin = hasta.Date.AddDays(1).AddTicks(-1);

            var cuentas = await _context.CuentasContables
                .Where(c => c.IdEmpresa == idEmpresa && c.PermiteMovimiento && c.Activa)
                .Where(c =>
                    c.TipoCuenta == ContabilidadConstantes.TipoIngresos ||
                    c.TipoCuenta == ContabilidadConstantes.TipoCostos ||
                    c.TipoCuenta == ContabilidadConstantes.TipoGastos)
                .OrderBy(c => c.Codigo)
                .ToListAsync();

            var movimientos = await (
                from detalle in _context.AsientosContablesDetalle
                join asiento in _context.AsientosContables
                    on detalle.IdAsientoContable equals asiento.IdAsientoContable
                join cuenta in _context.CuentasContables
                    on detalle.IdCuentaContable equals cuenta.IdCuentaContable
                where asiento.IdEmpresa == idEmpresa
                    && asiento.Fecha >= desde.Date
                    && asiento.Fecha <= fin
                    && asiento.Estado != ContabilidadConstantes.EstadoAsientoAnulado
                group new { detalle.Debito, detalle.Credito } by new
                {
                    detalle.IdCuentaContable,
                    cuenta.Codigo,
                    cuenta.Nombre,
                    cuenta.TipoCuenta
                } into g
                select new
                {
                    g.Key.IdCuentaContable,
                    g.Key.Codigo,
                    g.Key.Nombre,
                    g.Key.TipoCuenta,
                    Debito = g.Sum(x => x.Debito),
                    Credito = g.Sum(x => x.Credito)
                }).ToListAsync();

            var resultado = new EstadoResultadosDto
            {
                Desde = desde.Date,
                Hasta = hasta.Date
            };

            foreach (var mov in movimientos)
            {
                decimal monto = mov.TipoCuenta switch
                {
                    ContabilidadConstantes.TipoIngresos => mov.Credito - mov.Debito,
                    _ => mov.Debito - mov.Credito
                };

                if (monto == 0) continue;

                var linea = new EstadoResultadosLineaDto
                {
                    CodigoCuenta = mov.Codigo,
                    NombreCuenta = mov.Nombre,
                    Monto = monto
                };

                switch (mov.TipoCuenta)
                {
                    case ContabilidadConstantes.TipoIngresos:
                        resultado.Ingresos.Lineas.Add(linea);
                        break;
                    case ContabilidadConstantes.TipoCostos:
                        resultado.Costos.Lineas.Add(linea);
                        break;
                    case ContabilidadConstantes.TipoGastos:
                        resultado.Gastos.Lineas.Add(linea);
                        break;
                }
            }

            resultado.Ingresos.Total = resultado.Ingresos.Lineas.Sum(l => l.Monto);
            resultado.Costos.Total = resultado.Costos.Lineas.Sum(l => l.Monto);
            resultado.Gastos.Total = resultado.Gastos.Lineas.Sum(l => l.Monto);
            resultado.UtilidadBruta = resultado.Ingresos.Total - resultado.Costos.Total;
            resultado.UtilidadNeta = resultado.UtilidadBruta - resultado.Gastos.Total;

            return resultado;
        }

        public async Task<BalanceGeneralDto> GetBalanceGeneralAsync(int idEmpresa, DateTime fechaCorte)
        {
            var fin = fechaCorte.Date.AddDays(1).AddTicks(-1);

            var movimientos = await (
                from detalle in _context.AsientosContablesDetalle
                join asiento in _context.AsientosContables
                    on detalle.IdAsientoContable equals asiento.IdAsientoContable
                join cuenta in _context.CuentasContables
                    on detalle.IdCuentaContable equals cuenta.IdCuentaContable
                where asiento.IdEmpresa == idEmpresa
                    && asiento.Fecha <= fin
                    && asiento.Estado != ContabilidadConstantes.EstadoAsientoAnulado
                    && cuenta.PermiteMovimiento
                    && cuenta.Activa
                group new { detalle.Debito, detalle.Credito, cuenta.TipoCuenta } by new
                {
                    detalle.IdCuentaContable,
                    cuenta.Codigo,
                    cuenta.Nombre,
                    cuenta.TipoCuenta
                } into g
                select new
                {
                    g.Key.Codigo,
                    g.Key.Nombre,
                    g.Key.TipoCuenta,
                    Saldo = g.Sum(x => x.Debito - x.Credito)
                }).ToListAsync();

            var balance = new BalanceGeneralDto { FechaCorte = fechaCorte.Date };

            foreach (var mov in movimientos.Where(m => m.Saldo != 0).OrderBy(m => m.Codigo))
            {
                var linea = new BalanceGeneralLineaDto
                {
                    CodigoCuenta = mov.Codigo,
                    NombreCuenta = mov.Nombre,
                    Saldo = mov.Saldo
                };

                switch (mov.TipoCuenta)
                {
                    case ContabilidadConstantes.TipoActivo:
                        balance.Activos.Lineas.Add(linea);
                        break;
                    case ContabilidadConstantes.TipoPasivo:
                        balance.Pasivos.Lineas.Add(linea);
                        break;
                    case ContabilidadConstantes.TipoCapital:
                        balance.Capital.Lineas.Add(linea);
                        break;
                }
            }

            balance.Activos.Total = balance.Activos.Lineas.Sum(l => l.Saldo);
            balance.Pasivos.Total = balance.Pasivos.Lineas.Sum(l => l.Saldo);
            balance.Capital.Total = balance.Capital.Lineas.Sum(l => l.Saldo);
            balance.TotalActivos = balance.Activos.Total;
            balance.TotalPasivos = balance.Pasivos.Total;
            balance.TotalCapital = balance.Capital.Total;
            balance.TotalPasivoCapital = balance.TotalPasivos + balance.TotalCapital;
            balance.Diferencia = balance.TotalActivos - balance.TotalPasivoCapital;

            return balance;
        }
    }
}
