using System;
using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto.Fiscal
{
    public class FiscalEnvioResultado
    {
        public bool Exitoso { get; set; }
        public string? TrackId { get; set; }
        /// <summary>Job id del Transmission Engine del proveedor (si aplica).</summary>
        public string? TransmissionJobId { get; set; }
        public string Estado { get; set; } = "";
        public string? Encf { get; set; }
        public DateTime? FechaRecepcion { get; set; }
        public string? CodigoError { get; set; }
        public List<string> Mensajes { get; set; } = new();
        public string? SecurityCode { get; set; }
        public string? UrlQR { get; set; }
        public DateTime? FechaFirma { get; set; }

        /// <summary>XML sin firmar (solo DgiiDirecto).</summary>
        public string? XmlSinFirmar { get; set; }
        /// <summary>XML firmado enviado a DGII (solo DgiiDirecto).</summary>
        public string? XmlFirmado { get; set; }
        /// <summary>Cuerpo crudo de respuesta DGII (solo DgiiDirecto).</summary>
        public string? XmlRespuesta { get; set; }

        /// <summary>
        /// El proveedor indicó que ese e-NCF ya se consumió. Hay que emitir el siguiente, no reenviar.
        /// </summary>
        public bool? SecuenciaUtilizada { get; set; }

        public static FiscalEnvioResultado Error(string codigo, string mensaje) => new()
        {
            Exitoso = false,
            Estado = "Error",
            CodigoError = codigo,
            Mensajes = new List<string> { mensaje }
        };
    }

    public class FiscalConsultaResultado
    {
        public string TrackId { get; set; } = "";
        public string Estado { get; set; } = "";
        public string? Encf { get; set; }
        public string? Rnc { get; set; }
        public string? CodigoError { get; set; }
        public List<string> Mensajes { get; set; } = new();
        public string? SecurityCode { get; set; }
        public string? UrlQR { get; set; }
        public DateTime? FechaFirma { get; set; }

        public bool EsAceptado => Estado == "Aceptado";
        public bool EsRechazado => Estado == "Rechazado";
        public bool EstaPendiente => !EsAceptado && !EsRechazado;
    }
}
