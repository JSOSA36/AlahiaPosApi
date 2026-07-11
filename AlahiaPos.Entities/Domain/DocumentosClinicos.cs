using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("DocumentosClinicos")]
    public class DocumentosClinicos
    {
        [Key]
        public int IdDocumentoClinico { get; set; }

        public int IdEmpresa { get; set; }

        [ForeignKey(nameof(IdEmpresa))]
        public Empresas? Empresa { get; set; }

        public int IdCliente { get; set; }

        [ForeignKey(nameof(IdCliente))]
        public Clientes? Cliente { get; set; }

        public int IdPlantilla { get; set; }

        [ForeignKey(nameof(IdPlantilla))]
        public PlantillasDocumentosClinicos? Plantilla { get; set; }

        [Required]
        [MaxLength(100)]
        public string TipoDocumento { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string NumeroDocumento { get; set; } = string.Empty;

        public DateTime FechaEmision { get; set; }

        [MaxLength(200)]
        public string? NombreDoctor { get; set; }

        public int? HorasReposo { get; set; }

        [MaxLength(500)]
        public string? Procedimiento { get; set; }

        public string? Observaciones { get; set; }

        [Required]
        public string ContenidoHTMLFinal { get; set; } = string.Empty;

        public string? DatosJSON { get; set; }

        public int IdUsuarioCreacion { get; set; }

        [ForeignKey(nameof(IdUsuarioCreacion))]
        public Usuarios? UsuarioCreacion { get; set; }

        [Required]
        [MaxLength(30)]
        public string Estado { get; set; } = "EMITIDO";

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
