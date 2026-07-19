using System;

namespace AlahiaPos.Entities.Dto
{
    public class UltimoServicioClienteDto
    {
        public int IdFacturaDetalle { get; set; }

        public int IdFacturaHeader { get; set; }

        public DateTime FechaServicio { get; set; }

        public string NombreServicio { get; set; } = string.Empty;

        public decimal PrecioUnitario { get; set; }

        public decimal Cantidad { get; set; }

        public decimal Total { get; set; }

        public string NumeroFactura { get; set; } = string.Empty;
    }
}
