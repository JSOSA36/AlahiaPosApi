using AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto;
using AlahiaPos.Entities.Dto.Fiscal;
using System;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway
{
    /// <summary>
    /// Normaliza UrlQR para almacenamiento e impresión térmica.
    /// Invoice (PG.eInvoicing) suele devolver <c>data:image/png;base64,...</c>,
    /// que no cabe en ECFEncabezado.UrlQR (NVARCHAR 500) ni sirve para ESC/POS PrintQRCode.
    /// </summary>
    public static class EcfQrUrlHelper
    {
        public const int MaxStoredLength = 500;

        /// <summary>
        /// Si el proveedor ya envió una URL http(s) usable, la conserva;
        /// si envió imagen base64 u otro valor inválido, construye ConsultaTimbre DGII.
        /// </summary>
        public static string? ResolveForStorage(
            string? providerQr,
            FiscalDocumentoElectronico documento,
            DateTime? fechaFirma,
            string? securityCode)
        {
            if (IsUsableHttpUrl(providerQr))
                return Truncate(providerQr!.Trim(), MaxStoredLength);

            if (string.IsNullOrWhiteSpace(securityCode))
                return null;

            var settings = new DgiiDirectoSettings
            {
                Ambiente = DgiiAmbienteHelper.Normalize(documento.AmbienteDgii)
            };

            var firma = fechaFirma ?? DateTime.Now;
            return Truncate(
                DgiiDirectoMapper.BuildQrUrl(documento, firma, securityCode.Trim(), settings),
                MaxStoredLength);
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
            string? securityCode)
        {
            if (IsUsableHttpUrl(providerQr))
                return Truncate(providerQr!.Trim(), MaxStoredLength);

            if (string.IsNullOrWhiteSpace(securityCode) || string.IsNullOrWhiteSpace(encf) || string.IsNullOrWhiteSpace(rncEmisor))
                return null;

            var doc = new FiscalDocumentoElectronico
            {
                AmbienteDgii = ambienteDgii,
                Encabezado = new FiscalDocumentoEncabezado
                {
                    RncEmisor = rncEmisor,
                    RncComprador = rncComprador,
                    Encf = encf,
                    FechaEmision = fechaEmision,
                    MontoTotal = montoTotal
                }
            };

            return ResolveForStorage(null, doc, fechaFirma, securityCode);
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
