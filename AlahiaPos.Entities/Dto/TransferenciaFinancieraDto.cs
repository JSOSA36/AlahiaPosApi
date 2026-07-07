using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class TransferenciaFinancieraDto
    {
        public int IdEmpresa { get; set; }

        public int IdUsuario { get; set; }

        public int IdCuentaOrigen { get; set; }

        public int IdCuentaDestino { get; set; }

        public decimal Monto { get; set; }

        public string Motivo { get; set; }

        public string? Observacion { get; set; }
    }
}
