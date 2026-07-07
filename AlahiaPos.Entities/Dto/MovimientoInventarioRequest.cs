using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class MovimientoInventarioRequest
    {
        public string TipoMovimiento { get; set; }
           = string.Empty;

        public string Motivo { get; set; }
            = string.Empty;

        public string? Referencia { get; set; }

        public string? Observacion { get; set; }

        public int IdEmpresa { get; set; }

        public int? IdUsuario { get; set; }

        public List<MovimientoInventarioDetalleRequest>
            Detalles
        { get; set; }
            = new();
    }
}
    

