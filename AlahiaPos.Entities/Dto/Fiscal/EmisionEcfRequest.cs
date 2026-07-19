using AlahiaPos.Entities.Domain;
using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto.Fiscal
{
    public class EmisionEcfRequest
    {
        public int IdEmpresa { get; set; }
        public int TipoEcfDgii { get; set; }
        public OrigenDocumento OrigenDocumento { get; set; }
        public int IdOrigen { get; set; }
        public int IdUsuario { get; set; }
        public string? NcfModificado { get; set; }
        public DateTime? FechaNcfModificado { get; set; }
        public int? CodigoModificacion { get; set; }
        public string? RazonModificacion { get; set; }
    }

    public class EmisionEcfResultado
    {
        public bool Exitoso { get; set; }
        public string? Encf { get; set; }
        public int? IdEcf { get; set; }
        public string? MensajeError { get; set; }
        public int SecuenciasRestantes { get; set; }

        public static EmisionEcfResultado Fallo(string mensaje) => new()
        {
            Exitoso = false,
            MensajeError = mensaje
        };
    }

    /// <summary>
    /// Resultado extendido para emisión síncrona (POS preview).
    /// Incluye datos del proveedor: QR, TrackId, estado DGII.
    /// </summary>
    public class EmisionEcfResultadoCompleto : EmisionEcfResultado
    {
        public string? TrackId { get; set; }
        public string? TransmissionJobId { get; set; }
        public string? EstadoDgii { get; set; }
        public string? UrlQR { get; set; }
        public string? SecurityCode { get; set; }
        public List<string> MensajesDgii { get; set; } = new();
        public string? RncEmisor { get; set; }
        public string? RazonSocialEmisor { get; set; }
    }
}
