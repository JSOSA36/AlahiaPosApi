using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public record RfceRow(
        string ENCF,
        string RNCEmisor,
        string RazonSocialEmisor,
        DateTime FechaEmision,
        
    string? RNCComprador,
        string? IdentificadorExtranjero,
        string? RazonSocialComprador,

        decimal MontoGravadoTotal,
        decimal TotalITBIS,
        decimal MontoTotal,

        // Requeridos por formato RFCE
        string TipoIngresos = "01", // 01 operaciones (no financieros)
        string TipoPago = "1",      // 1 contado
        List<RfceFormaPagoRow>? FormasPago = null
    );
}
