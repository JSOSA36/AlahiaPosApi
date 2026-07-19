using System;

namespace AlahiaPos.Entities.Dto
{
    public class HistorialServicioClienteDto
    {
        public int IdFacturaHeader { get; set; }

        public int IdFacturaDetalle { get; set; }

        public DateTime FechaServicio { get; set; }

        public string? Hora { get; set; }

        public string NumeroFactura { get; set; } = string.Empty;

        public string EstadoFactura { get; set; } = string.Empty;

        public int? IdCliente { get; set; }

        public string NombreCliente { get; set; } = string.Empty;

        public int IdProducto { get; set; }

        public string NombreServicio { get; set; } = string.Empty;

        public decimal Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal Total { get; set; }

        public int? IdUsuario { get; set; }

        public string? NombreUsuario { get; set; }
    }
}
