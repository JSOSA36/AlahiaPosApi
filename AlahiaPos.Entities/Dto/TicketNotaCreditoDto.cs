using System;
using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto
{
    public class TicketNotaCreditoDto
    {
        public int IdNotaCredito { get; set; }

        public string NumeroDocumento { get; set; } = "";

        public string NCF { get; set; } = "";

        public string NCFModificado { get; set; } = "";

        public string NumeroFactura { get; set; } = "";

        public DateTime Fecha { get; set; }

        public string Cliente { get; set; } = "";

        public string RNC { get; set; } = "";

        public decimal SubTotal { get; set; }

        public decimal TotalItbis { get; set; }

        public decimal Total { get; set; }

        public string NombreEmpresa { get; set; } = "";

        public string TelefonoEmpresa { get; set; } = "";

        public string DireccionEmpresa { get; set; } = "";

        public string? TrackId { get; set; }

        public string? EstadoDgii { get; set; }

        public string? TipoDocumentoOrigen { get; set; }

        public decimal SaldoDisponible { get; set; }

        public bool EmisionPendiente { get; set; }

        public string? MensajeEmision { get; set; }

        public DateTime? FechaEmisionEcf { get; set; }

        public string? SecurityCode { get; set; }

        public string? UrlQR { get; set; }

        public string? RncEmisor { get; set; }

        public List<TicketNotaCreditoDetalleDto> Detalles { get; set; }
            = new();
    }

    public class TicketNotaCreditoDetalleDto
    {
        public decimal Cantidad { get; set; }

        public string Descripcion { get; set; } = "";

        public decimal Precio { get; set; }

        public decimal SubTotal { get; set; }
    }
}
