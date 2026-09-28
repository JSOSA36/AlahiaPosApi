using System;

namespace AlahiaPos.Entities.Dto
{
    public class ClienteSaldoAFavorListadoDto
    {
        public int IdSaldoAFavor { get; set; }

        public int IdCliente { get; set; }

        public string NombreCliente { get; set; } = "";

        public int IdNotaCredito { get; set; }

        public int IdFacturaHeader { get; set; }

        /// <summary>e-NCF / NCF de la factura origen (incluye consumo al portador).</summary>
        public string? NcfFacturaOrigen { get; set; }

        /// <summary>Número interno de la factura origen.</summary>
        public string? NumeroFactura { get; set; }

        public string? NcfNotaCredito { get; set; }

        public string? NumeroDocumentoNotaCredito { get; set; }

        public decimal MontoOriginal { get; set; }

        public decimal SaldoDisponible { get; set; }

        public string Estado { get; set; } = "";

        public DateTime Fecha { get; set; }

        public string? Observacion { get; set; }
    }
}
