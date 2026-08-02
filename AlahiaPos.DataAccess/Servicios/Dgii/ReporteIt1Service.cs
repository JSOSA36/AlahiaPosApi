using System.Globalization;
using System.Text;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios.Dgii
{
    /// <summary>
    /// Motor liquidación IT-1 2020 + Anexo A (V1 preview en vivo).
    /// Consolida agregados 607/606 y fotos fiscales; no persiste periodo.
    /// </summary>
    public class ReporteIt1Service : IReporteIt1Service
    {
        private const int TipoDocumentoFacturaCompra = 11;
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        private readonly AlahiaPosContext _ctx;
        private readonly IReporte607Service _reporte607;

        public ReporteIt1Service(AlahiaPosContext ctx, IReporte607Service reporte607)
        {
            _ctx = ctx;
            _reporte607 = reporte607;
        }

        public async Task<ReporteIt1Dto> ObtenerAsync(
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

            var reporte607 = await _reporte607.ObtenerReporte607Async(idEmpresa, d, h, periodoTxt);

            var facturas = await _ctx.FacturaHeaders.AsNoTracking()
                .Where(x =>
                    x.IdEmpresa == idEmpresa
                    && x.IdTipoDocumentos == 1
                    && x.FechaInseccion.Date >= d
                    && x.FechaInseccion.Date <= h
                    && x.NCF != null && x.NCF != ""
                    && (
                        !x.EstaCancelada
                        || (x.MotivoAnulacion != null
                            && x.MotivoAnulacion.ToLower().Contains("nota de cr"))
                    ))
                .ToListAsync();

            var notas = await _ctx.NotasCredito.AsNoTracking()
                .Where(n =>
                    n.IdEmpresa == idEmpresa
                    && n.Estado != NotaCreditoEstado.Anulada
                    && n.FechaInseccion.Date >= d
                    && n.FechaInseccion.Date <= h
                    && n.NCF != null && n.NCF != "")
                .ToListAsync();

            var compras = await _ctx.OrdenCompraHeaders.AsNoTracking()
                .Where(o =>
                    o.IdEmpresa == idEmpresa
                    && o.IdTipoDocumentos == TipoDocumentoFacturaCompra
                    && o.Estado != "BORRADOR"
                    && o.Estado != "ANULADA"
                    && o.FechaInseccion.Date >= d
                    && o.FechaInseccion.Date <= h)
                .ToListAsync();

            var alertasGlobales = new List<string>();
            if (cfg == null || !cfg.FiscalActivo || !cfg.GenerarIt1)
                alertasGlobales.Add("GenerarIt1 no está activo en la configuración fiscal de la empresa.");

            var comprasConItbis = compras.Where(c => c.TotalItbis > 0).ToList();
            var sinClasificar = comprasConItbis.Count(c => !c.ClasificacionConfirmada || c.DestinoItbis == null);
            if (sinClasificar > 0)
            {
                alertasGlobales.Add(
                    $"{sinClasificar} factura(s) de compra con ITBIS sin Destino ITBIS confirmado (Anexo A 45–53).");
            }

            // --- Anexo A buckets ---
            var a1_8 = AgregarPorTipoNcf(reporte607.Lineas);
            var a9 = Casilla(9, "A9", "Otras operaciones (positivas)", "II",
                origen: "MANUAL_PENDIENTE", editable: true,
                alerta: "Completar manualmente si aplica (muestras, faltantes, NC >30 días que afecten el periodo).");
            var a10 = Casilla(10, "A10", "Otras operaciones (negativas)", "II",
                origen: "MANUAL_PENDIENTE", editable: true,
                alerta: "Completar manualmente si aplica (conceptos no sujetos incluidos en NCF).");

            var a11Monto = R(a1_8[1].Monto + a1_8[2].Monto + a1_8[3].Monto - a1_8[4].Monto
                + a1_8[5].Monto + a1_8[6].Monto + a1_8[7].Monto + a1_8[8].Monto
                + a9.Monto - a10.Monto);
            var a11Cant = (a1_8[1].Cantidad ?? 0) + (a1_8[2].Cantidad ?? 0) + (a1_8[3].Cantidad ?? 0)
                - (a1_8[4].Cantidad ?? 0) + (a1_8[5].Cantidad ?? 0) + (a1_8[6].Cantidad ?? 0)
                + (a1_8[7].Cantidad ?? 0) + (a1_8[8].Cantidad ?? 0)
                + (a9.Cantidad ?? 0) - (a10.Cantidad ?? 0);
            var a11 = Casilla(11, "A11", "Total operaciones", "II",
                cantidad: a11Cant, monto: a11Monto, origen: "FORMULA", calculada: true,
                formula: "1+2+3-4+5+6+7+8+9-10");

            var ventas607 = reporte607.Lineas.Where(l => l.TipoDocumentoAlahia == "Venta").ToList();
            var a12 = Casilla(12, "A12", "Efectivo", "III",
                monto: R(ventas607.Sum(l => l.Efectivo)), origen: "AUTO_607", calculada: true);
            var a13 = Casilla(13, "A13", "Cheque / Transferencia", "III",
                monto: R(ventas607.Sum(l => l.ChequeTransferenciaDeposito)), origen: "AUTO_607", calculada: true);
            var a14 = Casilla(14, "A14", "Tarjeta débito / crédito", "III",
                monto: R(ventas607.Sum(l => l.TarjetaDebitoCredito)), origen: "AUTO_607", calculada: true);
            var a15 = Casilla(15, "A15", "A crédito", "III",
                monto: R(ventas607.Sum(l => l.VentaCredito)), origen: "AUTO_607", calculada: true);
            var a16 = Casilla(16, "A16", "Bonos o certificado de regalo", "III",
                monto: R(ventas607.Sum(l => l.BonosCertificados)), origen: "AUTO_607", calculada: true);
            var a17 = Casilla(17, "A17", "Permutas", "III",
                monto: R(ventas607.Sum(l => l.Permuta)), origen: "AUTO_607", calculada: true);
            var a18 = Casilla(18, "A18", "Otras formas de venta", "III",
                monto: R(ventas607.Sum(l => l.OtrasFormasVenta)), origen: "AUTO_607", calculada: true);
            var a19 = Casilla(19, "A19", "Total operaciones por tipo de venta", "III",
                monto: R(a12.Monto + a13.Monto + a14.Monto + a15.Monto + a16.Monto + a17.Monto + a18.Monto),
                origen: "FORMULA", calculada: true, formula: "12+13+14+15+16+17+18");

            var ingresos = AgregarPorTipoIngreso(reporte607.Lineas);
            var a20 = ingresos[1];
            var a21 = ingresos[2];
            var a22 = ingresos[3];
            var a23 = ingresos[4];
            var a24 = ingresos[5];
            var a25 = ingresos[6];
            var a26 = Casilla(26, "A26", "Total por tipo de ingreso", "IV",
                monto: R(a20.Monto + a21.Monto + a22.Monto + a23.Monto + a24.Monto + a25.Monto),
                origen: "FORMULA", calculada: true, formula: "20+21+22+23+24+25");

            // Retenciones sufridas: sin foto dedicada → 0 + alerta
            var a27 = Casilla(27, "A27", "Pagos computables por retenciones (Norma 08-04)", "V",
                origen: "MANUAL_PENDIENTE", editable: true,
                alerta: "Registrar retención 08-04 sufrida si aplica.");
            var a28 = Casilla(28, "A28", "Pagos computables pasajes aéreos (Norma 02-05)", "V",
                origen: "MANUAL_PENDIENTE", editable: true);
            var a29 = Casilla(29, "A29", "Pagos computables por otras retenciones (Norma 02-05)", "V",
                origen: "MANUAL_PENDIENTE", editable: true);
            var a30 = Casilla(30, "A30", "Pagos computables paquetes alojamiento", "V",
                origen: "MANUAL_PENDIENTE", editable: true);
            var a31 = Casilla(31, "A31", "Crédito por retención entidades del Estado", "V",
                origen: "MANUAL_PENDIENTE", editable: true);
            var a32 = Casilla(32, "A32", "Pagos computables por ITBIS percibido", "V",
                origen: "MANUAL_PENDIENTE", editable: true);
            var a33 = Casilla(33, "A33", "Total pagos computables por retenciones/percepción", "V",
                monto: R(a27.Monto + a28.Monto + a29.Monto + a30.Monto + a31.Monto + a32.Monto),
                origen: "FORMULA", calculada: true, formula: "27+28+29+30+31+32");

            var esConstructor = cfg?.EsConstructor == true;
            var esComisionista = cfg?.EsComisionista == true;
            var a34 = CasillaVertical(34, "A34", "Dirección técnica (constructoras)", "VI", esConstructor);
            var a35 = CasillaVertical(35, "A35", "Contrato de administración (constructoras)", "VI", esConstructor);
            var a36 = CasillaVertical(36, "A36", "Asesorías / honorarios (constructoras)", "VI", esConstructor);
            var a37 = Casilla(37, "A37", "Total operaciones constructoras", "VI",
                monto: R(a34.Monto + a35.Monto + a36.Monto),
                origen: esConstructor ? "FORMULA" : "NO_APLICA", calculada: true, formula: "34+35+36");
            var a38 = Casilla(38, "A38", "Operaciones no sujetas ITBIS por construcción", "VI",
                origen: esConstructor ? "FORMULA" : "NO_APLICA", calculada: true);

            var a39 = CasillaVertical(39, "A39", "Ventas de bienes por comisión", "VII", esComisionista);
            var a40 = CasillaVertical(40, "A40", "Ventas de servicios en nombre de terceros", "VII", esComisionista);
            var a41 = Casilla(41, "A41", "Total operaciones comisionistas", "VII",
                monto: R(a39.Monto + a40.Monto),
                origen: esComisionista ? "FORMULA" : "NO_APLICA", calculada: true, formula: "39+40");
            var a42 = Casilla(42, "A42", "Operaciones no sujetas ITBIS por comisiones", "VII",
                origen: esComisionista ? "FORMULA" : "NO_APLICA", calculada: true);

            var ncMas30 = notas.Where(n =>
            {
                var origen = n.FechaFacturaOrigen?.Date;
                if (!origen.HasValue) return false;
                return (n.FechaInseccion.Date - origen.Value).TotalDays > 30;
            }).ToList();
            var a43 = Casilla(43, "A43", "NC emitidas con más de 30 días desde la facturación", "VIII",
                cantidad: ncMas30.Count,
                monto: R(ncMas30.Sum(n => MontoBaseNc(n))),
                origen: "AUTO_607", calculada: true);

            var comprasRegimen = compras.Where(c => EsNcfRegimenEspecial(c.NCF) ||
                string.Equals(c.RegimenFiscalProveedorCodigo, "ESPECIAL", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var a44 = Casilla(44, "A44", "Facturas en comprobantes para regímenes especiales (606)", "VIII",
                cantidad: comprasRegimen.Count,
                monto: R(comprasRegimen.Sum(c => c.Total - c.TotalItbis)),
                origen: "AUTO_606", calculada: true);

            var destinos = AgregarItbisPorDestino(compras);
            var a45 = destinos[1];
            var a46 = destinos[2];
            var a47 = destinos[3];
            var a48 = SumDestino(48, "A48", "Total ITBIS no deducible (45+46+47)", "IX", a45, a46, a47, "45+46+47");
            var a49 = destinos[4];
            var a50 = destinos[5];
            var a51 = destinos[6];
            var a52 = SumDestino(52, "A52", "Total ITBIS deducible no sujeto a proporcionalidad (49+50+51)", "IX",
                a49, a50, a51, "49+50+51");
            var a53 = destinos[7];

            // Coeficiente usa IT-1 2,5,10 / IT-1 1 → se calcula después de armar IT-1 bases;
            // placeholder A54-56, se actualizan abajo.
            var a54 = Casilla(54, "A54", "Coeficiente de proporcionalidad %", "IX",
                origen: "FORMULA", calculada: true, formula: "((IT1.2+IT1.5+IT1.10)/IT1.1)*100");
            var a55 = Casilla(55, "A55", "ITBIS admitido por proporcionalidad (53×54)", "IX",
                origen: "FORMULA", calculada: true, formula: "53*54",
                conColumnas: true);
            var a56 = Casilla(56, "A56", "Total ITBIS deducible (52+55)", "IX",
                origen: "FORMULA", calculada: true, formula: "52+55", conColumnas: true);

            // --- IT-1 ingresos ---
            var clasif = ClasificarVentasIt1(facturas, notas, a38.Monto, a42.Monto);

            var it1_1 = Casilla(1, "I1", "Total de operaciones del periodo (Anexo A 11)", "II",
                monto: a11.Monto, origen: "FORMULA", calculada: true, formula: "=AnexoA.11");
            var it1_2 = Casilla(2, "I2", "Ingresos por exportaciones de bienes", "II.A",
                monto: clasif.ExportBienes, origen: clasif.ExportBienes > 0 ? "AUTO_607" : "MANUAL_PENDIENTE",
                editable: true);
            var it1_3 = Casilla(3, "I3", "Ingresos por exportaciones de servicios", "II.A",
                monto: clasif.ExportServicios, origen: clasif.ExportServicios > 0 ? "AUTO_607" : "MANUAL_PENDIENTE",
                editable: true);
            var it1_4 = Casilla(4, "I4", "Ventas locales exentas Art. 343/344", "II.A",
                monto: clasif.ExentoLocal, origen: "AUTO_607", calculada: true);
            var it1_5 = Casilla(5, "I5", "Ventas exentas por destino", "II.A",
                monto: clasif.ExentoDestino, origen: clasif.ExentoDestino > 0 ? "AUTO_607" : "MANUAL_PENDIENTE",
                editable: true);
            var it1_6 = Casilla(6, "I6", "No sujetas por servicios de construcción (Anexo A 38)", "II.A",
                monto: a38.Monto, origen: "FORMULA", calculada: true, formula: "=AnexoA.38");
            var it1_7 = Casilla(7, "I7", "No sujetas por comisiones (Anexo A 42)", "II.A",
                monto: a42.Monto, origen: "FORMULA", calculada: true, formula: "=AnexoA.42");
            var it1_8 = Casilla(8, "I8", "Ventas locales bienes exentos Art. 343 III/IV", "II.A",
                monto: clasif.Exento343P3, origen: "MANUAL_PENDIENTE", editable: true);
            var it1_9 = Casilla(9, "I9", "Total ingresos por operaciones no gravadas", "II.A",
                monto: R(it1_2.Monto + it1_3.Monto + it1_4.Monto + it1_5.Monto + it1_6.Monto + it1_7.Monto + it1_8.Monto),
                origen: "FORMULA", calculada: true, formula: "2+3+4+5+6+7+8");
            var it1_10 = Casilla(10, "I10", "Total ingresos por operaciones gravadas", "II.B",
                monto: R(it1_1.Monto - it1_9.Monto), origen: "FORMULA", calculada: true, formula: "1-9");

            var it1_11 = Casilla(11, "I11", "Operaciones gravadas al 18%", "II.B",
                monto: clasif.Gravado18, origen: "AUTO_607", calculada: true);
            var it1_12 = Casilla(12, "I12", "Operaciones gravadas al 16%", "II.B",
                monto: clasif.Gravado16, origen: "AUTO_607", calculada: true);
            var it1_13 = Casilla(13, "I13", "Operaciones gravadas al 9%", "II.B",
                monto: clasif.Gravado9, origen: "AUTO_607", calculada: true);
            var it1_14 = Casilla(14, "I14", "Operaciones gravadas al 8%", "II.B",
                monto: clasif.Gravado8, origen: "AUTO_607", calculada: true);
            var it1_15 = Casilla(15, "I15", "Operaciones gravadas por venta de activos depreciables", "II.B",
                monto: clasif.ActivosDep, origen: clasif.ActivosDep > 0 ? "AUTO_607" : "MANUAL_PENDIENTE",
                editable: true);

            // Cuadre suave: si gravadas tipificadas ≠ casilla 10, empujar diferencia a 18%
            var sumaGrav = it1_11.Monto + it1_12.Monto + it1_13.Monto + it1_14.Monto + it1_15.Monto;
            if (Math.Abs(sumaGrav - it1_10.Monto) > 0.05m && it1_10.Monto > 0)
            {
                if (sumaGrav == 0)
                {
                    it1_11.Monto = it1_10.Monto;
                    it1_11.Alertas.Add("Asignado a 18% por falta de desglose de tasas en fotos fiscales.");
                }
                else
                {
                    it1_11.Monto = R(it1_11.Monto + (it1_10.Monto - sumaGrav));
                    it1_11.Alertas.Add("Ajuste de cuadre entre casilla 10 y desglose de tasas.");
                }
            }

            var it1_16 = Casilla(16, "I16", "ITBIS cobrado (18% de casilla 11)", "III",
                monto: R(it1_11.Monto * 0.18m), origen: "FORMULA", calculada: true, formula: "11*18%");
            var it1_17 = Casilla(17, "I17", "ITBIS cobrado (16% de casilla 12)", "III",
                monto: R(it1_12.Monto * 0.16m), origen: "FORMULA", calculada: true, formula: "12*16%");
            var it1_18 = Casilla(18, "I18", "ITBIS cobrado (9% de casilla 13)", "III",
                monto: R(it1_13.Monto * 0.09m), origen: "FORMULA", calculada: true, formula: "13*9%");
            var it1_19 = Casilla(19, "I19", "ITBIS cobrado (8% de casilla 14)", "III",
                monto: R(it1_14.Monto * 0.08m), origen: "FORMULA", calculada: true, formula: "14*8%");
            var it1_20 = Casilla(20, "I20", "ITBIS cobrado por ventas de activos (18% de 15)", "III",
                monto: R(it1_15.Monto * 0.18m), origen: "FORMULA", calculada: true, formula: "15*18%");
            var it1_21 = Casilla(21, "I21", "Total ITBIS cobrado", "III",
                monto: R(it1_16.Monto + it1_17.Monto + it1_18.Monto + it1_19.Monto + it1_20.Monto),
                origen: "FORMULA", calculada: true, formula: "16+17+18+19+20");

            // Actualizar A54-56 con casillas IT-1
            var coef = 0m;
            if (it1_1.Monto > 0)
                coef = R(((it1_2.Monto + it1_5.Monto + it1_10.Monto) / it1_1.Monto) * 100m);
            a54.Monto = coef;
            var admitido = R(a53.Monto * (coef / 100m));
            a55.Monto = admitido;
            a55.MontoLocal = R((a53.MontoLocal ?? 0) * (coef / 100m));
            a55.MontoServicios = R((a53.MontoServicios ?? 0) * (coef / 100m));
            a55.MontoImportaciones = R((a53.MontoImportaciones ?? 0) * (coef / 100m));
            a56.Monto = R(a52.Monto + a55.Monto);
            a56.MontoLocal = R((a52.MontoLocal ?? 0) + (a55.MontoLocal ?? 0));
            a56.MontoServicios = R((a52.MontoServicios ?? 0) + (a55.MontoServicios ?? 0));
            a56.MontoImportaciones = R((a52.MontoImportaciones ?? 0) + (a55.MontoImportaciones ?? 0));

            var it1_22 = Casilla(22, "I22", "ITBIS pagado en compras locales (Anexo A 56 local)", "III",
                monto: a56.MontoLocal ?? 0, origen: "FORMULA", calculada: true, formula: "=AnexoA.56.local");
            var it1_23 = Casilla(23, "I23", "ITBIS pagado por servicios deducibles (Anexo A 56 serv.)", "III",
                monto: a56.MontoServicios ?? 0, origen: "FORMULA", calculada: true, formula: "=AnexoA.56.serv");
            var it1_24 = Casilla(24, "I24", "ITBIS pagado en importaciones (Anexo A 56 imp.)", "III",
                monto: a56.MontoImportaciones ?? 0, origen: "FORMULA", calculada: true, formula: "=AnexoA.56.imp");
            var it1_25 = Casilla(25, "I25", "Total ITBIS deducible", "III",
                monto: R(it1_22.Monto + it1_23.Monto + it1_24.Monto),
                origen: "FORMULA", calculada: true, formula: "22+23+24");

            var diff2125 = it1_21.Monto - it1_25.Monto;
            var it1_26 = Casilla(26, "I26", "Impuesto a pagar", "III",
                monto: diff2125 > 0 ? R(diff2125) : 0, origen: "FORMULA", calculada: true, formula: "max(21-25,0)");
            var it1_27 = Casilla(27, "I27", "Saldo a favor", "III",
                monto: diff2125 < 0 ? R(-diff2125) : 0, origen: "FORMULA", calculada: true, formula: "max(25-21,0)");

            var it1_28 = Casilla(28, "I28", "Saldos compensables autorizados / reembolsos", "III",
                origen: "MANUAL_PENDIENTE", editable: true);
            var it1_29 = Casilla(29, "I29", "Saldo a favor anterior", "III",
                origen: "MANUAL_PENDIENTE", editable: true,
                alerta: "Traer del periodo anterior cuando exista snapshot DgiiIt1Periodo.");
            var it1_30 = Casilla(30, "I30", "Total pagos computables por retenciones (Anexo A 33)", "III",
                monto: a33.Monto, origen: "FORMULA", calculada: true, formula: "=AnexoA.33");
            var it1_31 = Casilla(31, "I31", "Otros pagos computables a cuenta", "III",
                origen: "MANUAL_PENDIENTE", editable: true);
            var it1_32 = Casilla(32, "I32", "Compensaciones y/o reembolsos autorizados", "III",
                origen: "MANUAL_PENDIENTE", editable: true);

            var baseDiff = it1_26.Monto - it1_28.Monto - it1_29.Monto - it1_30.Monto - it1_31.Monto - it1_32.Monto;
            var it1_33 = Casilla(33, "I33", "Diferencia a pagar", "III",
                monto: baseDiff > 0 ? R(baseDiff) : 0, origen: "FORMULA", calculada: true,
                formula: "si(26-28-29-30-31-32)>0");
            var it1_34 = Casilla(34, "I34", "Nuevo saldo a favor", "III",
                monto: baseDiff < 0
                    ? R(-baseDiff)
                    : R(it1_27.Monto + it1_28.Monto + it1_29.Monto + it1_30.Monto + it1_31.Monto + it1_32.Monto),
                origen: "FORMULA", calculada: true);

            var it1_35 = Casilla(35, "I35", "Recargos", "IV", origen: "MANUAL_PENDIENTE", editable: true);
            var it1_36 = Casilla(36, "I36", "Interés indemnizatorio", "IV", origen: "MANUAL_PENDIENTE", editable: true);
            var it1_37 = Casilla(37, "I37", "Sanciones", "IV", origen: "MANUAL_PENDIENTE", editable: true);
            var it1_38 = Casilla(38, "I38", "Total a pagar (ITBIS)", "V",
                monto: R(it1_33.Monto + it1_35.Monto + it1_36.Monto + it1_37.Monto),
                origen: "FORMULA", calculada: true, formula: "33+35+36+37");

            // Sección A — retenciones practicadas
            var ret = AgregarRetencionesPracticadas(compras);
            var it1_39 = Casilla(39, "I39", "Servicios sujetos a retención personas físicas", "A",
                monto: ret.BasePf, origen: ret.BasePf > 0 ? "AUTO_606" : "MANUAL_PENDIENTE", editable: true);
            var it1_40 = Casilla(40, "I40", "Servicios sujetos a retención entidades no lucrativas (01-11)", "A",
                monto: ret.BaseIsfl, origen: ret.BaseIsfl > 0 ? "AUTO_606" : "MANUAL_PENDIENTE", editable: true);
            var it1_41 = Casilla(41, "I41", "Total servicios retención PF / ISFL", "A",
                monto: R(it1_39.Monto + it1_40.Monto), origen: "FORMULA", calculada: true, formula: "39+40");
            var it1_42 = Casilla(42, "I42", "Servicios retención sociedades (Norma 07-09)", "A",
                monto: ret.BaseSoc0709, origen: ret.BaseSoc0709 > 0 ? "AUTO_606" : "MANUAL_PENDIENTE", editable: true);
            var it1_43 = Casilla(43, "I43", "Servicios retención sociedades (02-05 / 07-07)", "A",
                monto: ret.BaseSoc0205, origen: ret.BaseSoc0205 > 0 ? "AUTO_606" : "MANUAL_PENDIENTE", editable: true);
            var it1_44 = Casilla(44, "I44", "Retención RST gravadas 18%", "A",
                monto: ret.BaseRst18, origen: ret.BaseRst18 > 0 ? "AUTO_606" : "MANUAL_PENDIENTE", editable: true);
            var it1_45 = Casilla(45, "I45", "Retención RST gravadas 16%", "A",
                monto: ret.BaseRst16, origen: ret.BaseRst16 > 0 ? "AUTO_606" : "MANUAL_PENDIENTE", editable: true);
            var it1_46 = Casilla(46, "I46", "Total bases RST", "A",
                monto: R(it1_44.Monto + it1_45.Monto), origen: "FORMULA", calculada: true, formula: "44+45");
            var it1_47 = Casilla(47, "I47", "Bienes retención comprobante de compras 18%", "A",
                monto: ret.BaseCompra18, origen: ret.BaseCompra18 > 0 ? "AUTO_606" : "MANUAL_PENDIENTE", editable: true);
            var it1_48 = Casilla(48, "I48", "Bienes retención comprobante de compras 16%", "A",
                monto: ret.BaseCompra16, origen: ret.BaseCompra16 > 0 ? "AUTO_606" : "MANUAL_PENDIENTE", editable: true);
            var it1_49 = Casilla(49, "I49", "Total bienes retención comprobante compras", "A",
                monto: R(it1_47.Monto + it1_48.Monto), origen: "FORMULA", calculada: true, formula: "47+48");

            var it1_50 = Casilla(50, "I50", "ITBIS retención PF/ISFL (18% de 41)", "A",
                monto: R(it1_41.Monto * 0.18m), origen: "FORMULA", calculada: true, formula: "41*18%");
            var it1_51 = Casilla(51, "I51", "ITBIS retención sociedades 07-09 (18% de 42)", "A",
                monto: R(it1_42.Monto * 0.18m), origen: "FORMULA", calculada: true, formula: "42*18%");
            var it1_52 = Casilla(52, "I52", "ITBIS retención 02-05/07-07 (18%×0.30 de 43)", "A",
                monto: R(it1_43.Monto * 0.18m * 0.30m), origen: "FORMULA", calculada: true, formula: "43*18%*0.30");
            var it1_53 = Casilla(53, "I53", "ITBIS retenido RST 18%", "A",
                monto: R(it1_44.Monto * 0.18m), origen: "FORMULA", calculada: true, formula: "44*18%");
            var it1_54 = Casilla(54, "I54", "ITBIS retenido RST 16%", "A",
                monto: R(it1_45.Monto * 0.16m), origen: "FORMULA", calculada: true, formula: "45*16%");
            var it1_55 = Casilla(55, "I55", "Total ITBIS retenido RST", "A",
                monto: R(it1_53.Monto + it1_54.Monto), origen: "FORMULA", calculada: true, formula: "53+54");
            var it1_56 = Casilla(56, "I56", "ITBIS bienes comprobante compras 18%", "A",
                monto: R(it1_47.Monto * 0.18m), origen: "FORMULA", calculada: true, formula: "47*18%");
            var it1_57 = Casilla(57, "I57", "ITBIS bienes comprobante compras 16%", "A",
                monto: R(it1_48.Monto * 0.16m), origen: "FORMULA", calculada: true, formula: "48*16%");
            var it1_58 = Casilla(58, "I58", "Total ITBIS bienes comprobante compras", "A",
                monto: R(it1_56.Monto + it1_57.Monto), origen: "FORMULA", calculada: true, formula: "56+57");
            var it1_59 = Casilla(59, "I59", "Total ITBIS percibido en venta", "A",
                origen: "MANUAL_PENDIENTE", editable: true);
            var it1_60 = Casilla(60, "I60", "Impuesto a pagar (retenciones/percepción)", "A",
                monto: R(it1_50.Monto + it1_51.Monto + it1_52.Monto + it1_55.Monto + it1_58.Monto + it1_59.Monto),
                origen: "FORMULA", calculada: true, formula: "50+51+52+55+58+59");
            var it1_61 = Casilla(61, "I61", "Pagos computables a cuenta (sección A)", "A",
                origen: "MANUAL_PENDIENTE", editable: true);
            var diffA = it1_60.Monto - it1_61.Monto;
            var it1_62 = Casilla(62, "I62", "Diferencia a pagar (sección A)", "A",
                monto: diffA > 0 ? R(diffA) : 0, origen: "FORMULA", calculada: true);
            var it1_63 = Casilla(63, "I63", "Nuevo saldo a favor (sección A)", "A",
                monto: diffA < 0 ? R(-diffA) : 0, origen: "FORMULA", calculada: true);
            var it1_64 = Casilla(64, "I64", "Recargos (sección A)", "B", origen: "MANUAL_PENDIENTE", editable: true);
            var it1_65 = Casilla(65, "I65", "Interés indemnizatorio (sección A)", "B", origen: "MANUAL_PENDIENTE", editable: true);
            var it1_66 = Casilla(66, "I66", "Sanciones (sección A)", "B", origen: "MANUAL_PENDIENTE", editable: true);
            var it1_67 = Casilla(67, "I67", "Total a pagar (sección A)", "C",
                monto: R(it1_62.Monto + it1_64.Monto + it1_65.Monto + it1_66.Monto),
                origen: "FORMULA", calculada: true, formula: "62+64+65+66");
            var it1_68 = Casilla(68, "I68", "Total general", "C",
                monto: R(it1_38.Monto + it1_67.Monto),
                origen: "FORMULA", calculada: true, formula: "38+67");

            var anexoA = new List<It1CasillaDto>
            {
                a1_8[1], a1_8[2], a1_8[3], a1_8[4], a1_8[5], a1_8[6], a1_8[7], a1_8[8],
                a9, a10, a11,
                a12, a13, a14, a15, a16, a17, a18, a19,
                a20, a21, a22, a23, a24, a25, a26,
                a27, a28, a29, a30, a31, a32, a33,
                a34, a35, a36, a37, a38,
                a39, a40, a41, a42,
                a43, a44,
                a45, a46, a47, a48, a49, a50, a51, a52, a53, a54, a55, a56
            };

            var it1 = new List<It1CasillaDto>
            {
                it1_1, it1_2, it1_3, it1_4, it1_5, it1_6, it1_7, it1_8, it1_9, it1_10,
                it1_11, it1_12, it1_13, it1_14, it1_15,
                it1_16, it1_17, it1_18, it1_19, it1_20, it1_21,
                it1_22, it1_23, it1_24, it1_25, it1_26, it1_27,
                it1_28, it1_29, it1_30, it1_31, it1_32, it1_33, it1_34,
                it1_35, it1_36, it1_37, it1_38,
                it1_39, it1_40, it1_41, it1_42, it1_43, it1_44, it1_45, it1_46,
                it1_47, it1_48, it1_49, it1_50, it1_51, it1_52, it1_53, it1_54, it1_55,
                it1_56, it1_57, it1_58, it1_59, it1_60, it1_61, it1_62, it1_63,
                it1_64, it1_65, it1_66, it1_67, it1_68
            };

            var cantidadAlertas = alertasGlobales.Count
                + anexoA.Sum(c => c.Alertas.Count)
                + it1.Sum(c => c.Alertas.Count);

            var dto = new ReporteIt1Dto
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
                FechaLimitePago = new DateTime(h.Year, h.Month, 1).AddMonths(2).AddDays(-1) > h
                    ? new DateTime(h.Year, h.Month, 1).AddMonths(1).AddDays(19)
                    : new DateTime(h.Year, h.Month, 1).AddMonths(1).AddDays(19),
                TipoDeclaracion = "Original",
                VersionInstructivo = cfg?.VersionInstructivoPreferida ?? "IT-1-2020",
                TotalOperacionesPeriodo = a11.Monto,
                TotalItbisCobrado = it1_21.Monto,
                TotalItbisDeducible = it1_25.Monto,
                ImpuestoAPagar = it1_26.Monto,
                SaldoAFavor = it1_27.Monto,
                TotalGeneralAPagar = it1_68.Monto,
                CantidadAlertas = cantidadAlertas,
                AlertasGlobales = alertasGlobales,
                AnexoA = anexoA,
                It1 = it1
            };

            dto.ContenidoCsv = GenerarCsv(dto);
            return dto;
        }

        // ---------- helpers agregación ----------

        private static Dictionary<int, It1CasillaDto> AgregarPorTipoNcf(List<Reporte607LineaDto> lineas)
        {
            var labels = new Dictionary<int, string>
            {
                [1] = "Comprobantes válidos para crédito fiscal (01 y 31)",
                [2] = "Comprobantes consumo (02 y 32)",
                [3] = "Comprobantes nota de débito (03 y 33)",
                [4] = "Comprobantes nota de crédito (04 y 34)",
                [5] = "Comprobantes registro único de ingresos (12)",
                [6] = "Comprobantes regímenes especiales (14 y 44)",
                [7] = "Comprobantes gubernamentales (15 y 45)",
                [8] = "Comprobantes para exportaciones (16 y 46)"
            };

            var map = new Dictionary<int, It1CasillaDto>();
            for (var i = 1; i <= 8; i++)
            {
                map[i] = Casilla(i, $"A{i}", labels[i], "II", origen: "AUTO_607", calculada: true);
            }

            foreach (var l in lineas)
            {
                var bucket = BucketTipoNcf(l.Ncf, l.TipoDocumentoAlahia);
                if (bucket < 1 || bucket > 8) continue;
                map[bucket].Cantidad = (map[bucket].Cantidad ?? 0) + 1;
                map[bucket].Monto = R(map[bucket].Monto + l.MontoFacturado);
            }

            return map;
        }

        private static int BucketTipoNcf(string ncf, string tipoDoc)
        {
            var codigo = ExtraerCodigoTipo(ncf);
            return codigo switch
            {
                "01" or "31" => 1,
                "02" or "32" => 2,
                "03" or "33" => 3,
                "04" or "34" => 4,
                "12" => 5,
                "14" or "44" => 6,
                "15" or "45" => 7,
                "16" or "46" => 8,
                _ => tipoDoc == "NotaCredito" ? 4 : 0
            };
        }

        private static string ExtraerCodigoTipo(string? ncf)
        {
            if (string.IsNullOrWhiteSpace(ncf) || ncf.Length < 3) return "";
            var u = ncf.Trim().ToUpperInvariant();
            if (u.StartsWith("E") && u.Length >= 3) return u.Substring(1, 2);
            if (u.StartsWith("B") && u.Length >= 3) return u.Substring(1, 2);
            return "";
        }

        private static Dictionary<int, It1CasillaDto> AgregarPorTipoIngreso(List<Reporte607LineaDto> lineas)
        {
            var labels = new Dictionary<int, (int Num, string Cod, string Etiqueta)>
            {
                [1] = (20, "A20", "Ingresos por operaciones (no financieros)"),
                [2] = (21, "A21", "Ingresos financieros"),
                [3] = (22, "A22", "Ingresos extraordinarios"),
                [4] = (23, "A23", "Ingresos por arrendamientos"),
                [5] = (24, "A24", "Ingresos por ventas de activos depreciables"),
                [6] = (25, "A25", "Otros ingresos")
            };

            var map = new Dictionary<int, It1CasillaDto>();
            foreach (var kv in labels)
            {
                map[kv.Key] = Casilla(kv.Value.Num, kv.Value.Cod, kv.Value.Etiqueta, "IV",
                    origen: "AUTO_607", calculada: true);
            }

            foreach (var l in lineas)
            {
                if (l.TipoDocumentoAlahia == "NotaCredito") continue;
                var t = l.TipoIngreso is >= 1 and <= 6 ? l.TipoIngreso : 1;
                map[t].Monto = R(map[t].Monto + l.MontoFacturado);
            }

            return map;
        }

        private static Dictionary<int, It1CasillaDto> AgregarItbisPorDestino(List<OrdenCompraHeader> compras)
        {
            var labels = new Dictionary<int, (int Num, string Cod, string Etiqueta)>
            {
                [1] = (45, "A45", "ITBIS no deducible — productores bienes/servicios exentos"),
                [2] = (46, "A46", "ITBIS a incluir en activos (categoría I)"),
                [3] = (47, "A47", "Otros ITBIS pagados no deducibles"),
                [4] = (49, "A49", "ITBIS deducible — bienes exportados"),
                [5] = (50, "A50", "ITBIS deducible — bienes gravados"),
                [6] = (51, "A51", "ITBIS deducible — servicios gravados"),
                [7] = (53, "A53", "ITBIS sujeto a proporcionalidad")
            };

            var map = new Dictionary<int, It1CasillaDto>();
            foreach (var kv in labels)
            {
                map[kv.Key] = Casilla(kv.Value.Num, kv.Value.Cod, kv.Value.Etiqueta, "IX",
                    origen: "AUTO_606", calculada: true, conColumnas: true);
            }

            foreach (var c in compras.Where(x => x.TotalItbis > 0))
            {
                byte destino;
                if (c.DestinoItbis is >= 1 and <= 7)
                    destino = c.DestinoItbis.Value;
                else if (c.ItbisProporcionalidad > 0)
                    destino = 7;
                else if (c.ItbisLlevadoAlCosto > 0 && c.ItbisLlevadoAlCosto >= c.TotalItbis)
                    destino = 3;
                else if (c.DestinoItbisSugerido is >= 1 and <= 7)
                    destino = c.DestinoItbisSugerido.Value;
                else
                    destino = 5; // default bienes gravados

                var local = c.ItbisComprasLocales;
                var serv = c.ItbisServicios;
                var imp = c.ItbisImportaciones;
                if (local + serv + imp <= 0)
                {
                    // Split heurístico: servicios tipo bienes 2 → servicios; importación flag → imp; resto local
                    if (c.EsImportacion)
                        imp = c.TotalItbis;
                    else if (c.MontoFacturadoServicios > 0 && c.MontoFacturadoBienes <= 0)
                        serv = c.TotalItbis;
                    else if (c.MontoFacturadoServicios > 0)
                    {
                        var neto = c.Total - c.TotalItbis;
                        if (neto > 0)
                        {
                            serv = R(c.TotalItbis * (c.MontoFacturadoServicios / neto));
                            local = R(c.TotalItbis - serv);
                        }
                        else
                            local = c.TotalItbis;
                    }
                    else
                        local = c.TotalItbis;
                }

                // Si hay proporcionalidad explícita, ese monto va a casilla 53
                if (c.ItbisProporcionalidad > 0 && destino != 7)
                {
                    AcumularDestino(map[7], c.ItbisProporcionalidad, local, serv, imp, c.TotalItbis);
                    var resto = c.TotalItbis - c.ItbisProporcionalidad;
                    if (resto > 0)
                        AcumularDestino(map[destino], resto, local, serv, imp, c.TotalItbis);
                }
                else
                {
                    AcumularDestino(map[destino], c.TotalItbis, local, serv, imp, c.TotalItbis);
                }

                if (!c.ClasificacionConfirmada)
                    map[destino].Alertas.Add($"FACTC {c.NumeroDocumento ?? c.IdOrdenCompraHeader.ToString()}: destino no confirmado.");
            }

            return map;
        }

        private static void AcumularDestino(
            It1CasillaDto casilla,
            decimal itbis,
            decimal local,
            decimal serv,
            decimal imp,
            decimal totalItbisDoc)
        {
            var factor = totalItbisDoc > 0 ? itbis / totalItbisDoc : 1m;
            casilla.Monto = R(casilla.Monto + itbis);
            casilla.MontoLocal = R((casilla.MontoLocal ?? 0) + local * factor);
            casilla.MontoServicios = R((casilla.MontoServicios ?? 0) + serv * factor);
            casilla.MontoImportaciones = R((casilla.MontoImportaciones ?? 0) + imp * factor);
        }

        private static It1CasillaDto SumDestino(
            int num, string codigo, string etiqueta, string seccion,
            It1CasillaDto a, It1CasillaDto b, It1CasillaDto c, string formula)
        {
            return Casilla(num, codigo, etiqueta, seccion,
                monto: R(a.Monto + b.Monto + c.Monto),
                origen: "FORMULA", calculada: true, formula: formula, conColumnas: true,
                local: R((a.MontoLocal ?? 0) + (b.MontoLocal ?? 0) + (c.MontoLocal ?? 0)),
                serv: R((a.MontoServicios ?? 0) + (b.MontoServicios ?? 0) + (c.MontoServicios ?? 0)),
                imp: R((a.MontoImportaciones ?? 0) + (b.MontoImportaciones ?? 0) + (c.MontoImportaciones ?? 0)));
        }

        private sealed class ClasifIt1
        {
            public decimal ExportBienes;
            public decimal ExportServicios;
            public decimal ExentoLocal;
            public decimal ExentoDestino;
            public decimal Exento343P3;
            public decimal Gravado18;
            public decimal Gravado16;
            public decimal Gravado9;
            public decimal Gravado8;
            public decimal ActivosDep;
        }

        private static ClasifIt1 ClasificarVentasIt1(
            List<FacturaHeaders> facturas,
            List<NotasCredito> notas,
            decimal noSujetasConstruccion,
            decimal noSujetasComisiones)
        {
            _ = noSujetasConstruccion;
            _ = noSujetasComisiones;
            var r = new ClasifIt1();

            foreach (var f in facturas)
            {
                var codigo = !string.IsNullOrWhiteSpace(f.CodigoTipoComprobanteDgii)
                    ? f.CodigoTipoComprobanteDgii!.Trim()
                    : ExtraerCodigoTipo(f.NCF);
                var baseFoto = f.MontoGravado + f.MontoExento;
                if (baseFoto <= 0)
                    baseFoto = f.SubTotal > 0 ? f.SubTotal : Math.Max(0, f.Total - f.TotalItbis);

                var esExport = codigo is "16" or "46";
                var esEspecial = codigo is "14" or "44"
                    || string.Equals(f.RegimenFiscalClienteCodigo, "ESPECIAL", StringComparison.OrdinalIgnoreCase);

                if (esExport)
                {
                    // Sin flag servicio en header: todo a bienes export; usuario ajusta servicios
                    r.ExportBienes += baseFoto;
                    continue;
                }

                if (esEspecial)
                {
                    r.ExentoDestino += baseFoto;
                    continue;
                }

                r.ExentoLocal += f.MontoExento;
                r.Gravado18 += f.MontoGravadoI1 > 0 ? f.MontoGravadoI1 : f.MontoGravado;
                r.Gravado16 += f.MontoGravadoI2;
                r.Gravado9 += f.MontoGravadoI3;
                r.Gravado8 += f.MontoGravadoI4;

                if (f.TipoIngresoDgii == 5)
                    r.ActivosDep += f.MontoGravado > 0 ? f.MontoGravado : baseFoto;
            }

            foreach (var n in notas)
            {
                var baseFoto = n.MontoGravado + n.MontoExento;
                if (baseFoto <= 0)
                    baseFoto = n.SubTotal > 0 ? n.SubTotal : Math.Max(0, n.Total - n.TotalItbis);

                // NC reduce gravado/exento
                r.ExentoLocal = Math.Max(0, r.ExentoLocal - n.MontoExento);
                var g1 = n.MontoGravadoI1 > 0 ? n.MontoGravadoI1 : n.MontoGravado;
                r.Gravado18 = Math.Max(0, r.Gravado18 - g1);
                r.Gravado16 = Math.Max(0, r.Gravado16 - n.MontoGravadoI2);
                r.Gravado9 = Math.Max(0, r.Gravado9 - n.MontoGravadoI3);
                r.Gravado8 = Math.Max(0, r.Gravado8 - n.MontoGravadoI4);
                if (g1 <= 0 && n.MontoExento <= 0)
                    r.Gravado18 = Math.Max(0, r.Gravado18 - baseFoto);
            }

            r.ExportBienes = R(r.ExportBienes);
            r.ExportServicios = R(r.ExportServicios);
            r.ExentoLocal = R(r.ExentoLocal);
            r.ExentoDestino = R(r.ExentoDestino);
            r.Gravado18 = R(r.Gravado18);
            r.Gravado16 = R(r.Gravado16);
            r.Gravado9 = R(r.Gravado9);
            r.Gravado8 = R(r.Gravado8);
            r.ActivosDep = R(r.ActivosDep);
            return r;
        }

        private sealed class RetencionesAgg
        {
            public decimal BasePf;
            public decimal BaseIsfl;
            public decimal BaseSoc0709;
            public decimal BaseSoc0205;
            public decimal BaseRst18;
            public decimal BaseRst16;
            public decimal BaseCompra18;
            public decimal BaseCompra16;
        }

        private static RetencionesAgg AgregarRetencionesPracticadas(List<OrdenCompraHeader> compras)
        {
            var r = new RetencionesAgg();
            foreach (var c in compras)
            {
                if (c.ItbisRetenido <= 0 && c.BaseRetencionItbis <= 0) continue;
                var baseRet = c.BaseRetencionItbis > 0
                    ? c.BaseRetencionItbis
                    : (c.TotalItbis > 0 ? R(c.ItbisRetenido / 0.18m) : 0);
                if (baseRet <= 0) continue;

                var norma = (c.CodigoNormaRetencionItbis ?? "").Trim().ToUpperInvariant();
                var tasa = c.TasaItbis ?? 18m;

                if (norma.Contains("01-11") || norma.Contains("0111"))
                    r.BaseIsfl += baseRet;
                else if (norma.Contains("07-09") || norma.Contains("0709"))
                    r.BaseSoc0709 += baseRet;
                else if (norma.Contains("02-05") || norma.Contains("07-07") || norma.Contains("0205") || norma.Contains("0707"))
                    r.BaseSoc0205 += baseRet;
                else if (norma.Contains("RST") || norma.Contains("PST"))
                {
                    if (tasa <= 16.5m) r.BaseRst16 += baseRet;
                    else r.BaseRst18 += baseRet;
                }
                else if (norma.Contains("08-10") || norma.Contains("05-19") || norma.Contains("0810") || norma.Contains("0519"))
                {
                    if (tasa <= 16.5m) r.BaseCompra16 += baseRet;
                    else r.BaseCompra18 += baseRet;
                }
                else
                    r.BasePf += baseRet; // default personas físicas
            }

            r.BasePf = R(r.BasePf);
            r.BaseIsfl = R(r.BaseIsfl);
            r.BaseSoc0709 = R(r.BaseSoc0709);
            r.BaseSoc0205 = R(r.BaseSoc0205);
            r.BaseRst18 = R(r.BaseRst18);
            r.BaseRst16 = R(r.BaseRst16);
            r.BaseCompra18 = R(r.BaseCompra18);
            r.BaseCompra16 = R(r.BaseCompra16);
            return r;
        }

        private static decimal MontoBaseNc(NotasCredito n)
        {
            var foto = n.MontoGravado + n.MontoExento;
            if (foto > 0) return R(foto);
            if (n.SubTotal > 0) return R(n.SubTotal);
            return R(Math.Max(0, n.Total - n.TotalItbis));
        }

        private static bool EsNcfRegimenEspecial(string? ncf)
        {
            var c = ExtraerCodigoTipo(ncf);
            return c is "14" or "44";
        }

        // ---------- casilla factories ----------

        private static It1CasillaDto Casilla(
            int numero,
            string codigo,
            string etiqueta,
            string seccion,
            int? cantidad = null,
            decimal monto = 0,
            string origen = "FORMULA",
            bool calculada = false,
            bool editable = false,
            string? formula = null,
            string? alerta = null,
            bool conColumnas = false,
            decimal? local = null,
            decimal? serv = null,
            decimal? imp = null)
        {
            var c = new It1CasillaDto
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
            if (conColumnas)
            {
                c.MontoLocal = R(local ?? 0);
                c.MontoServicios = R(serv ?? 0);
                c.MontoImportaciones = R(imp ?? 0);
            }
            if (!string.IsNullOrWhiteSpace(alerta))
                c.Alertas.Add(alerta);
            return c;
        }

        private static It1CasillaDto CasillaVertical(int num, string codigo, string etiqueta, string seccion, bool aplica)
        {
            return Casilla(num, codigo, etiqueta, seccion,
                origen: aplica ? "MANUAL_PENDIENTE" : "NO_APLICA",
                editable: aplica,
                alerta: aplica ? "Completar si aplica a la empresa." : null);
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

        private static string GenerarCsv(ReporteIt1Dto dto)
        {
            var sb = new StringBuilder();
            sb.AppendLine("FORMULARIO;NUMERO;CODIGO;SECCION;ETIQUETA;CANTIDAD;MONTO;LOCAL;SERVICIOS;IMPORTACIONES;ORIGEN;FORMULA;ALERTAS");
            void Write(string form, It1CasillaDto c)
            {
                sb.Append(form).Append(';')
                    .Append(c.Numero).Append(';')
                    .Append(c.Codigo).Append(';')
                    .Append(Esc(c.Seccion)).Append(';')
                    .Append(Esc(c.Etiqueta)).Append(';')
                    .Append(c.Cantidad?.ToString(Inv) ?? "").Append(';')
                    .Append(c.Monto.ToString("0.00", Inv)).Append(';')
                    .Append(c.MontoLocal?.ToString("0.00", Inv) ?? "").Append(';')
                    .Append(c.MontoServicios?.ToString("0.00", Inv) ?? "").Append(';')
                    .Append(c.MontoImportaciones?.ToString("0.00", Inv) ?? "").Append(';')
                    .Append(c.Origen).Append(';')
                    .Append(Esc(c.FormulaAplicada)).Append(';')
                    .Append(Esc(string.Join(" | ", c.Alertas)))
                    .AppendLine();
            }

            foreach (var c in dto.AnexoA) Write("AnexoA", c);
            foreach (var c in dto.It1) Write("IT-1", c);
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
