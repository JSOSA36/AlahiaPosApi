using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public record RfceFormaPagoRow(
        string FormaPago,   // "1" efectivo, "2" transferencia, "3" tarjeta, etc.
        decimal MontoPago
    );
}
