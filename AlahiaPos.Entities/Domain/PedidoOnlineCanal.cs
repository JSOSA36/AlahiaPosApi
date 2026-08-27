using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("PedidoOnlineCanal")]
    public class PedidoOnlineCanal
    {
        [Key]
        public int IdCanal { get; set; }

        public int IdEmpresa { get; set; }

        [Required, MaxLength(80)]
        public string Slug { get; set; } = "";

        [Required, MaxLength(160)]
        public string NombrePublico { get; set; } = "";

        [MaxLength(40)]
        public string? WhatsApp { get; set; }

        [MaxLength(300)]
        public string? LogoUrl { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
