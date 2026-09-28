using AlahiaPos.DataAccess.Seguridad;
using AlahiaPos.Entities.Dto.Fiscal;
using AlahiaPos.Entities.Fiscal;
using System.Globalization;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto
{
    public static class DgiiDirectoMapper
    {
        public static FiscalEnvioResultado ToEnvioResultado(
            DgiiHttpResultado resp,
            FiscalDocumentoElectronico doc,
            string xmlSinFirmar,
            string xmlFirmado,
            DateTime fechaFirma,
            DgiiDirectoSettings settings)
        {
            var codigoSeguridad = XmlSigner.ExtractCodigoSeguridad(xmlFirmado);
            var urlQr = BuildQrUrl(doc, fechaFirma, codigoSeguridad, settings);

            var trackOk = !string.IsNullOrWhiteSpace(resp.TrackId)
                          && !string.Equals(resp.TrackId, "00000000-0000-0000-0000-000000000000", StringComparison.OrdinalIgnoreCase);

            var estado = NormalizeEstado(resp.Estado, resp.Codigo, resp.Ok, trackOk);
            var exitoso = estado is "Aceptado" or "AceptadoCondicional" or "EnProceso" || (trackOk && resp.Ok);

            if (!exitoso && !trackOk)
            {
                return new FiscalEnvioResultado
                {
                    Exitoso = false,
                    Estado = "Error",
                    CodigoError = resp.Codigo ?? $"HTTP_{resp.StatusCode}",
                    Mensajes = resp.Mensajes.Count > 0 ? resp.Mensajes : new List<string> { Trunc(resp.Body, 500) },
                    XmlSinFirmar = xmlSinFirmar,
                    XmlFirmado = xmlFirmado,
                    XmlRespuesta = resp.Body,
                    SecurityCode = codigoSeguridad,
                    UrlQR = urlQr,
                    FechaFirma = fechaFirma,
                    Encf = doc.Encabezado.Encf
                };
            }

            return new FiscalEnvioResultado
            {
                Exitoso = true,
                TrackId = resp.TrackId,
                Estado = estado,
                Encf = resp.Encf ?? doc.Encabezado.Encf,
                CodigoError = exitoso ? null : resp.Codigo,
                Mensajes = resp.Mensajes,
                FechaRecepcion = resp.FechaRecepcion,
                SecurityCode = codigoSeguridad,
                UrlQR = urlQr,
                FechaFirma = fechaFirma,
                XmlSinFirmar = xmlSinFirmar,
                XmlFirmado = xmlFirmado,
                XmlRespuesta = resp.Body
            };
        }

        public static FiscalConsultaResultado ToConsultaResultado(DgiiHttpResultado resp, string trackId)
        {
            return new FiscalConsultaResultado
            {
                TrackId = resp.TrackId ?? trackId,
                Estado = NormalizeEstado(resp.Estado, resp.Codigo, resp.Ok, !string.IsNullOrWhiteSpace(resp.TrackId)),
                Encf = resp.Encf,
                Rnc = resp.Rnc,
                CodigoError = resp.Ok ? null : (resp.Codigo ?? $"HTTP_{resp.StatusCode}"),
                Mensajes = resp.Mensajes.Count > 0 ? resp.Mensajes : new List<string> { Trunc(resp.Body, 500) }
            };
        }

        public static string BuildQrUrl(
            FiscalDocumentoElectronico doc,
            DateTime fechaFirma,
            string? codigoSeguridad,
            DgiiDirectoSettings settings)
        {
            var enc = doc.Encabezado;
            var ambiente = !string.IsNullOrWhiteSpace(doc.AmbienteDgii)
                ? doc.AmbienteDgii
                : settings.AmbientePath;
            return EcfConsultaTimbreUrl.Build(
                ambiente,
                enc.TipoEcf,
                enc.RncEmisor,
                enc.RncComprador,
                enc.Encf,
                enc.FechaEmision,
                enc.MontoTotal,
                fechaFirma,
                codigoSeguridad ?? "");
        }

        private static string NormalizeEstado(string? estado, string? codigo, bool httpOk, bool trackOk)
        {
            if (!string.IsNullOrWhiteSpace(estado))
            {
                var e = estado.Trim();
                if (e.Contains("AceptadoCondicional", StringComparison.OrdinalIgnoreCase) ||
                    e.Contains("aceptado condicional", StringComparison.OrdinalIgnoreCase))
                    return "AceptadoCondicional";
                if (e.Contains("Aceptado", StringComparison.OrdinalIgnoreCase)) return "Aceptado";
                if (e.Contains("Rechazado", StringComparison.OrdinalIgnoreCase)) return "Rechazado";
                if (e.Contains("Proceso", StringComparison.OrdinalIgnoreCase) ||
                    e.Contains("Pendiente", StringComparison.OrdinalIgnoreCase))
                    return "EnProceso";
                return e;
            }

            if (codigo == "1") return "Aceptado";
            if (trackOk && httpOk) return "EnProceso";
            if (!httpOk) return "Error";
            return "Desconocido";
        }

        private static string SoloDigitos(string s) => new string((s ?? "").Where(char.IsDigit).ToArray());
        private static string Trunc(string? s, int max)
            => string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..max] + "...";
    }
}
