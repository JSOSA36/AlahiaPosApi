using System;
using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto
{
    public class TicketFacturaClienteDto
    {
        public int NumeroFactura { get; set; }

        public string NumeroDocumento { get; set; } = "";

        public DateTime Fecha { get; set; }

        public string Hora { get; set; } = "";

        public string Cliente { get; set; } = "";

        public string? RncCliente { get; set; }

        public decimal SubTotal { get; set; }

        public decimal TotalItbis { get; set; }

        public decimal TotalDescuento { get; set; }

        public decimal Total { get; set; }

        public decimal Pagado { get; set; }

        public decimal Pendiente { get; set; }

        public string? TipoFactura { get; set; }

        public string? FormaPago { get; set; }

        public List<TicketFacturaClientePagoDto> Pagos { get; set; } = new();

        public string NombreEmpresa { get; set; } = "";

        public string TelefonoEmpresa { get; set; } = "";

        public string DireccionEmpresa { get; set; } = "";

        public string? RncEmpresa { get; set; }

        /// <summary>NCF tradicional o e-NCF (E31…).</summary>
        public string? NCF { get; set; }

        public string? TipoComprobante { get; set; }

        /// <summary>True si el documento es e-CF DGII (no imprimir TrackId).</summary>
        public bool EsComprobanteElectronico { get; set; }

        public string? TipoECF { get; set; }

        public string? SecurityCode { get; set; }

        public string? UrlQR { get; set; }

        public DateTime? FechaFirma { get; set; }

        public DateTime? FechaEmisionEcf { get; set; }

        public string? EstadoDgii { get; set; }

        public List<TicketFacturaClienteDetalleDto> Detalles { get; set; } = new();
    }
}
