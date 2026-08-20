using System;

namespace AlahiaPos.Entities.Dto
{
    /// <summary>Datos para recibo térmico de abono / cobro CxC.</summary>
    public class TicketReciboAbonoDto
    {
        public int IdPago { get; set; }

        public int IdFacturaHeader { get; set; }

        public string NumeroDocumento { get; set; } = "";

        public string? NcfFactura { get; set; }

        public DateTime FechaPago { get; set; }

        public string Cliente { get; set; } = "";

        public string? RncCliente { get; set; }

        public string FormaPago { get; set; } = "";

        public decimal MontoAbono { get; set; }

        public decimal TotalFactura { get; set; }

        public decimal PagadoAcumulado { get; set; }

        public decimal Pendiente { get; set; }

        public string? Nota { get; set; }

        public string NombreEmpresa { get; set; } = "";

        public string TelefonoEmpresa { get; set; } = "";

        public string DireccionEmpresa { get; set; } = "";

        public string? RncEmpresa { get; set; }
    }
}
