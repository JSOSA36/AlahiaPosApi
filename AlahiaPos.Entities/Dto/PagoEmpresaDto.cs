using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class PagoEmpresaDto
    {
        public int Id { get; set; }
        public int IdEmpresa { get; set; }
        public string? NombreEmpresa { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaSubida { get; set; }

        public string ArchivoUrl { get; set; }

        public string Estado { get; set; } // PENDIENTE, APROBADO, RECHAZADO
        public string Observacion { get; set; }

        public DateTime? FechaValidacion { get; set; }
        public string UsuarioValida { get; set; }
        public DateTime? FechaPago { get; set; }
        public string? Banco { get; set; }
        public string? Referencia { get; set; }
        public int? IdCiclo { get; set; }
    }
}
