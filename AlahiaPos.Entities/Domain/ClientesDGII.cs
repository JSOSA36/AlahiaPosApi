using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class ClientesDGII
    {
        [Key]
        [StringLength(20)]
        public string RNC { get; set; } = string.Empty;

        [StringLength(200)]
        public string? RazonSocial { get; set; }

        [StringLength(200)]
        public string? NombreComercial { get; set; }

        [StringLength(50)]
        public string? Estado { get; set; }

        [StringLength(50)]
        public string? Regimen { get; set; }

        // 🔥 opcional pero PRO
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;
    }
}
