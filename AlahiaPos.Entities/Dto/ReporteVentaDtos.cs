namespace AlahiaPos.Entities.Dto
{
    public class ReporteVentaFacturasDto
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public int CantidadFacturas { get; set; }
        public int ConNcf { get; set; }
        public int SinNcf { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TotalDescuento { get; set; }
        public decimal TotalItbis { get; set; }
        public decimal Total { get; set; }
        public List<ReporteVentaFormaPagoGrupoDto> Grupos { get; set; } = new();
        public List<ReporteVentaSucursalGrupoDto> GruposSucursal { get; set; } = new();
        public bool EsConsolidado { get; set; }
    }

    public class ReporteVentaSucursalGrupoDto
    {
        public int IdSucursal { get; set; }
        public string NombreSucursal { get; set; } = "";
        public int CantidadFacturas { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TotalDescuento { get; set; }
        public decimal TotalItbis { get; set; }
        public decimal Total { get; set; }
    }

    public class ReporteVentaFormaPagoGrupoDto
    {
        public string FormaPago { get; set; } = "";
        public int CantidadFacturas { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TotalDescuento { get; set; }
        public decimal TotalItbis { get; set; }
        public decimal Total { get; set; }
        public List<ReporteVentaFacturaDto> Facturas { get; set; } = new();
    }

    public class ReporteVentaFacturaDto
    {
        public int IdFacturaHeader { get; set; }
        public string NumeroDocumento { get; set; } = "";
        public DateTime Fecha { get; set; }
        public string Cliente { get; set; } = "";
        public string Ncf { get; set; } = "";
        public bool TieneNcf { get; set; }
        public bool TieneItbis { get; set; }
        public string TipoFactura { get; set; } = "";
        public string Estado { get; set; } = "";
        public string FormaPago { get; set; } = "";
        public decimal SubTotal { get; set; }
        public decimal TotalDescuento { get; set; }
        public decimal TotalItbis { get; set; }
        public decimal Total { get; set; }
        public int IdSucursal { get; set; }
        public string NombreSucursal { get; set; } = "";
        public List<ReporteVentaPagoDto> Pagos { get; set; } = new();
        public List<ReporteVentaLineaDto> Lineas { get; set; } = new();
    }

    public class ReporteVentaPagoDto
    {
        public string FormaPago { get; set; } = "";
        public decimal Monto { get; set; }
    }

    public class ReporteVentaLineaDto
    {
        public int IdProducto { get; set; }
        public string Codigo { get; set; } = "";
        public string Producto { get; set; } = "";
        public bool EsServicio { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public decimal Itbis { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Total { get; set; }
    }

    public class ReporteVentaProductosDto
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public int CantidadProductos { get; set; }
        public decimal CantidadVendida { get; set; }
        public decimal TotalDescuento { get; set; }
        public decimal TotalItbis { get; set; }
        public decimal Total { get; set; }
        public List<ReporteVentaProductoDto> Productos { get; set; } = new();
    }

    public class ReporteVentaProductoDto
    {
        public int IdProducto { get; set; }
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public bool EsServicio { get; set; }
        public decimal Cantidad { get; set; }
        public int CantidadFacturas { get; set; }
        public decimal PrecioPromedio { get; set; }
        public decimal Descuento { get; set; }
        public decimal Itbis { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Total { get; set; }
    }
}
