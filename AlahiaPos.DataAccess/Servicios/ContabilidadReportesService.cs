using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    /// <summary>
    /// Reportes contables. Todos los saldos provienen del mismo origen:
    /// AsientosContables + AsientosContablesDetalle (Libro Diario / Mayor).
    /// </summary>
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
                .AsNoTracking()
                .Where(c => c.IdEmpresa == idEmpresa && c.PermiteMovimiento && c.Activa)
                .OrderBy(c => c.Codigo)
                .ToListAsync();

            var movimientos = await ObtenerMovimientosAsync(idEmpresa, null, fin);

            var lineas = new List<BalanceComprobacionLineaDto>();

            foreach (var cuenta in cuentas)
            {
                var movsCuenta = movimientos.Where(m => m.IdCuentaContable == cuenta.IdCuentaContable).ToList();

                var debIni = movsCuenta.Where(m => m.Fecha < desde.Date).Sum(m => m.Debito);
                var creIni = movsCuenta.Where(m => m.Fecha < desde.Date).Sum(m => m.Credito);
                var saldoInicial = ContabilidadSaldoHelper.SaldoPorNaturaleza(cuenta.TipoCuenta, debIni, creIni);

                var enPeriodo = movsCuenta
                    .Where(m => m.Fecha >= desde.Date && m.Fecha <= fin)
                    .ToList();

                var totalDebito = enPeriodo.Sum(m => m.Debito);
                var totalCredito = enPeriodo.Sum(m => m.Credito);
                var saldoFinal = ContabilidadSaldoHelper.SaldoPorNaturaleza(
                    cuenta.TipoCuenta,
                    debIni + totalDebito,
                    creIni + totalCredito);

                if (saldoInicial == 0 && totalDebito == 0 && totalCredito == 0)
                    continue;

                var (deudor, acreedor) = PartirSaldoComprobacion(cuenta.TipoCuenta, saldoFinal);

                lineas.Add(new BalanceComprobacionLineaDto
                {
                    IdCuentaContable = cuenta.IdCuentaContable,
                    CodigoCuenta = cuenta.Codigo,
                    NombreCuenta = cuenta.Nombre,
                    TipoCuenta = cuenta.TipoCuenta,
                    SaldoInicial = saldoInicial,
                    TotalDebito = totalDebito,
                    TotalCredito = totalCredito,
                    SaldoFinal = saldoFinal,
                    SaldoDeudor = deudor,
                    SaldoAcreedor = acreedor
                });
            }

            var totalDebitos = lineas.Sum(l => l.TotalDebito);
            var totalCreditos = lineas.Sum(l => l.TotalCredito);
            var totalDeudor = lineas.Sum(l => l.SaldoDeudor);
            var totalAcreedor = lineas.Sum(l => l.SaldoAcreedor);

            return new BalanceComprobacionResumenDto
            {
                Desde = desde.Date,
                Hasta = hasta.Date,
                Lineas = lineas,
                TotalDebitos = totalDebitos,
                TotalCreditos = totalCreditos,
                TotalSaldoDeudor = totalDeudor,
                TotalSaldoAcreedor = totalAcreedor,
                Cuadra = Math.Abs(totalDebitos - totalCreditos) < 0.01m
                    && Math.Abs(totalDeudor - totalAcreedor) < 0.01m
            };
        }

        public async Task<EstadoResultadosDto> GetEstadoResultadosAsync(
            int idEmpresa, DateTime desde, DateTime hasta)
        {
            var fin = hasta.Date.AddDays(1).AddTicks(-1);
            var movimientos = await ObtenerSaldosPorCuentaAsync(
                idEmpresa,
                desde.Date,
                fin,
                soloResultado: true);

            var resultado = new EstadoResultadosDto
            {
                Desde = desde.Date,
                Hasta = hasta.Date
            };

            foreach (var mov in movimientos.OrderBy(m => m.Codigo))
            {
                var monto = ContabilidadSaldoHelper.SaldoPorNaturaleza(mov.TipoCuenta, mov.Debito, mov.Credito);
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
            resultado.UtilidadNeta = ContabilidadSaldoHelper.CalcularUtilidadNeta(
                resultado.Ingresos.Total,
                resultado.Costos.Total,
                resultado.Gastos.Total);

            return resultado;
        }

        public async Task<BalanceGeneralDto> GetBalanceGeneralAsync(int idEmpresa, DateTime fechaCorte)
        {
            var fin = fechaCorte.Date.AddDays(1).AddTicks(-1);

            // Misma fuente que Libro Diario / Mayor: asientos confirmados hasta la fecha de corte.
            var saldos = await ObtenerSaldosPorCuentaAsync(idEmpresa, null, fin, soloResultado: false);

            var balance = new BalanceGeneralDto { FechaCorte = fechaCorte.Date };

            decimal ingresos = 0, costos = 0, gastos = 0;

            foreach (var mov in saldos.OrderBy(m => m.Codigo))
            {
                var saldo = ContabilidadSaldoHelper.SaldoPorNaturaleza(mov.TipoCuenta, mov.Debito, mov.Credito);
                if (saldo == 0) continue;

                switch (mov.TipoCuenta)
                {
                    case ContabilidadConstantes.TipoActivo:
                        balance.Activos.Lineas.Add(new BalanceGeneralLineaDto
                        {
                            CodigoCuenta = mov.Codigo,
                            NombreCuenta = mov.Nombre,
                            Saldo = saldo
                        });
                        break;

                    case ContabilidadConstantes.TipoPasivo:
                        balance.Pasivos.Lineas.Add(new BalanceGeneralLineaDto
                        {
                            CodigoCuenta = mov.Codigo,
                            NombreCuenta = mov.Nombre,
                            Saldo = saldo
                        });
                        break;

                    case ContabilidadConstantes.TipoCapital:
                        balance.Capital.Lineas.Add(new BalanceGeneralLineaDto
                        {
                            CodigoCuenta = mov.Codigo,
                            NombreCuenta = mov.Nombre,
                            Saldo = saldo
                        });
                        break;

                    case ContabilidadConstantes.TipoIngresos:
                        ingresos += saldo;
                        break;
                    case ContabilidadConstantes.TipoCostos:
                        costos += saldo;
                        break;
                    case ContabilidadConstantes.TipoGastos:
                        gastos += saldo;
                        break;
                }
            }

            // Sin cierre contable: la utilidad/pérdida neta integra el Patrimonio.
            var resultadoEjercicio = ContabilidadSaldoHelper.CalcularUtilidadNeta(ingresos, costos, gastos);
            balance.ResultadoEjercicio = resultadoEjercicio;

            if (resultadoEjercicio != 0)
            {
                balance.Capital.Lineas.Add(new BalanceGeneralLineaDto
                {
                    CodigoCuenta = ContabilidadSaldoHelper.ResultadoEjercicioCodigo,
                    NombreCuenta = ContabilidadSaldoHelper.ResultadoEjercicioNombre,
                    Saldo = resultadoEjercicio
                });
            }

            balance.Activos.Total = balance.Activos.Lineas.Sum(l => l.Saldo);
            balance.Pasivos.Total = balance.Pasivos.Lineas.Sum(l => l.Saldo);
            balance.Capital.Total = balance.Capital.Lineas.Sum(l => l.Saldo);
            balance.TotalActivos = balance.Activos.Total;
            balance.TotalPasivos = balance.Pasivos.Total;
            balance.TotalCapital = balance.Capital.Total;
            balance.TotalPasivoCapital = balance.TotalPasivos + balance.TotalCapital;
            balance.Diferencia = Math.Round(balance.TotalActivos - balance.TotalPasivoCapital, 2);
            balance.Cuadra = Math.Abs(balance.Diferencia) < 0.01m;

            return balance;
        }

        private async Task<List<MovimientoCuentaRaw>> ObtenerMovimientosAsync(
            int idEmpresa,
            DateTime? desdeInclusive,
            DateTime hastaInclusive)
        {
            var query =
                from detalle in _context.AsientosContablesDetalle.AsNoTracking()
                join asiento in _context.AsientosContables.AsNoTracking()
                    on detalle.IdAsientoContable equals asiento.IdAsientoContable
                where asiento.IdEmpresa == idEmpresa
                    && asiento.Estado != ContabilidadConstantes.EstadoAsientoAnulado
                    && asiento.Fecha <= hastaInclusive
                    && (!desdeInclusive.HasValue || asiento.Fecha >= desdeInclusive.Value)
                select new MovimientoCuentaRaw
                {
                    IdCuentaContable = detalle.IdCuentaContable,
                    Fecha = asiento.Fecha,
                    Debito = detalle.Debito,
                    Credito = detalle.Credito
                };

            return await query.ToListAsync();
        }

        private async Task<List<SaldoCuentaRaw>> ObtenerSaldosPorCuentaAsync(
            int idEmpresa,
            DateTime? desdeInclusive,
            DateTime hastaInclusive,
            bool soloResultado)
        {
            var query =
                from detalle in _context.AsientosContablesDetalle.AsNoTracking()
                join asiento in _context.AsientosContables.AsNoTracking()
                    on detalle.IdAsientoContable equals asiento.IdAsientoContable
                join cuenta in _context.CuentasContables.AsNoTracking()
                    on detalle.IdCuentaContable equals cuenta.IdCuentaContable
                where asiento.IdEmpresa == idEmpresa
                    && asiento.Fecha <= hastaInclusive
                    && asiento.Estado != ContabilidadConstantes.EstadoAsientoAnulado
                    && cuenta.PermiteMovimiento
                    && cuenta.Activa
                select new { detalle, asiento, cuenta };

            if (desdeInclusive.HasValue)
                query = query.Where(x => x.asiento.Fecha >= desdeInclusive.Value);

            if (soloResultado)
            {
                query = query.Where(x =>
                    x.cuenta.TipoCuenta == ContabilidadConstantes.TipoIngresos ||
                    x.cuenta.TipoCuenta == ContabilidadConstantes.TipoCostos ||
                    x.cuenta.TipoCuenta == ContabilidadConstantes.TipoGastos);
            }

            return await query
                .GroupBy(x => new
                {
                    x.detalle.IdCuentaContable,
                    x.cuenta.Codigo,
                    x.cuenta.Nombre,
                    x.cuenta.TipoCuenta
                })
                .Select(g => new SaldoCuentaRaw
                {
                    IdCuentaContable = g.Key.IdCuentaContable,
                    Codigo = g.Key.Codigo,
                    Nombre = g.Key.Nombre,
                    TipoCuenta = g.Key.TipoCuenta,
                    Debito = g.Sum(x => x.detalle.Debito),
                    Credito = g.Sum(x => x.detalle.Credito)
                })
                .ToListAsync();
        }

        /// <summary>
        /// En balance de comprobación: deudor/acreedor según naturaleza y signo del saldo.
        /// </summary>
        private static (decimal deudor, decimal acreedor) PartirSaldoComprobacion(
            string tipoCuenta,
            decimal saldoNaturaleza)
        {
            if (saldoNaturaleza == 0)
                return (0, 0);

            // Saldo positivo en naturaleza deudora → columna deudora; en acreedora → acreedora.
            // Saldo negativo = naturaleza invertida (contra-cuenta).
            if (ContabilidadSaldoHelper.EsNaturalezaDeudora(tipoCuenta))
            {
                return saldoNaturaleza > 0
                    ? (saldoNaturaleza, 0)
                    : (0, Math.Abs(saldoNaturaleza));
            }

            return saldoNaturaleza > 0
                ? (0, saldoNaturaleza)
                : (Math.Abs(saldoNaturaleza), 0);
        }

        private sealed class MovimientoCuentaRaw
        {
            public int IdCuentaContable { get; set; }
            public DateTime Fecha { get; set; }
            public decimal Debito { get; set; }
            public decimal Credito { get; set; }
        }

        private sealed class SaldoCuentaRaw
        {
            public int IdCuentaContable { get; set; }
            public string Codigo { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public string TipoCuenta { get; set; } = string.Empty;
            public decimal Debito { get; set; }
            public decimal Credito { get; set; }
        }
    }
}
