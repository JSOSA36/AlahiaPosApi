using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("CertecfInboundLog")]
    public class CertecfInboundLog
    {
        [Key]
        public int IdLog { get; set; }

        public int? IdEmpresa { get; set; }

        [MaxLength(11)]
        public string Rnc { get; set; } = "";

        [MaxLength(20)]
        public string Tipo { get; set; } = "";

        [MaxLength(20)]
        public string? Encf { get; set; }

        [MaxLength(30)]
        public string? Estado { get; set; }

        [MaxLength(1000)]
        public string? Mensaje { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}
