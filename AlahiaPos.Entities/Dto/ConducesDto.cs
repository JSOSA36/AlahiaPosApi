using System;
using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto
{
    public class ConduceLineaRequest
    {
        public int IdFacturaDetalle { get; set; }
        public int IdProducto { get; set; }
        public decimal CantidadEntregada { get; set; }
    }

    public class CrearConduceRequest
    {
        public int IdEmpresa { get; set; }
        public int IdFacturaHeader { get; set; }
        public string? QuienEntrega { get; set; }
        public string? QuienRecibe { get; set; }
        public string? Observacion { get; set; }
        public int? IdAlmacen { get; set; }
        public int? IdUsuario { get; set; }
        public DateTime? Fecha { get; set; }
        public List<ConduceLineaRequest> Detalles { get; set; } = new();
    }

    public class ConduceDetalleDto
    {
        public int IdConduceDetalle { get; set; }
        public int IdFacturaDetalle { get; set; }
        public int IdProducto { get; set; }
        public string ProductoNombre { get; set; } = "";
        public decimal CantidadEntregada { get; set; }
        public decimal CantidadFacturada { get; set; }
        public decimal CantidadPendiente { get; set; }
    }

    public class ConduceDto
    {
        public int IdConduceHeader { get; set; }
        public int IdFacturaHeader { get; set; }
        public string Numero { get; set; } = "";
        public DateTime Fecha { get; set; }
        public string? QuienEntrega { get; set; }
        public string? QuienRecibe { get; set; }
        public string? Observacion { get; set; }
        public int? IdAlmacen { get; set; }
        public string? AlmacenNombre { get; set; }
        public int IdEmpresa { get; set; }
        public string? NumeroFactura { get; set; }
        public string? ClienteNombre { get; set; }
        public string? Ncf { get; set; }
        public List<ConduceDetalleDto> Detalles { get; set; } = new();
    }

    public class FacturaParaConduceDto
    {
        public int IdFacturaHeader { get; set; }
        public string NumeroDocumento { get; set; } = "";
        public string? Ncf { get; set; }
        public DateTime Fecha { get; set; }
        public string? ClienteNombre { get; set; }
        public int? IdCliente { get; set; }
        public decimal Total { get; set; }
        public int LineasPendientes { get; set; }
        public decimal CantidadPendienteTotal { get; set; }
    }

    public class LineaPendienteEntregaDto
    {
        public int IdFacturaDetalle { get; set; }
        public int IdProducto { get; set; }
        public string ProductoNombre { get; set; } = "";
        public decimal CantidadFacturada { get; set; }
        public decimal CantidadDevuelta { get; set; }
        public decimal CantidadEntregada { get; set; }
        public decimal CantidadPendiente { get; set; }
    }

    public class EstadoEntregaFacturaDto
    {
        public int IdFacturaHeader { get; set; }
        public string NumeroDocumento { get; set; } = "";
        public string? Ncf { get; set; }
        public DateTime FechaFactura { get; set; }
        public string? ClienteNombre { get; set; }
        public string? ClienteDocumento { get; set; }
        public List<LineaPendienteEntregaDto> Lineas { get; set; } = new();
        public List<ConduceDto> Conduces { get; set; } = new();
        public decimal TotalFacturado { get; set; }
        public decimal TotalEntregado { get; set; }
        public decimal TotalPendiente { get; set; }
    }
}
