using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("ClienteDireccion")]
    public class ClienteDireccion
    {
        [Key]
        public int IdClienteDireccion { get; set; }

        public int IdEmpresa { get; set; }

        public int IdCliente { get; set; }

        [MaxLength(300)]
        public string Direccion { get; set; } = "";

        [MaxLength(300)]
        public string? Referencia { get; set; }

        [Column(TypeName = "decimal(10,7)")]
        public decimal? Latitud { get; set; }

        [Column(TypeName = "decimal(10,7)")]
        public decimal? Longitud { get; set; }

        [MaxLength(120)]
        public string? NombreDestinatario { get; set; }

        [MaxLength(30)]
        public string? Telefono { get; set; }

        public bool Activa { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
