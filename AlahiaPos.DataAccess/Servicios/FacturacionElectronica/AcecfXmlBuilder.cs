using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using AlahiaPos.Entities.Dto.Fiscal;

namespace AlahiaPos.DataAccess.Servicios.FacturacionElectronica
{
    /// <summary>Formato Aprobación Comercial v1.0 + XSD ACECF v.1.0 (DGII).</summary>
    public static class AcecfXmlBuilder
    {
        public static string Build(AcecfDocumento d, DateTime? firma = null)
        {
            var fechaHora = NormalizarFechaHora(d.FechaHoraAprobacionComercial)
                            ?? (firma ?? DateTime.Now).ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            var fecha = NormalizarFecha(d.FechaEmision);
            var estado = d.Estado is 1 or 2 ? d.Estado : 1;
            var detalle = new XElement("DetalleAprobacionComercial",
                new XElement("Version", "1.0"),
                new XElement("RNCEmisor", Digits(d.RncEmisor)),
                new XElement("eNCF", d.Encf.Trim().ToUpperInvariant()),
                new XElement("FechaEmision", fecha),
                new XElement("MontoTotal", d.MontoTotal.ToString("0.00", CultureInfo.InvariantCulture)),
                new XElement("RNCComprador", Digits(d.RncComprador)),
                new XElement("Estado", estado));
            if (estado == 2 && !string.IsNullOrWhiteSpace(d.DetalleMotivoRechazo))
                detalle.Add(new XElement("DetalleMotivoRechazo", d.DetalleMotivoRechazo.Trim()));
            detalle.Add(new XElement("FechaHoraAprobacionComercial", fechaHora));

            var xml = new XDocument(new XDeclaration("1.0", "utf-8", null),
                new XElement("ACECF", detalle));
            return xml.Declaration + xml.Root!.ToString(SaveOptions.DisableFormatting);
        }

        public static string NombreArchivo(string rncComprador, string encf)
            => $"{Digits(rncComprador)}{encf.Trim().ToUpperInvariant()}.xml";

        private static string Digits(string? s) => new string((s ?? "").Where(char.IsDigit).ToArray());

        private static string NormalizarFecha(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return DateTime.Today.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
            var s = raw.Trim();
            if (DateTime.TryParseExact(s, new[] { "dd-MM-yyyy", "dd/MM/yyyy", "yyyy-MM-dd" },
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
            return s;
        }

        private static string? NormalizarFechaHora(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var s = raw.Trim();
            var formatos = new[]
            {
                "dd-MM-yyyy HH:mm:ss",
                "dd-MM-yyyy H:mm:ss",
                "dd/MM/yyyy HH:mm:ss",
                "dd/MM/yyyy H:mm:ss",
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-ddTHH:mm:ss",
                "M/d/yyyy h:mm:ss tt",
                "M/d/yyyy h:mm:ss tt"
            };
            if (DateTime.TryParseExact(s, formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                || DateTime.TryParse(s, CultureInfo.GetCultureInfo("es-DO"), DateTimeStyles.None, out dt)
                || DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return dt.ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            return s;
        }
    }

    /// <summary>Formato Acuse de Recibo v1.0 + XSD ARECF v1.0 (DGII).</summary>
    public static class ArecfXmlBuilder
    {
        public static string Build(string rncEmisor, string rncComprador, string encf, int estado = 0, int? motivoNoRecibido = null)
        {
            var ahora = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            var detalle = new XElement("DetalleAcusedeRecibo",
                new XElement("Version", "1.0"),
                new XElement("RNCEmisor", new string(rncEmisor.Where(char.IsDigit).ToArray())),
                new XElement("RNCComprador", new string(rncComprador.Where(char.IsDigit).ToArray())),
                new XElement("eNCF", encf.Trim().ToUpperInvariant()),
                new XElement("Estado", estado));
            if (estado == 1 && motivoNoRecibido is >= 1 and <= 4)
                detalle.Add(new XElement("CodigoMotivoNoRecibido", motivoNoRecibido));
            detalle.Add(new XElement("FechaHoraAcuseRecibo", ahora));
            var xml = new XDocument(new XDeclaration("1.0", "utf-8", null),
                new XElement("ARECF", detalle));
            return xml.Declaration + xml.Root!.ToString(SaveOptions.DisableFormatting);
        }
    }
}
