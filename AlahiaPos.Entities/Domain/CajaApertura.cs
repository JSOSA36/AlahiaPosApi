using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class CajaApertura
    {
        [Key]
        public int IdCajaApertura { get; set; }

        public int IdEmpresa { get; set; }

        public int IdUsuario { get; set; }

        public DateTime FechaApertura { get; set; }
            = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoInicial { get; set; }

        public string Estado { get; set; }
            = "ABIERTA";

        public string? Observacion { get; set; }
    }
}
