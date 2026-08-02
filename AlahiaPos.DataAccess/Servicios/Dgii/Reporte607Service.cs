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
    /// Motor Formato 607 (ventas) — NG 07-2018 / 05-2019.
    /// Solo lectura: facturas + notas de crédito con NCF → 23 columnas + TXT.
    /// </summary>
    public class Reporte607Service : IReporte607Service
    {
        private const decimal UmbralFacturaConsumo = 250_000m;
        private const decimal ToleranciaCuadre = 0.02m;

        private readonly AlahiaPosContext _ctx;

        public Reporte607Service(AlahiaPosContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<Reporte607Dto> ObtenerReporte607Async(
            int idEmpresa,
            DateTime desde,
            DateTime hasta,
            string? periodo = null)
        {
            var d = desde.Date;
            var h = hasta.Date;
            if (h < d)
                throw new ArgumentException("La fecha hasta no puede ser menor que desde.");

            var empresa = await _ctx.Empresas
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa);

            var periodoTxt = string.IsNullOrWhiteSpace(periodo)
                ? h.ToString("yyyyMM")
                : periodo.Trim();

            var result = new Reporte607Dto
            {
                IdEmpresa = idEmpresa,
                RncEmpresa = LimpiarDocumento(empresa?.RNC),
                NombreEmpresa = empresa?.NombreComercial,
                Periodo = periodoTxt,
                Desde = d,
                Hasta = h
            };

            // Fiscal: incluir ventas con NCF del periodo.
            // Las anuladas por NC también entran: el e-CF/NCF se emitió; la NC lo referencia (col 4).
            // Solo se excluyen cancelaciones operativas sin vínculo fiscal a NC.
            var facturas = await _ctx.FacturaHeaders
                .AsNoTracking()
                .Where(x =>
                    x.IdEmpresa == idEmpresa
                    && x.IdTipoDocumentos == 1
                    && x.FechaInseccion.Date >= d
                    && x.FechaInseccion.Date <= h
                    && x.NCF != null
                    && x.NCF != ""
                    && (
                        !x.EstaCancelada
                        || (x.MotivoAnulacion != null
                            && x.MotivoAnulacion.ToLower().Contains("nota de cr"))
                    ))
                .OrderBy(x => x.FechaInseccion)
                .ThenBy(x => x.IdFacturaHeader)
                .ToListAsync();

            var clienteIds = facturas
                .Where(f => f.IDCliente.HasValue && f.IDCliente.Value > 0)
                .Select(f => f.IDCliente!.Value)
                .Distinct()
                .ToList();

            var clientes = clienteIds.Count == 0
                ? new Dictionary<int, Clientes>()
                : await _ctx.Clientes
                    .AsNoTracking()
                    .Where(c => clienteIds.Contains(c.IDCliente))
                    .ToDictionaryAsync(c => c.IDCliente);

            var notas = await _ctx.NotasCredito
                .AsNoTracking()
                .Where(n =>
                    n.IdEmpresa == idEmpresa
                    && n.Estado != NotaCreditoEstado.Anulada
                    && n.FechaInseccion.Date >= d
                    && n.FechaInseccion.Date <= h
                    && n.NCF != null
                    && n.NCF != "")
                .OrderBy(n => n.FechaInseccion)
                .ThenBy(n => n.IdNotaCredito)
                .ToListAsync();

            var ncClienteIds = notas
                .Where(n => n.IdCliente.HasValue && n.IdCliente.Value > 0)
                .Select(n => n.IdCliente!.Value)
                .Distinct()
                .Where(id => !clientes.ContainsKey(id))
                .ToList();

            if (ncClienteIds.Count > 0)
            {
                var extra = await _ctx.Clientes
                    .AsNoTracking()
                    .Where(c => ncClienteIds.Contains(c.IDCliente))
                    .ToListAsync();
                foreach (var c in extra)
                    clientes[c.IDCliente] = c;
            }

            // Nombre del comprador: e-CF receptor y/o padrón DGII (ventas e-CF suelen ir sin IdCliente).
            var ncfs = facturas.Select(f => f.NCF!.Trim().ToUpper())
                .Concat(notas.Select(n => n.NCF!.Trim().ToUpper()))
                .Distinct()
                .ToList();

            var nombresPorEncf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (ncfs.Count > 0)
            {
                var ecfRows = await _ctx.ECFEncabezados
                    .AsNoTracking()
                    .Where(e => e.IdEmpresa == idEmpresa && e.ENCF != null && ncfs.Contains(e.ENCF))
                    .Select(e => new { e.IdECF, e.ENCF, e.NombreReceptor })
                    .ToListAsync();

                foreach (var row in ecfRows.OrderByDescending(x => x.IdECF))
                {
                    var key = (row.ENCF ?? "").Trim().ToUpperInvariant();
                    if (key.Length == 0 || nombresPorEncf.ContainsKey(key))
                        continue;
                    if (!string.IsNullOrWhiteSpace(row.NombreReceptor))
                        nombresPorEncf[key] = row.NombreReceptor.Trim();
                }
            }

            var rncsBuscar = facturas
                .Select(f => LimpiarDocumento(f.RNC))
                .Concat(notas.Select(n => LimpiarDocumento(n.RNC)))
                .Where(r => r.Length > 0)
                .Distinct()
                .ToList();

            var nombresPorRnc = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (rncsBuscar.Count > 0)
            {
                var dgiiRows = await _ctx.ClientesDGII
                    .AsNoTracking()
                    .Where(c => rncsBuscar.Contains(c.RNC))
                    .Select(c => new { c.RNC, c.NombreComercial, c.RazonSocial })
                    .ToListAsync();

                foreach (var row in dgiiRows)
                {
                    var key = LimpiarDocumento(row.RNC);
                    if (key.Length == 0 || nombresPorRnc.ContainsKey(key))
                        continue;
                    var nom = FirstNonEmpty(row.NombreComercial, row.RazonSocial);
                    if (!string.IsNullOrWhiteSpace(nom))
                        nombresPorRnc[key] = nom!;
                }
            }

            foreach (var f in facturas)
            {
                clientes.TryGetValue(f.IDCliente ?? 0, out var cli);
                var ncfKey = (f.NCF ?? "").Trim().ToUpperInvariant();
                nombresPorEncf.TryGetValue(ncfKey, out var nombreEcf);
                nombresPorRnc.TryGetValue(LimpiarDocumento(f.RNC), out var nombreRnc);
                result.Lineas.Add(MapearFactura(f, cli, nombreEcf, nombreRnc));
            }

            foreach (var n in notas)
            {
                clientes.TryGetValue(n.IdCliente ?? 0, out var cli);
                result.Lineas.Add(MapearNotaCredito(n, cli));
            }

            result.Lineas = result.Lineas
                .OrderBy(l => l.FechaComprobante)
                .ThenBy(l => l.Ncf)
                .ToList();

            // Resumen FC < 250K (no van al TXT detalle)
            var resumen = result.Lineas.Where(l => l.EsResumenFacturaConsumo).ToList();
            result.ResumenFcCantidad = resumen.Count;
            result.ResumenFcMonto = resumen.Sum(l => l.MontoFacturado);
            result.ResumenFcItbis = resumen.Sum(l => l.ItbisFacturado);

            var detalle = result.Lineas.Where(l => !l.EsResumenFacturaConsumo).ToList();
            result.CantidadRegistros = detalle.Count;
            result.CantidadConAlertas = detalle.Count(l => !l.EsValidaParaEnvio);
            result.TotalMontoFacturado = detalle.Sum(l => l.MontoFacturado);
            result.TotalItbisFacturado = detalle.Sum(l => l.ItbisFacturado);
            result.ContenidoTxt = GenerarTxt607(result, detalle);

            return result;
        }

        private static Reporte607LineaDto MapearFactura(
            FacturaHeaders f,
            Clientes? cli,
            string? nombreEcf = null,
            string? nombrePorRnc = null)
        {
            var ncf = (f.NCF ?? "").Trim().ToUpperInvariant();
            var rnc = LimpiarDocumento(f.RNC);
            if (string.IsNullOrEmpty(rnc))
                rnc = LimpiarDocumento(cli?.CedulaRNC);

            var tipoId = ResolverTipoIdentificacion(rnc, cli?.TipoIdentificacionDgii);
            var tipoIngreso = f.TipoIngresoDgii is >= 1 and <= 6
                ? f.TipoIngresoDgii.Value
                : (byte)1;

            var montoFacturado = ResolverMontoFacturado(f.MontoGravado, f.MontoExento, f.SubTotal, f.Total, f.TotalItbis);
            var itbis = f.TotalItbis;
            var propina = f.MontoPropinaLegal > 0 ? f.MontoPropinaLegal : f.MontoPropina;
            var totalBruto = montoFacturado + itbis + propina;

            var clienteNombre =
                FirstNonEmpty(
                    cli?.NombreComercial,
                    f.NombreCuenta,
                    nombreEcf,
                    nombrePorRnc);

            var linea = new Reporte607LineaDto
            {
                IdDocumento = f.IdFacturaHeader,
                TipoDocumentoAlahia = "Venta",
                NumeroDocumento = f.NumeroDocumento,
                ClienteNombre = clienteNombre,
                RncCedulaComprador = rnc,
                TipoIdentificacion = tipoId,
                Ncf = ncf,
                NcfModificado = null,
                TipoIngreso = tipoIngreso,
                FechaComprobante = f.FechaInseccion.Date,
                MontoFacturado = Redondear(montoFacturado),
                ItbisFacturado = Redondear(itbis),
                MontoPropinaLegal = Redondear(propina)
            };

            AsignarMediosPagoFactura(linea, f, totalBruto);
            CuadrarMediosPago(linea, totalBruto);

            linea.EsResumenFacturaConsumo = EsFacturaConsumoBajoUmbral(ncf, totalBruto);
            ValidarLinea(linea, esNotaCredito: false);
            return linea;
        }

        private static Reporte607LineaDto MapearNotaCredito(NotasCredito n, Clientes? cli)
        {
            var ncf = (n.NCF ?? "").Trim().ToUpperInvariant();
            var ncfMod = string.IsNullOrWhiteSpace(n.NCFModificado)
                ? null
                : n.NCFModificado.Trim().ToUpperInvariant();

            var rnc = LimpiarDocumento(n.RNC);
            if (string.IsNullOrEmpty(rnc))
                rnc = LimpiarDocumento(cli?.CedulaRNC);

            var tipoId = ResolverTipoIdentificacion(rnc, cli?.TipoIdentificacionDgii);
            var tipoIngreso = n.TipoIngresoDgii is >= 1 and <= 6
                ? n.TipoIngresoDgii.Value
                : (byte)1;

            var montoFacturado = ResolverMontoFacturado(n.MontoGravado, n.MontoExento, n.SubTotal, n.Total, n.TotalItbis);
            var itbis = n.TotalItbis;
            var totalBruto = montoFacturado + itbis;

            var linea = new Reporte607LineaDto
            {
                IdDocumento = n.IdNotaCredito,
                TipoDocumentoAlahia = "NotaCredito",
                NumeroDocumento = n.NumeroDocumento,
                ClienteNombre = n.NombreCliente ?? cli?.NombreComercial,
                RncCedulaComprador = rnc,
                TipoIdentificacion = tipoId,
                Ncf = ncf,
                NcfModificado = ncfMod,
                TipoIngreso = tipoIngreso,
                FechaComprobante = n.FechaInseccion.Date,
                MontoFacturado = Redondear(montoFacturado),
                ItbisFacturado = Redondear(itbis),
                // NC: DGII acepta el total en "Otras formas"
                OtrasFormasVenta = Redondear(totalBruto),
                EsResumenFacturaConsumo = false
            };

            ValidarLinea(linea, esNotaCredito: true);
            return linea;
        }

        private static void AsignarMediosPagoFactura(
            Reporte607LineaDto linea,
            FacturaHeaders f,
            decimal totalBruto)
        {
            var esCredito = EsVentaACredito(f);

            if (esCredito && f.MontoEfectivo == 0 && f.MontoTransferencia == 0
                && f.MontoCheques == 0 && f.MontoTarjetaVisa == 0
                && f.MontoTarjetaMasterCard == 0 && f.MontoNotaCredito == 0)
            {
                linea.VentaCredito = Redondear(totalBruto);
                return;
            }

            linea.Efectivo = Redondear(f.MontoEfectivo);
            linea.ChequeTransferenciaDeposito = Redondear(f.MontoTransferencia + f.MontoCheques);
            linea.TarjetaDebitoCredito = Redondear(f.MontoTarjetaVisa + f.MontoTarjetaMasterCard);
            linea.BonosCertificados = Redondear(f.MontoNotaCredito);

            if (esCredito && f.Pendiente > 0)
                linea.VentaCredito = Redondear(f.Pendiente);
            else if (esCredito)
                linea.VentaCredito = 0;

            var suma =
                linea.Efectivo
                + linea.ChequeTransferenciaDeposito
                + linea.TarjetaDebitoCredito
                + linea.VentaCredito
                + linea.BonosCertificados;

            if (suma <= 0 && totalBruto > 0)
            {
                // Fallback por texto FormaPago
                var fp = (f.FormaPago ?? "").ToLowerInvariant();
                if (fp.Contains("visa") || fp.Contains("master") || fp.Contains("tarjeta") || fp.Contains("card"))
                    linea.TarjetaDebitoCredito = Redondear(totalBruto);
                else if (fp.Contains("transfer") || fp.Contains("cheque") || fp.Contains("deposito")
                         || fp.Contains("popular") || fp.Contains("bhd") || fp.Contains("banreserva") || fp.Contains("apap") || fp.Contains("qik"))
                    linea.ChequeTransferenciaDeposito = Redondear(totalBruto);
                else if (esCredito || fp.Contains("credito") || fp.Contains("crédito"))
                    linea.VentaCredito = Redondear(totalBruto);
                else
                    linea.Efectivo = Redondear(totalBruto);
            }
        }

        private static void CuadrarMediosPago(Reporte607LineaDto linea, decimal totalBruto)
        {
            var suma =
                linea.Efectivo
                + linea.ChequeTransferenciaDeposito
                + linea.TarjetaDebitoCredito
                + linea.VentaCredito
                + linea.BonosCertificados
                + linea.Permuta
                + linea.OtrasFormasVenta;

            var diff = Redondear(totalBruto - suma);
            if (Math.Abs(diff) >= ToleranciaCuadre)
                linea.OtrasFormasVenta = Redondear(linea.OtrasFormasVenta + diff);
            else if (Math.Abs(diff) > 0 && Math.Abs(diff) < ToleranciaCuadre)
                linea.OtrasFormasVenta = Redondear(linea.OtrasFormasVenta + diff);
        }

        private static void ValidarLinea(Reporte607LineaDto linea, bool esNotaCredito)
        {
            var ncf = linea.Ncf;
            if (string.IsNullOrWhiteSpace(ncf))
                linea.Alertas.Add("Falta NCF");
            else if (ncf.Length != 11 && ncf.Length != 13)
                linea.Alertas.Add($"NCF longitud inválida ({ncf.Length}); esperado 11 o 13");

            if (esNotaCredito && string.IsNullOrWhiteSpace(linea.NcfModificado))
                linea.Alertas.Add("NC sin NCF modificado");

            if (linea.TipoIngreso < 1 || linea.TipoIngreso > 6)
                linea.Alertas.Add("Tipo de ingreso inválido (1-6)");

            var requiereId = RequiereIdentificacionComprador(ncf);
            if (requiereId && string.IsNullOrWhiteSpace(linea.RncCedulaComprador))
                linea.Alertas.Add("Cliente sin RNC/Cédula");

            if (!string.IsNullOrWhiteSpace(linea.RncCedulaComprador)
                && (linea.TipoIdentificacion < 1 || linea.TipoIdentificacion > 3))
                linea.Alertas.Add("Tipo identificación inválido");

            if ((linea.ItbisRetenidoPorTercero > 0 || linea.IsrRetenidoPorTercero > 0)
                && !linea.FechaRetencion.HasValue)
                linea.Alertas.Add("Retención sin fecha de retención");

            if (linea.EsResumenFacturaConsumo)
                return; // no valida cuadre para líneas que no van al TXT

            var totalBruto =
                linea.MontoFacturado
                + linea.ItbisFacturado
                + linea.ImpuestoSelectivoConsumo
                + linea.OtrosImpuestos
                + linea.MontoPropinaLegal;

            var sumaMedios =
                linea.Efectivo
                + linea.ChequeTransferenciaDeposito
                + linea.TarjetaDebitoCredito
                + linea.VentaCredito
                + linea.BonosCertificados
                + linea.Permuta
                + linea.OtrasFormasVenta;

            if (Math.Abs(totalBruto - sumaMedios) > ToleranciaCuadre)
                linea.Alertas.Add("Medios de pago no cuadran con el total");
        }

        private static string GenerarTxt607(Reporte607Dto reporte, List<Reporte607LineaDto> detalle)
        {
            var rncEmp = reporte.RncEmpresa ?? "";
            var sb = new StringBuilder();
            sb.AppendLine($"607|{rncEmp}|{reporte.Periodo}|{detalle.Count}");

            foreach (var l in detalle)
            {
                var fechaComp = l.FechaComprobante.ToString("yyyyMMdd");
                var fechaRet = l.FechaRetencion.HasValue
                    ? l.FechaRetencion.Value.ToString("yyyyMMdd")
                    : "";

                sb.Append(l.RncCedulaComprador).Append('|')
                  .Append(l.TipoIdentificacion > 0 ? l.TipoIdentificacion.ToString() : "").Append('|')
                  .Append(l.Ncf).Append('|')
                  .Append(l.NcfModificado ?? "").Append('|')
                  .Append(l.TipoIngreso).Append('|')
                  .Append(fechaComp).Append('|')
                  .Append(fechaRet).Append('|')
                  .Append(FmtDec(l.MontoFacturado)).Append('|')
                  .Append(FmtDec(l.ItbisFacturado)).Append('|')
                  .Append(FmtDec(l.ItbisRetenidoPorTercero)).Append('|')
                  .Append(FmtDec(l.ItbisPercibido)).Append('|')
                  .Append(FmtDec(l.IsrRetenidoPorTercero)).Append('|')
                  .Append(FmtDec(l.IsrPercibido)).Append('|')
                  .Append(FmtDec(l.ImpuestoSelectivoConsumo)).Append('|')
                  .Append(FmtDec(l.OtrosImpuestos)).Append('|')
                  .Append(FmtDec(l.MontoPropinaLegal)).Append('|')
                  .Append(FmtDec(l.Efectivo)).Append('|')
                  .Append(FmtDec(l.ChequeTransferenciaDeposito)).Append('|')
                  .Append(FmtDec(l.TarjetaDebitoCredito)).Append('|')
                  .Append(FmtDec(l.VentaCredito)).Append('|')
                  .Append(FmtDec(l.BonosCertificados)).Append('|')
                  .Append(FmtDec(l.Permuta)).Append('|')
                  .Append(FmtDec(l.OtrasFormasVenta))
                  .AppendLine();
            }

            return sb.ToString();
        }

        private static decimal ResolverMontoFacturado(
            decimal montoGravado,
            decimal montoExento,
            decimal subTotal,
            decimal total,
            decimal itbis)
        {
            var foto = montoGravado + montoExento;
            if (foto > 0)
                return foto;
            if (subTotal > 0)
                return subTotal;
            var neto = total - itbis;
            return neto > 0 ? neto : 0;
        }

        private static bool EsFacturaConsumoBajoUmbral(string ncf, decimal totalBruto)
        {
            if (string.IsNullOrEmpty(ncf) || ncf.Length < 3)
                return false;
            var pref = ncf.Substring(0, 3);
            if (pref is not ("B02" or "E32"))
                return false;
            return totalBruto < UmbralFacturaConsumo;
        }

        private static bool RequiereIdentificacionComprador(string ncf)
        {
            if (string.IsNullOrEmpty(ncf) || ncf.Length < 3)
                return false;
            var pref = ncf.Substring(0, 3);
            return pref is "B01" or "E31" or "B14" or "E44" or "B15" or "E45"
                or "B04" or "E34" or "B03" or "E33";
        }

        private static bool EsVentaACredito(FacturaHeaders f)
        {
            if (f.FormaVentaFiscalDgii == 4)
                return true;
            var tipo = (f.TipoFactura ?? "").ToLowerInvariant();
            if (tipo.Contains("credito") || tipo.Contains("crédito"))
                return true;
            if (!string.IsNullOrWhiteSpace(f.Plazo) && f.Pendiente > 0)
                return true;
            return false;
        }

        private static int ResolverTipoIdentificacion(string digitos, byte? tipoCliente)
        {
            if (tipoCliente is >= 1 and <= 3)
                return tipoCliente.Value;
            if (string.IsNullOrEmpty(digitos))
                return 0;
            if (digitos.Length == 11)
                return 2;
            if (digitos.Length == 9)
                return 1;
            return 3;
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            foreach (var v in values)
            {
                if (!string.IsNullOrWhiteSpace(v))
                    return v.Trim();
            }
            return null;
        }

        private static string LimpiarDocumento(string? doc)
        {
            if (string.IsNullOrWhiteSpace(doc))
                return "";
            return new string(doc.Where(char.IsDigit).ToArray());
        }

        private static decimal Redondear(decimal v) =>
            Math.Round(v, 2, MidpointRounding.AwayFromZero);

        private static string FmtDec(decimal v) =>
            v.ToString("0.00", CultureInfo.InvariantCulture);
    }
}
