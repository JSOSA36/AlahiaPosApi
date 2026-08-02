using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class PagoDTO
    {
        public string Metodo { get; set; }
        public decimal Monto { get; set; }

        /// <summary>Saldo a favor a consumir cuando Metodo = NotaCredito.</summary>
        public int? IdSaldoAFavor { get; set; }

        public int? IdNotaCredito { get; set; }

        /// <summary>e-NCF o número interno de la NC (auditoría / UI).</summary>
        public string? NcfNotaCredito { get; set; }
    }

    public static class FormaPagoNotaCredito
    {
        public const string Metodo = "NotaCredito";

        public static bool EsNotaCredito(string? metodo)
            => string.Equals((metodo ?? "").Trim(), Metodo, StringComparison.OrdinalIgnoreCase);
    }
}
