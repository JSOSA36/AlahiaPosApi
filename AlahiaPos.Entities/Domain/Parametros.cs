using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class Parametros
    {
        [Key]
        public int IdParametro { get; set; }

        public int IdEmpresa { get; set; }

        public int? IdSucursal { get; set; }

        [MaxLength(20)]
        public string Tipo { get; set; } = string.Empty;
        // EMPRESA / POS

        [MaxLength(50)]
        public string? CodigoPOS { get; set; }
        // Solo aplica si Tipo = POS

        [Required]
        [MaxLength(100)]
        public string Clave { get; set; } = string.Empty;

        public string? Valor { get; set; }

        [MaxLength(200)]
        public string? Descripcion { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public bool Activo { get; set; } = true;
    }
}
