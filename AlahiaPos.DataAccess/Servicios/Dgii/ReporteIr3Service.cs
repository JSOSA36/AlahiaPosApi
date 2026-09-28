using System.Globalization;
using System.Text;
using System.Text.Json;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios.Dgii
{
    /// <summary>
    /// Motor IR-3: retenciones ISR de asalariados desde NominaProceso (PAGADA/CERRADA).
    /// No declara ITBIS (IT-1) ni retenciones a terceros (IR-17). Preview en vivo; no persiste periodo.
    /// </summary>
    public class ReporteIr3Service : IReporteIr3Service
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        private static readonly string[] EstadosIncluidos =
        {
            RrhhEstados.NominaPagada,
            RrhhEstados.NominaCerrada
        };
        private static readonly string[] EstadosPendientes =
        {
            RrhhEstados.NominaBorrador,
            RrhhEstados.NominaEnRevision,
            RrhhEstados.NominaAprobada
        };

        private readonly AlahiaPosContext _ctx;

        public ReporteIr3Service(AlahiaPosContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<ReporteIr3Dto> ObtenerAsync(
            int idEmpresa,
            DateTime? desde = null,
            DateTime? hasta = null,
            string? periodo = null)
        {
            var (d, h, periodoTxt) = ResolverRango(desde, hasta, periodo);

            var empresa = await _ctx.Empresas.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa);
            var cfg = await _ctx.DgiiConfiguracionEmpresa.AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdEmpresa == idEmpresa);

            var procesos = await _ctx.NominaProceso.AsNoTracking()
                .Include(p => p.Empleados)
                .Where(p => p.IdEmpresa == idEmpresa)
                .ToListAsync();

            var incluidos = procesos
                .Where(p => EstadosIncluidos.Contains(p.Estado) && Solapa(p.FechaInicio, p.FechaFin, d, h))
                .OrderBy(p => p.FechaInicio)
                .ThenBy(p => p.IdNominaProceso)
                .ToList();

            var pendientes = procesos
                .Where(p => EstadosPendientes.Contains(p.Estado) && Solapa(p.FechaInicio, p.FechaFin, d, h))
                .ToList();

            var alertas = new List<string>();
            if (cfg == null || !cfg.FiscalActivo || !cfg.GenerarIt1)
                alertas.Add("La declaración IR-3 se habilita con la misma configuración fiscal que el IT-1.");

            alertas.Add(
                "El IR-3 se presenta mensualmente aunque no haya retención (opción Declaraciones en Cero de la OFV). Vence el día 10 del mes siguiente.");
            alertas.Add(
                "Fuente: nóminas PAGADA o CERRADA del período. Antes de enviar a DGII debe cargar la misma nómina en TSS y validar el IR-4.");
            alertas.Add(
                "ISR de suplidores, honorarios y alquileres no entra aquí (va al IR-17). Beneficios en especie al empleado van al IR-17 casilla 28.");

            foreach (var p in pendientes)
            {
                alertas.Add(
                    $"Nómina #{p.IdNominaProceso} ({p.PeriodKey}) está en {p.Estado} y no entra al IR-3 hasta pagarla o cerrarla.");
            }

            var cedulas = await _ctx.EmpleadosP.AsNoTracking()
                .Where(e => e.IdEmpresa == idEmpresa)
                .Select(e => new { e.IdEmpleados, e.Cedula, e.Nombre })
                .ToListAsync();
            var cedulaMap = cedulas.ToDictionary(x => x.IdEmpleados);

            var lineas = new List<Ir3LineaAsalariadoDto>();
            var nominasDto = new List<Ir3NominaIncluidaDto>();

            foreach (var p in incluidos)
            {
                decimal isrNomina = 0;
                decimal brutoNomina = 0;
                foreach (var e in p.Empleados)
                {
                    var afp = R(SumLegalJson(e.LineasJson, "AFP_EMPLEADO"));
                    var sfs = R(SumLegalJson(e.LineasJson, "SFS_EMPLEADO"));
                    var isr = R(SumLegalJson(e.LineasJson, "ISR_EMPLEADO"));
                    var ingresos = R(e.SalarioBase + e.HorasExtra + e.Comisiones + e.Bonificaciones + e.OtrosIngresos);
                    if (ingresos <= 0)
                        ingresos = R(e.Bruto);
                    var baseIsr = R(Math.Max(0, ingresos - afp - sfs));

                    cedulaMap.TryGetValue(e.IdEmpleados, out var emp);
                    lineas.Add(new Ir3LineaAsalariadoDto
                    {
                        IdNominaProceso = p.IdNominaProceso,
                        PeriodKey = p.PeriodKey,
                        EstadoNomina = p.Estado,
                        IdEmpleados = e.IdEmpleados,
                        Cedula = emp?.Cedula,
                        Nombre = FirstNonEmpty(e.NombreEmpleado, emp?.Nombre),
                        SalarioBase = R(e.SalarioBase),
                        HorasExtra = R(e.HorasExtra),
                        Comisiones = R(e.Comisiones),
                        Bonificaciones = R(e.Bonificaciones),
                        OtrosIngresos = R(e.OtrosIngresos),
                        Bruto = R(e.Bruto),
                        AfpEmpleado = afp,
                        SfsEmpleado = sfs,
                        BaseImponibleIsr = baseIsr,
                        IsrRetenido = isr,
                        FechaInicio = p.FechaInicio,
                        FechaFin = p.FechaFin,
                        FechaPago = p.FechaPago
                    });
                    isrNomina += isr;
                    brutoNomina += R(e.Bruto);
                }

                nominasDto.Add(new Ir3NominaIncluidaDto
                {
                    IdNominaProceso = p.IdNominaProceso,
                    PeriodKey = p.PeriodKey,
                    Estado = p.Estado,
                    Frecuencia = p.Frecuencia,
                    FechaInicio = p.FechaInicio,
                    FechaFin = p.FechaFin,
                    FechaPago = p.FechaPago,
                    Empleados = p.Empleados.Count,
                    TotalBruto = brutoNomina,
                    TotalIsr = isrNomina
                });
            }

            var empleadosDistintos = lineas.Select(l => l.IdEmpleados).Distinct().Count();
            var totalBruto = R(lineas.Sum(l => l.Bruto));
            var totalAfp = R(lineas.Sum(l => l.AfpEmpleado));
            var totalSfs = R(lineas.Sum(l => l.SfsEmpleado));
            var totalBase = R(lineas.Sum(l => l.BaseImponibleIsr));
            var totalIsr = R(lineas.Sum(l => l.IsrRetenido));

            if (incluidos.Count == 0)
            {
                alertas.Add(
                    "No hay nóminas pagadas/cerradas en el período. Si no hubo asalariados, presente el IR-3 en cero en Oficina Virtual.");
            }
            else if (totalIsr == 0)
            {
                alertas.Add(
                    "Hay nómina en el período pero el ISR retenido es RD$0.00 (empleados bajo el mínimo exento). Aun así debe presentar el IR-3.");
            }

            var resumen = new List<Ir3CasillaDto>
            {
                Casilla(1, "R1", "Asalariados (distintos)", "I. Resumen de nómina",
                    monto: empleadosDistintos, cantidad: empleadosDistintos, origen: "AUTO_NOMINA"),
                Casilla(2, "R2", "Total remuneraciones (bruto)", "I. Resumen de nómina",
                    monto: totalBruto, origen: "AUTO_NOMINA"),
                Casilla(3, "R3", "AFP empleado (excluido de la base ISR)", "I. Resumen de nómina",
                    monto: totalAfp, origen: "AUTO_NOMINA"),
                Casilla(4, "R4", "SFS empleado (excluido de la base ISR)", "I. Resumen de nómina",
                    monto: totalSfs, origen: "AUTO_NOMINA"),
                Casilla(5, "R5", "Base imponible ISR (remuneraciones − AFP − SFS)", "I. Resumen de nómina",
                    monto: totalBase, origen: "AUTO_NOMINA", formula: "R2−R3−R4"),
                Casilla(6, "R6", "ISR retenido a asalariados", "I. Resumen de nómina",
                    monto: totalIsr, origen: "AUTO_NOMINA")
            };

            var c1 = totalIsr;
            var c2 = 0m;
            var c3 = 0m;
            var c4 = 0m;
            var c5 = 0m;
            var diferencia = R(c1 - c2 - c3 - c4 - c5);
            var c6 = diferencia > 0 ? diferencia : 0m;
            var c7 = diferencia < 0 ? R(-diferencia) : 0m;
            var c8 = 0m;
            var c9 = 0m;
            var c10 = R(c6 + c8 + c9);

            var liquidacion = new List<Ir3CasillaDto>
            {
                Casilla(1, "C1", "IMPUESTO RETENIDO EN EL PERÍODO", "II. Liquidación",
                    monto: c1, origen: "AUTO_NOMINA", formula: "Suma ISR_EMPLEADO de nóminas pagadas"),
                Casilla(2, "C2", "PAGOS A CUENTA", "II. Liquidación",
                    origen: "MANUAL_PENDIENTE", editable: true,
                    alerta: "Complete en Oficina Virtual si aplicó pagos a cuenta del IR-3."),
                Casilla(3, "C3", "SALDO A FAVOR ANTERIOR", "II. Liquidación",
                    origen: "MANUAL_PENDIENTE", editable: true,
                    alerta: "Saldo a favor del IR-3 del período anterior."),
                Casilla(4, "C4", "CRÉDITO GASTOS EDUCATIVOS (IR-18)", "II. Liquidación",
                    origen: "MANUAL_PENDIENTE", editable: true,
                    alerta: "Crédito autorizado por gastos educativos (formulario IR-18)."),
                Casilla(5, "C5", "CRÉDITO AUTORIZADO", "II. Liquidación",
                    origen: "MANUAL_PENDIENTE", editable: true,
                    alerta: "Otros créditos autorizados por DGII."),
                Casilla(6, "C6", "IMPUESTO A PAGAR", "II. Liquidación",
                    monto: c6, origen: "FORMULA", calculada: true, formula: "MAX(0, 1−2−3−4−5)"),
                Casilla(7, "C7", "NUEVO SALDO A FAVOR", "II. Liquidación",
                    monto: c7, origen: "FORMULA", calculada: true, formula: "MAX(0, 2+3+4+5−1)"),
                Casilla(8, "C8", "RECARGOS", "III. Penalidades",
                    origen: "MANUAL_PENDIENTE", editable: true,
                    alerta: "Si presenta fuera de plazo, DGII aplica recargo. No se infiere de la nómina."),
                Casilla(9, "C9", "INTERÉS INDEMNIZATORIO", "III. Penalidades",
                    origen: "MANUAL_PENDIENTE", editable: true,
                    alerta: "Interés por mora según DGII. Complete en Oficina Virtual si aplica."),
                Casilla(10, "C10", "TOTAL A PAGAR", "IV. Monto a pagar",
                    monto: c10, origen: "FORMULA", calculada: true, formula: "6+8+9")
            };

            var fechaLimite = new DateTime(h.Year, h.Month, 1).AddMonths(1).AddDays(9);

            var dto = new ReporteIr3Dto
            {
                IdEmpresa = idEmpresa,
                RncEmpresa = Limpiar(empresa?.RNC),
                RazonSocial = FirstNonEmpty(cfg?.RazonSocial, empresa?.NombreComercial),
                NombreComercial = empresa?.NombreComercial,
                CorreoElectronico = empresa?.CorreElectronico,
                Telefono = empresa?.Telefono,
                Periodo = periodoTxt,
                Desde = d,
                Hasta = h,
                FechaLimitePago = fechaLimite,
                TipoDeclaracion = "Original",
                VersionInstructivo = "IR-3",
                CantidadEmpleados = empleadosDistintos,
                CantidadNominas = incluidos.Count,
                TotalRemuneraciones = totalBruto,
                TotalAfpEmpleado = totalAfp,
                TotalSfsEmpleado = totalSfs,
                TotalBaseImponible = totalBase,
                TotalIsrRetenido = totalIsr,
                ImpuestoAPagar = c6,
                SaldoAFavor = c7,
                TotalGeneralAPagar = c10,
                AlertasGlobales = alertas,
                Resumen = resumen,
                Liquidacion = liquidacion,
                NominasIncluidas = nominasDto,
                LineasAsalariados = lineas
                    .OrderBy(l => l.Nombre)
                    .ThenBy(l => l.FechaInicio)
                    .ToList()
            };

            dto.CantidadAlertas = dto.AlertasGlobales.Count
                + dto.Resumen.Sum(c => c.Alertas.Count)
                + dto.Liquidacion.Sum(c => c.Alertas.Count);
            dto.ContenidoCsv = GenerarCsv(dto);
            return dto;
        }

        private static Ir3CasillaDto Casilla(
            int numero,
            string codigo,
            string etiqueta,
            string seccion,
            decimal monto = 0,
            int? cantidad = null,
            string origen = "FORMULA",
            bool calculada = false,
            bool editable = false,
            string? formula = null,
            string? alerta = null)
        {
            var c = new Ir3CasillaDto
            {
                Numero = numero,
                Codigo = codigo,
                Etiqueta = etiqueta,
                Seccion = seccion,
                Cantidad = cantidad,
                Monto = R(monto),
                Origen = origen,
                EsCalculada = calculada,
                EsEditableUsuario = editable,
                FormulaAplicada = formula
            };
            if (!string.IsNullOrWhiteSpace(alerta))
                c.Alertas.Add(alerta);
            return c;
        }

        private static bool Solapa(DateTime inicio, DateTime fin, DateTime d, DateTime h) =>
            inicio.Date <= h && fin.Date >= d;

        private static (DateTime d, DateTime h, string periodo) ResolverRango(
            DateTime? desde, DateTime? hasta, string? periodo)
        {
            string periodoTxt;
            DateTime d;
            DateTime h;

            if (!string.IsNullOrWhiteSpace(periodo) && periodo!.Trim().Length == 6
                && int.TryParse(periodo.Trim()[..4], out var y)
                && int.TryParse(periodo.Trim().Substring(4, 2), out var m)
                && m is >= 1 and <= 12)
            {
                periodoTxt = periodo.Trim();
                d = new DateTime(y, m, 1);
                h = d.AddMonths(1).AddDays(-1);
            }
            else
            {
                d = (desde ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
                h = (hasta ?? DateTime.Today).Date;
                if (h < d) throw new ArgumentException("La fecha hasta no puede ser menor que desde.");
                periodoTxt = h.ToString("yyyyMM");
            }

            if (h < d) throw new ArgumentException("La fecha hasta no puede ser menor que desde.");
            return (d, h, periodoTxt);
        }

        private static decimal SumLegalJson(string? json, string code)
        {
            if (string.IsNullOrWhiteSpace(json))
                return 0;
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    return 0;

                decimal sum = 0;
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    if (el.ValueKind != JsonValueKind.Object)
                        continue;
                    var concepto = PropString(el, "ConceptCode") ?? PropString(el, "conceptCode");
                    if (!string.Equals(concepto, code, StringComparison.OrdinalIgnoreCase))
                        continue;
                    sum += PropDecimal(el, "Amount") ?? PropDecimal(el, "amount") ?? 0;
                }
                return sum;
            }
            catch
            {
                return 0;
            }
        }

        private static string? PropString(JsonElement el, string name) =>
            el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String
                ? p.GetString()
                : null;

        private static decimal? PropDecimal(JsonElement el, string name)
        {
            if (!el.TryGetProperty(name, out var p))
                return null;
            return p.ValueKind switch
            {
                JsonValueKind.Number => p.GetDecimal(),
                JsonValueKind.String when decimal.TryParse(p.GetString(), NumberStyles.Any, Inv, out var v) => v,
                _ => null
            };
        }

        private static decimal R(decimal v) =>
            Math.Round(v, 2, MidpointRounding.AwayFromZero);

        private static string Limpiar(string? s) =>
            string.IsNullOrWhiteSpace(s) ? "" : new string(s.Where(char.IsDigit).ToArray());

        private static string? FirstNonEmpty(params string?[] values) =>
            values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        private static string GenerarCsv(ReporteIr3Dto dto)
        {
            var sb = new StringBuilder();
            sb.AppendLine("FORMULARIO;NUMERO;CODIGO;SECCION;ETIQUETA;CANTIDAD;MONTO;ORIGEN;FORMULA;ALERTAS");
            void Write(string form, Ir3CasillaDto c)
            {
                sb.Append(form).Append(';')
                    .Append(c.Numero).Append(';')
                    .Append(Esc(c.Codigo)).Append(';')
                    .Append(Esc(c.Seccion)).Append(';')
                    .Append(Esc(c.Etiqueta)).Append(';')
                    .Append(c.Cantidad?.ToString(Inv) ?? "").Append(';')
                    .Append(c.Monto.ToString("0.00", Inv)).Append(';')
                    .Append(c.Origen).Append(';')
                    .Append(Esc(c.FormulaAplicada)).Append(';')
                    .Append(Esc(string.Join(" | ", c.Alertas)))
                    .AppendLine();
            }

            foreach (var c in dto.Resumen) Write("IR-3-RESUMEN", c);
            foreach (var c in dto.Liquidacion) Write("IR-3-LIQ", c);

            sb.AppendLine();
            sb.AppendLine("ORIGEN;ID_NOMINA;PERIODO_NOMINA;ESTADO;ID_EMPLEADO;CEDULA;NOMBRE;BRUTO;AFP;SFS;BASE_ISR;ISR;DESDE;HASTA;FECHA_PAGO");
            foreach (var l in dto.LineasAsalariados)
            {
                sb.Append("NOMINA").Append(';')
                    .Append(l.IdNominaProceso).Append(';')
                    .Append(Esc(l.PeriodKey)).Append(';')
                    .Append(Esc(l.EstadoNomina)).Append(';')
                    .Append(l.IdEmpleados).Append(';')
                    .Append(Esc(l.Cedula)).Append(';')
                    .Append(Esc(l.Nombre)).Append(';')
                    .Append(l.Bruto.ToString("0.00", Inv)).Append(';')
                    .Append(l.AfpEmpleado.ToString("0.00", Inv)).Append(';')
                    .Append(l.SfsEmpleado.ToString("0.00", Inv)).Append(';')
                    .Append(l.BaseImponibleIsr.ToString("0.00", Inv)).Append(';')
                    .Append(l.IsrRetenido.ToString("0.00", Inv)).Append(';')
                    .Append(l.FechaInicio.ToString("yyyy-MM-dd", Inv)).Append(';')
                    .Append(l.FechaFin.ToString("yyyy-MM-dd", Inv)).Append(';')
                    .Append(l.FechaPago?.ToString("yyyy-MM-dd", Inv) ?? "")
                    .AppendLine();
            }
            return sb.ToString();
        }

        private static string Esc(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var t = s.Replace("\"", "\"\"");
            return t.Contains(';') || t.Contains('"') || t.Contains('\n') ? $"\"{t}\"" : t;
        }
    }
}
