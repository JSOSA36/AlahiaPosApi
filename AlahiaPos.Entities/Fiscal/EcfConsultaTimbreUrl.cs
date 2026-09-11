using System;
using System.Globalization;
using System.Linq;

namespace AlahiaPos.Entities.Fiscal
{
    /// <summary>
    /// URL del QR de representación impresa. El suplidor (Invoice) no envía esta URL
    /// (manda data:image); hay que construirla como exige DGII.
    /// E32 &lt; RD$250,000 = RFCE → fc.dgii.gov.do ConsultaTimbreFC.
    /// El resto = e-CF → ecf.dgii.gov.do ConsultaTimbre.
    /// </summary>
    public static class EcfConsultaTimbreUrl
    {
        public const decimal UmbralRfceDop = 250_000m;

        public static bool EsCanalRfce(int tipoEcf, decimal montoTotal)
            => tipoEcf == 32 && montoTotal < UmbralRfceDop;

        public static int ParseTipoEcf(string? tipoEcf, string? encf = null)
        {
            var t = (tipoEcf ?? "").Trim();
            if (t.StartsWith("E", StringComparison.OrdinalIgnoreCase) && t.Length >= 3)
                t = t.Substring(1, 2);
            var digits = new string(t.Where(char.IsDigit).Take(2).ToArray());
            if (int.TryParse(digits, out var n) && n > 0)
                return n;

            var e = (encf ?? "").Trim();
            if (e.Length >= 3 && e.StartsWith("E", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(e.Substring(1, 2), out n) && n > 0)
                return n;

            return 0;
        }

        public static string AmbientePath(string? ambienteDgii)
        {
            var a = (ambienteDgii ?? "").Trim().ToLowerInvariant();
            return a switch
            {
                "cert" or "certecf" or "certificacion" or "certificación" or "certification" => "certecf",
                "prod" or "ecf" or "produccion" or "producción" or "production" => "ecf",
                "test" or "testecf" or "pruebas" or "sandbox" or "prueba" => "testecf",
                _ when a.Contains("cert") => "certecf",
                _ when a.Contains("prod") => "ecf",
                _ => "testecf"
            };
        }

        public static string? ExtraerAmbienteDeUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var u = url.Trim().ToLowerInvariant();
            if (u.Contains("/certecf/")) return "certecf";
            if (u.Contains("/testecf/")) return "testecf";
            if (u.Contains("/ecf/")) return "ecf";
            return null;
        }

        /// <summary>URL de consulta e-CF (no RFCE).</summary>
        public static bool EsUrlConsultaTimbreEcf(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            var u = url.Trim();
            if (EsUrlConsultaTimbreFc(u)) return false;
            return u.Contains("ConsultaTimbre", StringComparison.OrdinalIgnoreCase)
                   || u.Contains("consultatimbre", StringComparison.OrdinalIgnoreCase);
        }

        public static bool EsUrlConsultaTimbreFc(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            var u = url.Trim();
            return u.Contains("ConsultaTimbreFC", StringComparison.OrdinalIgnoreCase)
                   || u.Contains("consultatimbrefc", StringComparison.OrdinalIgnoreCase);
        }

        public static string Build(
            string? ambienteDgii,
            int tipoEcf,
            string rncEmisor,
            string? rncComprador,
            string encf,
            DateTime fechaEmision,
            decimal montoTotal,
            DateTime fechaFirma,
            string codigoSeguridad)
        {
            var amb = AmbientePath(ambienteDgii);
            var rncE = SoloDigitos(rncEmisor);
            var encfT = (encf ?? "").Trim();
            var monto = montoTotal.ToString("0.00", CultureInfo.InvariantCulture);
            var codigo = (codigoSeguridad ?? "").Trim();

            if (EsCanalRfce(tipoEcf, montoTotal))
            {
                var qsFc = string.Join("&", new[]
                {
                    "RncEmisor=" + Uri.EscapeDataString(rncE),
                    "ENCF=" + Uri.EscapeDataString(encfT),
                    "MontoTotal=" + Uri.EscapeDataString(monto),
                    "CodigoSeguridad=" + Uri.EscapeDataString(codigo)
                });
                return $"https://fc.dgii.gov.do/{amb}/ConsultaTimbreFC?{qsFc}";
            }

            var qs = string.Join("&", new[]
            {
                "RncEmisor=" + Uri.EscapeDataString(rncE),
                "RncComprador=" + Uri.EscapeDataString(SoloDigitos(rncComprador)),
                "ENCF=" + Uri.EscapeDataString(encfT),
                "FechaEmision=" + Uri.EscapeDataString(fechaEmision.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)),
                "MontoTotal=" + Uri.EscapeDataString(monto),
                // DGII: espacio → %20; no escapar ':' de la hora (ejemplo oficial).
                "FechaFirma=" + fechaFirma.ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture).Replace(" ", "%20"),
                "CodigoSeguridad=" + Uri.EscapeDataString(codigo)
            });
            return $"https://ecf.dgii.gov.do/{amb}/ConsultaTimbre?{qs}";
        }

        public static DateTime? TryGetFechaFirma(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var m = System.Text.RegularExpressions.Regex.Match(
                url,
                @"[?&]FechaFirma=([^&]+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!m.Success) return null;
            var raw = Uri.UnescapeDataString(m.Groups[1].Value.Replace("+", " "));
            return EcfDgiiFecha.Parse(raw);
        }

        private static string SoloDigitos(string? s)
            => string.IsNullOrWhiteSpace(s) ? "" : new string(s.Where(char.IsDigit).ToArray());
    }
}
