using System.Collections.Generic;
using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Events
{
    /// <summary>
    /// Contrato estándar: un módulo origen solicita crear un trabajo de producción.
    /// El motor no conoce el módulo; solo consume este payload (snapshot).
    /// </summary>
    public class ProduccionTrabajoSolicitadoEvent : DomainEventBase
    {
        public override string TipoEvento => DomainEventTypes.ProduccionTrabajoSolicitado;

        public string TipoTrabajo { get; set; } = "";
        public string OrigenModulo { get; set; } = "";
        public string OrigenTipo { get; set; } = "";
        public int OrigenId { get; set; }
        public string IdempotencyKey { get; set; } = "";
        public string NumeroVisible { get; set; } = "";
        public string NombreVisible { get; set; } = "";
        public string? Referencia { get; set; }
        public string? EtiquetaContexto { get; set; }
        public string? Observacion { get; set; }
        public int? IdUsuarioSolicita { get; set; }
        public string? Prioridad { get; set; }

        /// <summary>Etapa 1: nullable e ignorado por el motor hasta Etapa 2.</summary>
        public string? PlantillaCodigo { get; set; }

        public List<ProduccionTrabajoItemSolicitudDto> Items { get; set; } = new();
    }
}
