namespace AlahiaPos.Entities.Dto
{
    public class FacturaCompraDetalleDto
    {
        public int IdOrdenCompraDetalle { get; set; }
        public int IdProducto { get; set; }
        public decimal Cantidad { get; set; }
        public decimal CantidadRecibida { get; set; }
        public decimal CantidadPendienteRecepcion { get; set; }
        public decimal PrecioCompra { get; set; }
        public decimal Descuento { get; set; }
        public decimal Itbis { get; set; }
        public decimal SubTotal { get; set; }
        public string? NombreProducto { get; set; }
        public string? TipoComportamientoLinea { get; set; }
        public bool RequiereRecepcionFisica { get; set; }
    }

    public class FacturaCompraDto
    {
        public int IdOrdenCompraHeader { get; set; }
        public int IdEmpresa { get; set; }
        public int IdProveedor { get; set; }
        public string? ProveedorNombre { get; set; }
        public string? NumeroDocumento { get; set; }
        public string? NumeroComprobanteProveedor { get; set; }
        public string? NcfModificado { get; set; }
        public DateTime FechaDocumento { get; set; }
        public string CondicionFactura { get; set; } = "Contado";
        public DateTime? FechaVencimiento { get; set; }
        public int? IdAlmacen { get; set; }
        public string? Comentario { get; set; }
        public int? IdTipoBienesServicios { get; set; }
        public int? FormaPagoDgii { get; set; }
        public decimal MontoFacturadoServicios { get; set; }
        public decimal MontoFacturadoBienes { get; set; }
        public decimal ItbisRetenido { get; set; }
        public decimal ItbisProporcionalidad { get; set; }
        public decimal ItbisLlevadoAlCosto { get; set; }
        public int? TipoRetencionIsr { get; set; }
        public decimal MontoRetencionRenta { get; set; }
        public DateTime? FechaPagoFiscal { get; set; }
        /// <summary>Destino ITBIS 1–7 (Anexo A 45–53). Null = pendiente.</summary>
        public byte? DestinoItbis { get; set; }
        public byte? DestinoItbisSugerido { get; set; }
        public bool ClasificacionConfirmada { get; set; }
        public decimal ItbisComprasLocales { get; set; }
        public decimal ItbisServicios { get; set; }
        public decimal ItbisImportaciones { get; set; }
        public string? CodigoNormaRetencionItbis { get; set; }
        public decimal BaseRetencionItbis { get; set; }
        public decimal TotalDescuento { get; set; }
        public decimal TotalItbis { get; set; }
        public decimal Total { get; set; }
        public decimal Pagado { get; set; }
        public decimal Pendiente { get; set; }
        public string Estado { get; set; } = "BORRADOR";
        public string EstadoRecepcion { get; set; } = EstadoRecepcionCompraConstantes.NoAplica;
        public DateTime? FechaUltimaRecepcion { get; set; }
        public bool AjustadaInventario { get; set; }
        public int? IdTipoDocumentos { get; set; }
        public int? IdDocumentoOrigen { get; set; }
        public string? NumeroDocumentoOrigen { get; set; }
        public DateTime? FechaEnvioProveedor { get; set; }
        public List<FacturaCompraDetalleDto> Detalles { get; set; } = new();
    }

    public class GuardarFacturaCompraRequest
    {
        public int IdOrdenCompraHeader { get; set; }
        public int IdEmpresa { get; set; }
        public int IdProveedor { get; set; }
        public string? NumeroComprobanteProveedor { get; set; }
        public string? NcfModificado { get; set; }
        public DateTime FechaDocumento { get; set; }
        public string CondicionFactura { get; set; } = "Contado";
        public DateTime? FechaVencimiento { get; set; }
        public int? IdAlmacen { get; set; }
        public string? Comentario { get; set; }
        public int? IdTipoBienesServicios { get; set; }
        public int? FormaPagoDgii { get; set; }
        public decimal? MontoFacturadoServicios { get; set; }
        public decimal? MontoFacturadoBienes { get; set; }
        public decimal ItbisRetenido { get; set; }
        public decimal ItbisProporcionalidad { get; set; }
        public decimal ItbisLlevadoAlCosto { get; set; }
        public int? TipoRetencionIsr { get; set; }
        public decimal MontoRetencionRenta { get; set; }
        public DateTime? FechaPagoFiscal { get; set; }
        public byte? DestinoItbis { get; set; }
        public bool ClasificacionConfirmada { get; set; }
        public decimal? ItbisComprasLocales { get; set; }
        public decimal? ItbisServicios { get; set; }
        public decimal? ItbisImportaciones { get; set; }
        public string? CodigoNormaRetencionItbis { get; set; }
        public decimal BaseRetencionItbis { get; set; }
        public List<GuardarFacturaCompraDetalleRequest> Detalles { get; set; } = new();
    }

    public class GuardarFacturaCompraDetalleRequest
    {
        public int IdProducto { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioCompra { get; set; }
        public decimal Descuento { get; set; }
        public decimal Itbis { get; set; }
    }

    public class ConfirmarFacturaCompraRequest
    {
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public string? FormaPago { get; set; }
    }

    public class RegistrarPagoProveedorRequest
    {
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public decimal Monto { get; set; }
        public string FormaPago { get; set; } = "EFECTIVO";
        public string? Nota { get; set; }
        public int? IdCuentaFinanciera { get; set; }
    }

    public class BuscarRecepcionCompraRequest
    {
        public int IdEmpresa { get; set; }
        public string? Texto { get; set; }
        public int? IdProveedor { get; set; }
        public bool SoloPendientes { get; set; } = true;
    }

    public class RecepcionCompraLineaRequest
    {
        public int IdOrdenCompraDetalle { get; set; }
        public decimal CantidadRecibir { get; set; }
    }

    public class ConfirmarRecepcionCompraRequest
    {
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public int IdAlmacen { get; set; }
        public string? Observacion { get; set; }
        public List<RecepcionCompraLineaRequest> Lineas { get; set; } = new();
    }

    public class EmitirOrdenCompraRequest
    {
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
    }

    public class EnviarOrdenCompraRequest
    {
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public string? EmailDestino { get; set; }
        public string? Mensaje { get; set; }
    }

    /// <summary>Movimiento del auxiliar Estado de Cuenta proveedor.</summary>
    public class EstadoCuentaMovimientoDto
    {
        public DateTime Fecha { get; set; }
        public string Tipo { get; set; } = ""; // FACTURA | PAGO | SALDO_INICIAL
        public int? IdDocumento { get; set; }
        public string? NumeroDocumento { get; set; }
        public string Concepto { get; set; } = "";
        public decimal Debito { get; set; }
        public decimal Credito { get; set; }
        public decimal Balance { get; set; }
        public string? FormaPago { get; set; }
        public int? IdOrdenCompraHeader { get; set; }
    }

    public class EstadoCuentaFacturaPendienteDto
    {
        public int IdOrdenCompraHeader { get; set; }
        public DateTime Fecha { get; set; }
        public string? NumeroDocumento { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public decimal MontoOriginal { get; set; }
        public decimal Pagado { get; set; }
        public decimal Pendiente { get; set; }
        /// <summary>Días vencidos (&gt;0) o días por vencer (&lt;0). Null si no hay vencimiento crédito.</summary>
        public int? DiasVencimiento { get; set; }
        public string Estado { get; set; } = "";
    }

    public class EstadoCuentaProveedorDto
    {
        public int IdEmpresa { get; set; }
        public int IdProveedor { get; set; }
        public string? ProveedorNombre { get; set; }
        public string? ProveedorRnc { get; set; }
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public decimal SaldoInicial { get; set; }
        public decimal TotalComprado { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal BalancePendiente { get; set; }
        public List<EstadoCuentaMovimientoDto> Movimientos { get; set; } = new();
        public List<EstadoCuentaFacturaPendienteDto> FacturasPendientes { get; set; } = new();
    }

    public class AnalisisCompraFiltroRequest
    {
        public int IdEmpresa { get; set; }
        public int? IdProducto { get; set; }
        public int? IdProveedor { get; set; }
        public int? IdAlmacen { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
    }

    public class AnalisisCompraLineaDto
    {
        public DateTime Fecha { get; set; }
        public int IdProveedor { get; set; }
        public string? ProveedorNombre { get; set; }
        public int IdOrdenCompraHeader { get; set; }
        public string? NumeroDocumento { get; set; }
        public int IdProducto { get; set; }
        public string? ProductoNombre { get; set; }
        public int? IdAlmacen { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Itbis { get; set; }
        public decimal Descuento { get; set; }
        public decimal Total { get; set; }
    }

    public class AnalisisCompraProveedorResumenDto
    {
        public int IdProveedor { get; set; }
        public string? ProveedorNombre { get; set; }
        public decimal UltimoPrecio { get; set; }
        public decimal PrecioPromedio { get; set; }
        public decimal MejorPrecio { get; set; }
        public decimal PeorPrecio { get; set; }
        public int VecesComprado { get; set; }
        public decimal CantidadTotal { get; set; }
        public DateTime? UltimaFecha { get; set; }
    }

    public class AnalisisCompraIndicadoresDto
    {
        public decimal? UltimoPrecio { get; set; }
        public string? UltimoProveedor { get; set; }
        public DateTime? UltimaFecha { get; set; }
        public decimal? PrecioPromedio { get; set; }
        public decimal? MejorPrecio { get; set; }
        public decimal? PeorPrecio { get; set; }
        public int VecesComprado { get; set; }
        public decimal CantidadTotal { get; set; }
        public decimal MayorCantidad { get; set; }
        public string? ProveedorMasBarato { get; set; }
        public string? ProveedorMasFrecuente { get; set; }
    }

    public class AnalisisCompraEvolucionDto
    {
        public DateTime Fecha { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int IdProveedor { get; set; }
        public string? ProveedorNombre { get; set; }
        public string? NumeroDocumento { get; set; }
    }

    public class AnalisisProductoProveedorDto
    {
        public int IdEmpresa { get; set; }
        public int? IdProducto { get; set; }
        public string? ProductoNombre { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public AnalisisCompraIndicadoresDto Indicadores { get; set; } = new();
        public List<AnalisisCompraProveedorResumenDto> ResumenProveedores { get; set; } = new();
        public List<AnalisisCompraLineaDto> Historial { get; set; } = new();
        public List<AnalisisCompraEvolucionDto> EvolucionPrecio { get; set; } = new();
    }

    public class ProveedorDto
    {
        public int IdProveedor { get; set; }
        public int IdEmpresa { get; set; }
        public string? RNC { get; set; }
        public string? NombreComercial { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public string? Email { get; set; }
        public string? Nota { get; set; }
        public bool IsActivo { get; set; } = true;
    }
}
