using System.Globalization;
using System.Text;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios.Dgii
{
    /// <summary>
    /// Motor IR-17 2026 (Julio 2026 en adelante). Consolida retenciones ISR del 606 (FACTC).
    /// No declara ITBIS (eso es IT-1) ni asalariados (IR-3). Preview en vivo; no persiste periodo.
    /// </summary>
    public class ReporteIr17Service : IReporteIr17Service
    {
        private const int TipoDocumentoFacturaCompra = 11;
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private readonly AlahiaPosContext _ctx;

        public ReporteIr17Service(AlahiaPosContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<ReporteIr17Dto> ObtenerAsync(
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

            var compras = await _ctx.OrdenCompraHeaders.AsNoTracking()
                .Where(o =>
                    o.IdEmpresa == idEmpresa
                    && o.IdTipoDocumentos == TipoDocumentoFacturaCompra
                    && o.Estado != "BORRADOR"
                    && o.Estado != "ANULADA"
                    && o.MontoRetencionRenta > 0
                    && o.TipoRetencionIsr != null
                    && o.TipoRetencionIsr > 0)
                .ToListAsync();

            compras = compras.Where(o =>
            {
                var fecha = (o.FechaPagoFiscal ?? o.FechaInseccion).Date;
                return fecha >= d && fecha <= h;
            }).ToList();

            var alertasGlobales = new List<string>();
            if (cfg == null || !cfg.FiscalActivo || !cfg.GenerarIt1)
                alertasGlobales.Add("La declaración IR-17 se habilita con la misma configuración fiscal que el IT-1.");

            if (int.TryParse(periodoTxt, out var pNum) && pNum < 202607)
                alertasGlobales.Add(
                    "Esta versión del formulario (IR-17-2026) aplica a retenciones practicadas desde julio 2026 (Ley 30-26). Verifique tasas si el período es anterior.");

            alertasGlobales.Add(
                "El IR-17 no se presenta en cero si no hubo retenciones ISR ni retribuciones complementarias. El Anexo R9C sí se envía en cero cuando sí corresponde presentar el IR-17.");
            alertasGlobales.Add(
                "ITBIS retenido no entra aquí (va al IT-1). ISR de asalariados va al IR-3.");

            var catalogo = CatalogoCasillas();
            var agregados = catalogo.ToDictionary(c => c.Numero, _ => (baseImp: 0m, impuesto: 0m, cant: 0, alertas: new List<string>()));
            var lineas = new List<Ir17LineaOrigenDto>();

            foreach (var o in compras)
            {
                var tipo = o.TipoRetencionIsr!.Value;
                if (!Mapa606.TryGetValue(tipo, out var casillaNum))
                {
                    alertasGlobales.Add(
                        $"FACTC #{o.IdOrdenCompraHeader} tiene TipoRetencionIsr={tipo} sin casilla IR-17. Revise el código 606.");
                    continue;
                }

                var baseImp = MontoImponible(o);
                var impuesto = R(o.MontoRetencionRenta);
                var def = catalogo.First(c => c.Numero == casillaNum);
                if (def.Tasa is > 0 && baseImp > 0)
                {
                    var esperado = R(baseImp * def.Tasa.Value);
                    if (Math.Abs(esperado - impuesto) > 1.00m)
                    {
                        agregados[casillaNum].alertas.Add(
                            $"NCF {o.NCF}: retención {impuesto:0.00} no cuadra con base {baseImp:0.00} × {(def.Tasa.Value * 100):0.##}% (= {esperado:0.00}). Se usa el monto capturado.");
                    }
                }

                var acc = agregados[casillaNum];
                acc.baseImp += baseImp;
                acc.impuesto += impuesto;
                acc.cant += 1;
                agregados[casillaNum] = acc;

                lineas.Add(new Ir17LineaOrigenDto
                {
                    IdOrdenCompraHeader = o.IdOrdenCompraHeader,
                    Ncf = o.NCF,
                    NumeroDocumento = o.NumeroDocumento,
                    TipoRetencionIsr = tipo,
                    Casilla = casillaNum,
                    MontoImponible = baseImp,
                    Impuesto = impuesto,
                    FechaPagoFiscal = o.FechaPagoFiscal ?? o.FechaInseccion
                });
            }

            var otras = new List<Ir17CasillaDto>();
            foreach (var def in catalogo.Where(c => c.Numero <= 24))
            {
                var acc = agregados[def.Numero];
                var origen = def.TipoRetencionIsr606.HasValue
                    ? (acc.cant > 0 ? "AUTO_606" : "AUTO_606")
                    : "MANUAL_PENDIENTE";
                var casilla = Clone(def);
                casilla.Cantidad = acc.cant;
                casilla.MontoImponible = R(acc.baseImp);
                casilla.Impuesto = R(acc.impuesto);
                casilla.Origen = origen;
                casilla.EsEditableUsuario = !def.TipoRetencionIsr606.HasValue;
                if (!def.TipoRetencionIsr606.HasValue)
                    casilla.Alertas.Add("Sin código 606 equivalente: complete en Oficina Virtual si aplica.");
                casilla.Alertas.AddRange(acc.alertas);
                otras.Add(casilla);
            }

            var c25imp = R(otras.Sum(c => c.Impuesto));
            var c25base = R(otras.Sum(c => c.MontoImponible));
            otras.Add(Casilla(25, "C25", "TOTAL OTRAS RETENCIONES (1 a 24)", "II. Otras retenciones",
                impuesto: c25imp, imponible: c25base, origen: "FORMULA", calculada: true,
                formula: "1+2+…+24"));

            var c26 = Casilla(26, "C26",
                "OTRAS RETENCIONES EN CONVENIOS Y ACUERDOS INTERNACIONALES (casilla 35 anexo R9C)",
                "III. Convenios", origen: "R9C", calculada: true, editable: true,
                formula: "Viene de R9C casilla 35",
                alerta: "Presente el Anexo R9C antes del IR-17. Si no hay operaciones de convenio, envíelo en cero.");
            otras.Add(c26);

            var c27imp = R(c25imp + c26.Impuesto);
            var c27base = R(c25base + c26.MontoImponible);
            otras.Add(Casilla(27, "C27", "TOTAL DE OTRAS RETENCIONES (25+26)", "IV. Total otras retenciones",
                impuesto: c27imp, imponible: c27base, origen: "FORMULA", calculada: true,
                formula: "25+26"));

            var c28 = Casilla(28, "C28", "RETRIBUCIONES COMPLEMENTARIAS", "V. Retribuciones complementarias",
                tasa: 0.27m, origen: "MANUAL_PENDIENTE", editable: true,
                alerta: "Bienes/servicios/beneficios en especie al empleado. No se infiere del 606.");

            var c29 = R(c27imp + c28.Impuesto);
            var c30 = 0m;
            var c31 = 0m;
            var c32 = 0m;
            var diferencia = R(c29 - c30 - c31 - c32);
            var c33 = diferencia > 0 ? diferencia : 0m;
            var c34 = diferencia < 0 ? R(-diferencia) : 0m;
            var c35 = 0m;
            var c36 = 0m;
            var c37 = R(c33 + c35 + c36);

            var liquidacion = new List<Ir17CasillaDto>
            {
                c28,
                Casilla(29, "C29", "IMPUESTO A PAGAR (27+28)", "VI. Liquidación",
                    impuesto: c29, origen: "FORMULA", calculada: true, formula: "27+28"),
                Casilla(30, "C30", "SALDOS COMPENSABLES AUTORIZADOS (Otros impuestos)", "VI. Liquidación",
                    origen: "MANUAL_PENDIENTE", editable: true),
                Casilla(31, "C31", "PAGOS COMPUTABLES A CUENTA", "VI. Liquidación",
                    origen: "MANUAL_PENDIENTE", editable: true),
                Casilla(32, "C32", "SALDO A FAVOR ANTERIOR", "VI. Liquidación",
                    origen: "MANUAL_PENDIENTE", editable: true),
                Casilla(33, "C33", "DIFERENCIA A PAGAR (si 29-30-31-32 es positivo)", "VI. Liquidación",
                    impuesto: c33, origen: "FORMULA", calculada: true, formula: "MAX(0, 29-30-31-32)"),
                Casilla(34, "C34", "NUEVO SALDO A FAVOR (si 29-30-31-32 es negativo)", "VI. Liquidación",
                    impuesto: c34, origen: "FORMULA", calculada: true, formula: "MAX(0, 30+31+32-29)"),
                Casilla(35, "C35", "RECARGOS", "VII. Penalidades",
                    origen: "MANUAL_PENDIENTE", editable: true,
                    alerta: "Desde julio 2026: 3% por mes o fracción (Ley 30-26), tope 100% del tributo."),
                Casilla(36, "C36", "INTERÉS INDEMNIZATORIO", "VII. Penalidades",
                    origen: "MANUAL_PENDIENTE", editable: true,
                    alerta: "1.10% acumulativo por mes o fracción, según DGII."),
                Casilla(37, "C37", "TOTAL A PAGAR (33+35+36)", "VIII. Monto a pagar",
                    impuesto: c37, origen: "FORMULA", calculada: true, formula: "33+35+36")
            };

            var fechaLimite = new DateTime(h.Year, h.Month, 1).AddMonths(1).AddDays(9); // día 10 mes siguiente

            var rnc = Limpiar(empresa?.RNC);
            var dto = new ReporteIr17Dto
            {
                IdEmpresa = idEmpresa,
                RncEmpresa = rnc,
                RazonSocial = FirstNonEmpty(cfg?.RazonSocial, empresa?.NombreComercial),
                NombreComercial = empresa?.NombreComercial,
                CorreoElectronico = empresa?.CorreElectronico,
                Telefono = empresa?.Telefono,
                Periodo = periodoTxt,
                Desde = d,
                Hasta = h,
                FechaLimitePago = fechaLimite,
                TipoDeclaracion = "Original",
                VersionInstructivo = "IR-17-2026",
                CantidadRetenciones = lineas.Count,
                TotalMontoImponible = c27base,
                TotalOtrasRetenciones = c27imp,
                ImpuestoAPagar = c33,
                SaldoAFavor = c34,
                TotalGeneralAPagar = c37,
                AlertasGlobales = alertasGlobales,
                OtrasRetenciones = otras,
                Liquidacion = liquidacion,
                LineasOrigen = lineas.OrderBy(x => x.Casilla).ThenBy(x => x.FechaPagoFiscal).ToList()
            };

            dto.CantidadAlertas = dto.AlertasGlobales.Count
                + dto.OtrasRetenciones.Sum(c => c.Alertas.Count)
                + dto.Liquidacion.Sum(c => c.Alertas.Count);
            dto.ContenidoCsv = GenerarCsv(dto);
            return dto;
        }

        /// <summary>606 campo 17 → casilla IR-17 2026.</summary>
        private static readonly Dictionary<int, int> Mapa606 = new()
        {
            [1] = 1,  // Alquileres → C1 15%
            [2] = 2,  // Honorarios → C2 15%
            [3] = 16, // Otras rentas → C16 15% (Art. 309 / Ley 30-26)
            [4] = 17, // Rentas presuntas → C17 3% (Decreto 139-98)
            [5] = 19, // Intereses PJ residentes → C19 1% (entidades financieras)
            [6] = 11, // Intereses PF residentes → C11 10% (no financieras)
            [7] = 12, // Proveedores del Estado → C12 5%
            [8] = 13, // Juegos telefónicos → C13 5%
            [9] = 21  // Ganadería carne bovina → C21 1%
        };

        private static List<Ir17CasillaDto> CatalogoCasillas() => new()
        {
            Def(1, "ALQUILERES", 0.15m, 1),
            Def(2, "HONORARIOS POR SERVICIOS INDEPENDIENTES", 0.15m, 2),
            Def(3, "PREMIOS (Ley 253-12)", 0.25m, null),
            Def(4, "TRANSFERENCIA DE TÍTULO Y PROPIEDADES", 0.02m, null),
            Def(5, "DIVIDENDOS (Ley 253-12)", 0.10m, null),
            Def(6, "INTERESES A PERSONAS JURÍDICAS O ENTIDADES NO RESIDENTES (Ley 253-12)", 0.10m, null),
            Def(7, "INTERESES A PERSONAS JURÍDICAS O ENTIDADES NO RESIDENTES (Ley 57-2007)", 0.05m, null),
            Def(8, "INTERESES A PERSONAS FÍSICAS NO RESIDENTES (Ley 253-12)", 0.10m, null),
            Def(9, "INTERESES A PERSONAS FÍSICAS NO RESIDENTES (Leyes 57-2007 y 253-12)", 0.05m, null),
            Def(10, "REMESAS AL EXTERIOR (Ley 253-12)", 0.27m, null),
            Def(11, "INTERESES PAGADOS POR ENTIDADES NO FINANCIERAS A PERSONAS FÍSICAS RESIDENTES", 0.10m, 6),
            Def(12, "PAGOS A PROVEEDORES DEL ESTADO (Ley 253-12)", 0.05m, 7),
            Def(13, "JUEGOS TELEFÓNICOS (Norma 08-2011)", 0.05m, 8),
            Def(14, "GANANCIA DE CAPITAL (Norma 07-2011)", 0.01m, null),
            Def(15, "JUEGOS VÍA INTERNET (Ley 139-11, Art. 7)", 0.10m, null),
            Def(16, "OTRAS RENTAS (Ley 11-92, Art. 309 Lit. f modificado por Ley 30-26)", 0.15m, 3),
            Def(17, "OTRAS RENTAS (Decreto 139-98, Art. 70 Lit. a y b)", 0.03m, 4),
            Def(18, "OTRAS RETENCIONES (Norma 07-2007 modificado por Ley 30-26)", 0.03m, null),
            Def(19, "INTERESES PAGADOS POR ENTIDADES FINANCIERAS A PERSONAS JURÍDICAS RESIDENTES (Norma 13-2011)", 0.01m, 5),
            Def(20, "INTERESES PAGADOS POR ENTIDADES FINANCIERAS A PERSONAS FÍSICAS RESIDENTES (Ley 253-12)", 0.10m, null),
            Def(21, "ADQUISICIÓN DE BIENES DE PERSONAS FÍSICAS DEDICADAS AL SUBSECTOR DE GANADERÍA DE CARNE BOVINA (NORMA 04-25)", 0.01m, 9),
            Def(22, "REGALÍAS O DERECHOS PAGADOS A NO RESIDENTES (Art. 305-1 Ley 30-26)", 0.15m, null),
            Def(23, "LICENCIAS DE SOFTWARE, PUBLICIDAD EN LÍNEA Y ALMACENAMIENTO DE DATOS A NO RESIDENTES (Art. 305-2 Ley 30-26)", 0.15m, null),
            Def(24, "REMESAS AL EXTERIOR (Mayores a 1,000 millones - Ley 30-26)", 0.30m, null),
        };

        private static Ir17CasillaDto Def(int num, string etiqueta, decimal tasa, int? tipo606) =>
            new()
            {
                Numero = num,
                Codigo = $"C{num}",
                Etiqueta = etiqueta,
                Seccion = "II. Otras retenciones",
                Tasa = tasa,
                TipoRetencionIsr606 = tipo606,
                Origen = tipo606.HasValue ? "AUTO_606" : "MANUAL_PENDIENTE"
            };

        private static Ir17CasillaDto Clone(Ir17CasillaDto d) => new()
        {
            Numero = d.Numero,
            Codigo = d.Codigo,
            Etiqueta = d.Etiqueta,
            Seccion = d.Seccion,
            Tasa = d.Tasa,
            TipoRetencionIsr606 = d.TipoRetencionIsr606,
            Origen = d.Origen
        };

        private static Ir17CasillaDto Casilla(
            int num, string codigo, string etiqueta, string seccion,
            decimal impuesto = 0, decimal imponible = 0, decimal? tasa = null,
            string origen = "FORMULA", bool calculada = false, bool editable = false,
            string? formula = null, string? alerta = null)
        {
            var c = new Ir17CasillaDto
            {
                Numero = num,
                Codigo = codigo,
                Etiqueta = etiqueta,
                Seccion = seccion,
                Tasa = tasa,
                MontoImponible = imponible,
                Impuesto = impuesto,
                Origen = origen,
                EsCalculada = calculada,
                EsEditableUsuario = editable,
                FormulaAplicada = formula
            };
            if (!string.IsNullOrWhiteSpace(alerta))
                c.Alertas.Add(alerta);
            return c;
        }

        private static decimal MontoImponible(Entities.Domain.OrdenCompraHeader o)
        {
            var neto = o.MontoFacturadoServicios + o.MontoFacturadoBienes;
            if (neto > 0) return R(neto);
            var alt = o.Total - o.TotalItbis;
            return R(alt > 0 ? alt : o.Total);
        }

        private static (DateTime d, DateTime h, string periodo) ResolverRango(
            DateTime? desde, DateTime? hasta, string? periodo)
        {
            string periodoTxt;
            DateTime d;
            DateTime h;

            if (!string.IsNullOrWhiteSpace(periodo) && periodo!.Trim().Length == 6
                && int.TryParse(periodo.Trim().Substring(0, 4), out var y)
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

        private static decimal R(decimal v) =>
            Math.Round(v, 2, MidpointRounding.AwayFromZero);

        private static string Limpiar(string? s) =>
            string.IsNullOrWhiteSpace(s) ? "" : new string(s.Where(char.IsDigit).ToArray());

        private static string? FirstNonEmpty(params string?[] values) =>
            values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        private static string GenerarCsv(ReporteIr17Dto dto)
        {
            var sb = new StringBuilder();
            sb.AppendLine("FORMULARIO;NUMERO;CODIGO;SECCION;ETIQUETA;TASA;CANTIDAD;MONTO_IMPONIBLE;IMPUESTO;ORIGEN;FORMULA;ALERTAS");
            void Write(string form, Ir17CasillaDto c)
            {
                sb.Append(form).Append(';')
                    .Append(c.Numero).Append(';')
                    .Append(c.Codigo).Append(';')
                    .Append(Esc(c.Seccion)).Append(';')
                    .Append(Esc(c.Etiqueta)).Append(';')
                    .Append(c.Tasa?.ToString("0.####", Inv) ?? "").Append(';')
                    .Append(c.Cantidad?.ToString(Inv) ?? "").Append(';')
                    .Append(c.MontoImponible.ToString("0.00", Inv)).Append(';')
                    .Append(c.Impuesto.ToString("0.00", Inv)).Append(';')
                    .Append(c.Origen).Append(';')
                    .Append(Esc(c.FormulaAplicada)).Append(';')
                    .Append(Esc(string.Join(" | ", c.Alertas)))
                    .AppendLine();
            }

            foreach (var c in dto.OtrasRetenciones) Write("IR-17", c);
            foreach (var c in dto.Liquidacion) Write("IR-17-LIQ", c);

            sb.AppendLine();
            sb.AppendLine("ORIGEN;ID_FACTC;NCF;DOCUMENTO;TIPO_606;CASILLA;MONTO_IMPONIBLE;IMPUESTO;FECHA_PAGO");
            foreach (var l in dto.LineasOrigen)
            {
                sb.Append("606").Append(';')
                    .Append(l.IdOrdenCompraHeader).Append(';')
                    .Append(Esc(l.Ncf)).Append(';')
                    .Append(Esc(l.NumeroDocumento)).Append(';')
                    .Append(l.TipoRetencionIsr?.ToString(Inv) ?? "").Append(';')
                    .Append(l.Casilla).Append(';')
                    .Append(l.MontoImponible.ToString("0.00", Inv)).Append(';')
                    .Append(l.Impuesto.ToString("0.00", Inv)).Append(';')
                    .Append(l.FechaPagoFiscal?.ToString("yyyy-MM-dd", Inv) ?? "")
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
