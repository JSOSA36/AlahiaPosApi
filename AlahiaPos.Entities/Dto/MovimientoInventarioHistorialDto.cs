using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class MovimientoInventarioHistorialDto
    {
        public int Id { get; set; }

        public string TipoMovimiento { get; set; }
            = string.Empty;

        public string Motivo { get; set; }
            = string.Empty;

        public string? Referencia { get; set; }

        public string? Observacion { get; set; }

        public DateTime Fecha { get; set; }

        public int? IdUsuario { get; set; }

        public string Usuario { get; set; }
            = string.Empty;

        public List<
            MovimientoInventarioDetalleDto>
            Detalles
        {
            get;
            set;
        }
        =
        new();
    }
}
