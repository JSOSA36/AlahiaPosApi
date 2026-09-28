using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    /// <summary>
    /// Aseguradora (ARS). El paciente de la venta es Clientes.IDCliente;
    /// esta entidad solo identifica al deudor de la cobertura.
    /// </summary>
    [Table("ArsAseguradoras")]
    public class ArsAseguradora : BaseEntity
    {
        [Key]
        public int IdArs { get; set; }

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? RNC { get; set; }

        [MaxLength(30)]
        public string? Telefono { get; set; }

        [MaxLength(250)]
        public string? Direccion { get; set; }

        [MaxLength(120)]
        public string? Email { get; set; }

        [MaxLength(120)]
        public string? Contacto { get; set; }

        [MaxLength(500)]
        public string? Observaciones { get; set; }

        public bool Activo { get; set; } = true;
    }
}
