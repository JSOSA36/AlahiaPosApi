using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AlahiaPos.Entities.Dto.Fiscal;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto.Definitions
{
    /// <summary>Helpers compartidos de formato/resolución para definiciones e-CF.</summary>
    public static class EcfXmlFormat
    {
        public static string Money(decimal v) => v.ToString("0.00", CultureInfo.InvariantCulture);
        public static string Date(DateTime d) => d.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        public static string DateTimeStamp(DateTime d) => d.ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
        public static string TipoIngreso(int t) => t.ToString("00", CultureInfo.InvariantCulture);

        public static string Esc(string? s, int max)
        {
            s = (s ?? "").Trim();
            if (s.Length == 0) return "";
            return s.Length <= max ? s : s[..max];
        }

        public static string NormalizarRnc(string? rnc)
        {
            var d = new string((rnc ?? "").Where(char.IsDigit).ToArray());
            if (d.Length == 9 || d.Length == 11) return d;
            if (d.Length < 9) return d.PadLeft(9, '0');
            if (d.Length == 10) return d.PadLeft(11, '0');
            return d.Length > 11 ? d[^11..] : d;
        }

        public static string? Telefono(string? tel)
        {
            if (string.IsNullOrWhiteSpace(tel)) return null;
            var d = new string(tel.Where(char.IsDigit).ToArray());
            if (d.Length == 10) return $"{d[..3]}-{d.Substring(3, 3)}-{d.Substring(6, 4)}";
            if (d.Length == 11 && d.StartsWith("1"))
                return $"{d.Substring(1, 3)}-{d.Substring(4, 3)}-{d.Substring(7, 4)}";
            return null;
        }

        public static bool CorreoOk(string? correo)
            => !string.IsNullOrWhiteSpace(correo) &&
               Regex.IsMatch(correo.Trim(), @"^\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$");

        public static bool CodigoProvMun(string? code)
            => !string.IsNullOrWhiteSpace(code) && Regex.IsMatch(code.Trim(), @"^\d{6}$");

        public static DateTime FechaVencimientoPrecert(DateTime? fv)
        {
            var d = fv ?? new DateTime(2028, 12, 31);
            if (d < new DateTime(2028, 12, 31)) return new DateTime(2028, 12, 31);
            return d;
        }

        public static EcfCampoDef Campo(
            string nombre,
            EcfCampoPresence presence,
            int orden,
            Func<EcfBuildContext, object?>? resolver = null,
            Action<EcfBuildContext>? validar = null,
            string? nota = null,
            bool complejo = false,
            EcfProhibidoModo prohibidoModo = EcfProhibidoModo.Omitir)
            => new()
            {
                Nombre = nombre,
                Presence = presence,
                Orden = orden,
                Resolver = resolver,
                Validar = validar,
                Nota = nota,
                EsComplejo = complejo,
                ProhibidoModo = prohibidoModo
            };

        public static XElement? TablaFormasPago(EcfBuildContext ctx, int max = 7)
        {
            if (ctx.Documento.FormasPago.Count == 0) return null;
            var tabla = new XElement("TablaFormasPago");
            foreach (var f in ctx.Documento.FormasPago.Take(max))
            {
                tabla.Add(new XElement("FormaDePago",
                    new XElement("FormaPago", f.FormaPago),
                    new XElement("MontoPago", Money(f.Monto))));
            }
            return tabla.HasElements ? tabla : null;
        }

        public static XElement? TablaTelefono(EcfBuildContext ctx)
        {
            var tel = Telefono(ctx.Enc.TelefonoEmisor);
            if (tel == null) return null;
            return new XElement("TablaTelefonoEmisor", new XElement("TelefonoEmisor", tel));
        }

        /// <summary>Nodo Retencion de ítem (E41: minOccurs=1, orden antes de NombreItem).</summary>
        /// <param name="emitirMontosAunqueCero">
        /// Si true, emite MontoITBISRetenido y MontoISRRetenido aunque sean 0
        /// (hallazgo testecf E41: omisión de MontoITBISRetenido → rechazo).
        /// </param>
        /// <param name="soloIsr">
        /// Si true (E47), solo emite MontoISRRetenido (obligatorio en XSD; sin MontoITBISRetenido).
        /// </param>
        public static XElement RetencionItem(EcfBuildContext ctx, bool emitirMontosAunqueCero = false, bool soloIsr = false)
        {
            var l = ctx.LineaActual
                ?? throw new InvalidOperationException("RetencionItem requiere LineaActual.");
            var ind = l.IndicadorAgenteRetencionoPercepcion ?? 1;
            if (ind is not (1 or 2))
                throw new InvalidOperationException("IndicadorAgenteRetencionoPercepcion debe ser 1 o 2.");

            var el = new XElement("Retencion",
                new XElement("IndicadorAgenteRetencionoPercepcion", ind));

            var itbis = l.MontoItbisRetenido ?? 0m;
            var isr = l.MontoIsrRetenido ?? 0m;

            if (soloIsr)
            {
                el.Add(new XElement("MontoISRRetenido", Money(isr)));
                return el;
            }

            if (emitirMontosAunqueCero || itbis > 0)
                el.Add(new XElement("MontoITBISRetenido", Money(itbis)));
            if (emitirMontosAunqueCero || isr > 0)
                el.Add(new XElement("MontoISRRetenido", Money(isr)));
            return el;
        }
    }
}
