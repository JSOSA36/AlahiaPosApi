using System;

namespace AlahiaPos.Entities.Dto
{
    public class PlantillaDocumentoClinicoDto
    {
        public int IdPlantilla { get; set; }

        public int IdEmpresa { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string TipoDocumento { get; set; } = string.Empty;

        public string ContenidoHTML { get; set; } = string.Empty;

        public bool EsPredeterminada { get; set; }

        public bool Activa { get; set; } = true;

        public DateTime? FechaCreacion { get; set; }

        public int IdUsuarioCreacion { get; set; }
    }
}
