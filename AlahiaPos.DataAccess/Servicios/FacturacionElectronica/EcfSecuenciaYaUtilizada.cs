using System;
using System.Collections.Generic;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto.Fiscal;

namespace AlahiaPos.DataAccess.Servicios.FacturacionElectronica
{
    /// <summary>
    /// Un e-NCF enviado al proveedor no se reutiliza.
    /// En operación comercial, "secuencia ya utilizada" obliga a reservar el siguiente.
    /// En CerteCF el e-NCF lo fija el Excel: se da por Aceptado y se sigue con el siguiente caso del set.
    /// </summary>
    public static class EcfSecuenciaYaUtilizada
    {
        private const string ZeroGuid = "00000000-0000-0000-0000-000000000000";

        public static bool EnMensajes(string? mensaje, IEnumerable<string>? extras = null)
        {
            if (EsTexto(mensaje)) return true;
            if (extras == null) return false;
            foreach (var m in extras)
            {
                if (EsTexto(m)) return true;
            }
            return false;
        }

        public static bool EnResultado(FiscalEnvioResultado resultado)
        {
            if (resultado == null) return false;
            var estado = (resultado.Estado ?? "").Trim();
            if (estado.Contains("Aceptado", StringComparison.OrdinalIgnoreCase)
                || estado.Equals("Resumen", StringComparison.OrdinalIgnoreCase))
                return false;

            if (resultado.SecuenciaUtilizada == true) return true;
            if (EsTexto(resultado.CodigoError)) return true;
            return EnMensajes(null, resultado.Mensajes);
        }

        /// <summary>
        /// Ya se mandó al proveedor (o él lo consumió): no reenviar el mismo e-NCF.
        /// Un Pendiente/Error local sin FechaEnvio ni TrackId sí se puede reutilizar.
        /// </summary>
        public static bool EncabezadoYaFueEnviado(ECFEncabezado ecf)
        {
            if (EnMensajes(ecf.MensajeRespuesta)) return true;
            if (ecf.FechaEnvio.HasValue) return true;
            if (!string.IsNullOrWhiteSpace(ecf.TrackId)
                && !string.Equals(ecf.TrackId.Trim(), ZeroGuid, StringComparison.OrdinalIgnoreCase))
                return true;

            var estado = (ecf.EstadoDGII ?? "").Trim();
            if (estado.Contains("Aceptado", StringComparison.OrdinalIgnoreCase)
                || estado.Equals("Rechazado", StringComparison.OrdinalIgnoreCase))
                return true;

            var doc = (ecf.EstadoDocumento ?? "").Trim();
            return doc.Equals(EstadoDocumentoElectronico.Enviado, StringComparison.OrdinalIgnoreCase)
                || doc.Equals(EstadoDocumentoElectronico.Rechazado, StringComparison.OrdinalIgnoreCase)
                || doc.Equals(EstadoDocumentoElectronico.Aceptado, StringComparison.OrdinalIgnoreCase);
        }

        public static bool EsTexto(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return false;
            var t = valor.Trim().ToLowerInvariant();
            return t.Contains("ya ha sido utilizado")
                || t.Contains("ya han sido utilizados")
                || t.Contains("utilizados previamente")
                || t.Contains("secuencia ya")
                || t.Contains("secuencia utilizada")
                || t.Contains("already been used")
                || t.Contains("already used")
                || t.Contains("encf ya existe")
                || t.Contains("ncf ya existe")
                || t.Contains("número de secuencia ya")
                || t.Contains("numero de secuencia ya");
        }

        /// <summary>
        /// En CerteCF el e-NCF lo fija el Excel: no se incrementa.
        /// Si DGII dice que ya se usó, ese comprobante ya fue Aceptado;
        /// se da por hecho y se sigue con el siguiente del set.
        /// </summary>
        public static string MensajeAceptadoPorConsumo(string encf) =>
            $"DGII ya tenía {encf} (secuencia utilizada). Se da por Aceptado; continúe con el siguiente del Excel.";
    }
}
