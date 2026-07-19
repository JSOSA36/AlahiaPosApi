using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class PagoEmpresa
    {
        [Key]
        public int Id { get; set; }
        public int? IdEmpresa { get; set; }

        public decimal? Monto { get; set; }
        public DateTime? FechaSubida { get; set; }
        public DateTime? FechaValidacion { get; set; }
        public string? ArchivoUrl { get; set; } // comprobante
        public string? UsuarioValida { get; set; }
        public string? Estado { get; set; } // PENDIENTE, APROBADO, RECHAZADO

        public string? Observacion { get; set; } // opcional

        public DateTime? FechaPago { get; set; }

        [MaxLength(120)]
        public string? Banco { get; set; }

        [MaxLength(120)]
        public string? Referencia { get; set; }

        public int? IdCiclo { get; set; }
        public int? IdUsuarioReporta { get; set; }
    }
}
