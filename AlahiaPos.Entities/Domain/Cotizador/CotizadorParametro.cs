using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain.Cotizador
{
    [Table("CotizadorParametro")]
    public class CotizadorParametro
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(80)]
        public string Clave { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Valor { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Descripcion { get; set; }

        public bool VisibleCliente { get; set; }
    }
}
