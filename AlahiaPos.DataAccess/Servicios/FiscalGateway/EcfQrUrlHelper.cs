using AlahiaPos.Entities.Dto.Fiscal;
using AlahiaPos.Entities.Fiscal;
using System;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway
{
    /// <summary>
    /// Normaliza UrlQR para almacenamiento e impresión térmica.
    /// Invoice (PG.eInvoicing) suele devolver <c>data:image/png;base64,...</c>,
    /// que no cabe en ECFEncabezado.UrlQR (NVARCHAR 500) ni sirve para ESC/POS PrintQRCode.
    /// El suplidor indicó usar la URL de consulta DGII (él no la manda).
    /// </summary>
    public static class EcfQrUrlHelper
    {
        public const int MaxStoredLength = 500;

        /// <summary>
        /// Conserva una URL http(s) usable, salvo E32 RFCE apuntando a ConsultaTimbre
        /// (esa página no tiene resúmenes FC; DGII responde “no existe”).
        /// Si el proveedor mandó imagen u otro valor inválido, construye la URL oficial.
        /// </summary>
        public static string? ResolveForStorage(
            string? providerQr,
            FiscalDocumentoElectronico documento,
            DateTime? fechaFirma,
            string? securityCode)
        {
            var enc = documento.Encabezado;
            return ResolveFromEncabezadoFields(
                providerQr,
                documento.AmbienteDgii,
                enc.RncEmisor,
                enc.RncComprador,
                enc.Encf,
                enc.FechaEmision,
                enc.MontoTotal,
                fechaFirma,
                securityCode,
                enc.TipoEcf.ToString());
        }

        /// <summary>
        /// Misma lógica cuando solo hay datos del ECF ya persistido (consulta estado, reimpresión).
        /// </summary>
        public static string? ResolveFromEncabezadoFields(
            string? providerQr,
            string? ambienteDgii,
            string rncEmisor,
            string? rncComprador,
            string encf,
            DateTime fechaEmision,
            decimal montoTotal,
            DateTime? fechaFirma,
            string? securityCode,
            string? tipoEcf = null)
        {
            var tipo = EcfConsultaTimbreUrl.ParseTipoEcf(tipoEcf, encf);
            var rfce = EcfConsultaTimbreUrl.EsCanalRfce(tipo, montoTotal);
            var ambiente = ambienteDgii
                           ?? EcfConsultaTimbreUrl.ExtraerAmbienteDeUrl(providerQr);

            if (IsUsableHttpUrl(providerQr)
                && !(rfce && EcfConsultaTimbreUrl.EsUrlConsultaTimbreEcf(providerQr)))
            {
                return Truncate(providerQr!.Trim(), MaxStoredLength);
            }

            if (string.IsNullOrWhiteSpace(securityCode) || string.IsNullOrWhiteSpace(encf) || string.IsNullOrWhiteSpace(rncEmisor))
                return null;

            // E31 ConsultaTimbre exige FechaFirma idéntica a la del XML firmado.
            // DateTime.Now (o la hora del ticket) no coincide → DGII responde “no existe”.
            var firma = fechaFirma
                        ?? EcfConsultaTimbreUrl.TryGetFechaFirma(providerQr);
            if (!firma.HasValue && !rfce)
                return null;

            return Truncate(
                EcfConsultaTimbreUrl.Build(
                    ambiente,
                    tipo,
                    rncEmisor,
                    rncComprador,
                    encf,
                    fechaEmision,
                    montoTotal,
                    firma ?? DateTime.MinValue,
                    securityCode.Trim()),
                MaxStoredLength);
        }

        public static bool IsUsableHttpUrl(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var v = value.Trim();
            if (v.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                return false;

            return v.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                   || v.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        private static string Truncate(string value, int max)
            => value.Length <= max ? value : value[..max];
    }
}
