using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class EntradaFinancieraDto
    {
        public int IdEmpresa { get; set; }

        public int IdUsuario { get; set; }

        public int IdCuentaDestino { get; set; }

        public decimal Monto { get; set; }

        public string Motivo { get; set; } = string.Empty;

        public string? Observacion { get; set; }

        public string? Categoria { get; set; }

        public int? ReferenciaId { get; set; }

        public string? ReferenciaTipo { get; set; }

        public string? ClaveIdempotencia { get; set; }
    }
}
