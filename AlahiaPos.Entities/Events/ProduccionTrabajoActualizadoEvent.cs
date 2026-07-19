using System.Collections.Generic;
using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Events
{
    /// <summary>
    /// El origen actualizó un documento ya vinculado a producción (p.ej. editar orden POS).
    /// Si OrigenIdAnterior &gt; 0, el motor reutiliza el trabajo existente y actualiza el snapshot.
    /// </summary>
    public class ProduccionTrabajoActualizadoEvent : DomainEventBase
    {
        public override string TipoEvento => DomainEventTypes.ProduccionTrabajoActualizado;

        public string TipoTrabajo { get; set; } = "";
        public string OrigenModulo { get; set; } = "";
        public string OrigenTipo { get; set; } = "";
        public int OrigenId { get; set; }

        /// <summary>Id de origen previo cuando el módulo recrea el documento (delete+insert).</summary>
        public int OrigenIdAnterior { get; set; }

        public string IdempotencyKey { get; set; } = "";
        public string NumeroVisible { get; set; } = "";
        public string NombreVisible { get; set; } = "";
        public string? Referencia { get; set; }
        public string? EtiquetaContexto { get; set; }
        public string? Observacion { get; set; }
        public int? IdUsuarioSolicita { get; set; }
        public string? Prioridad { get; set; }
        public string? PlantillaCodigo { get; set; }

        public List<ProduccionTrabajoItemSolicitudDto> Items { get; set; } = new();
    }
}
