using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class SalidaFinancieraDto
    {
        public int IdEmpresa { get; set; }

        public int IdUsuario { get; set; }

        public int IdCuentaOrigen { get; set; }

        public decimal Monto { get; set; }

        public string Motivo { get; set; } = string.Empty;

        public string? Observacion { get; set; }
    }
}
