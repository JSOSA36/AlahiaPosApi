using System;

namespace AlahiaPos.Entities.Dto
{
    public class NotaCreditoListadoDto
    {
        public int IdNotaCredito { get; set; }

        public string NumeroDocumento { get; set; } = "";

        public string NCF { get; set; } = "";

        public string NCFModificado { get; set; } = "";

        public int IdFacturaHeader { get; set; }

        public string NumeroFactura { get; set; } = "";

        public string NombreCliente { get; set; } = "";

        public string RNC { get; set; } = "";

        public decimal SubTotal { get; set; }

        public decimal TotalItbis { get; set; }

        public decimal Total { get; set; }

        public string Observacion { get; set; } = "";

        public DateTime FechaInseccion { get; set; }

        public int CantidadProductos { get; set; }

        public string ProductosDevueltos { get; set; } = "";

        public bool TieneComprobante { get; set; }

        public string? TrackId { get; set; }

        public string? EstadoDgii { get; set; }

        public string? TipoDocumentoOrigen { get; set; }

        public decimal SaldoDisponible { get; set; }

        public string Estado { get; set; } = "Activa";

        public bool EmisionPendiente { get; set; }

        public DateTime? FechaEmisionEcf { get; set; }
    }
}
