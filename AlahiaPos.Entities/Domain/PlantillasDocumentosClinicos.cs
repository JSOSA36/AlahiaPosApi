using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("PlantillasDocumentosClinicos")]
    public class PlantillasDocumentosClinicos
    {
        [Key]
        public int IdPlantilla { get; set; }

        public int IdEmpresa { get; set; }

        [ForeignKey(nameof(IdEmpresa))]
        public Empresas? Empresa { get; set; }

        [Required]
        [MaxLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string TipoDocumento { get; set; } = string.Empty;

        [Required]
        public string ContenidoHTML { get; set; } = string.Empty;

        public bool EsPredeterminada { get; set; }

        public bool Activa { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public int IdUsuarioCreacion { get; set; }

        [ForeignKey(nameof(IdUsuarioCreacion))]
        public Usuarios? UsuarioCreacion { get; set; }
    }
}
