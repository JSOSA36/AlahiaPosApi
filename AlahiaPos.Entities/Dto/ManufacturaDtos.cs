namespace AlahiaPos.Entities.Dto
{
    public class RecetaItemDto
    {
        public int IdRecetaItem { get; set; }
        public int IdProducto { get; set; }
        public string? NombreProducto { get; set; }
        public decimal Cantidad { get; set; }
        public int? IdUnidadMedida { get; set; }
        public string? Unidad { get; set; }
        public int Orden { get; set; }
    }

    public class RecetaDto
    {
        public int IdReceta { get; set; }
        public int IdEmpresa { get; set; }
        public int IdProductoTerminado { get; set; }
        public string? NombreProducto { get; set; }
        public string Nombre { get; set; } = "";
        public decimal RendimientoBase { get; set; }
        public int? IdUnidadMedida { get; set; }
        public string? Unidad { get; set; }
        public bool Activa { get; set; }
        public string? Observacion { get; set; }
        public List<RecetaItemDto> Items { get; set; } = new();
    }

    public class GuardarRecetaRequest
    {
        public int IdReceta { get; set; }
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public int IdProductoTerminado { get; set; }
        public string Nombre { get; set; } = "";
        public decimal RendimientoBase { get; set; } = 1;
        public int? IdUnidadMedida { get; set; }
        public bool Activa { get; set; } = true;
        public string? Observacion { get; set; }
        public List<RecetaItemDto> Items { get; set; } = new();
    }

    public class ExplosionMaterialDto
    {
        public int IdProducto { get; set; }
        public string? NombreProducto { get; set; }
        public int? IdUnidadMedida { get; set; }
        public string? Unidad { get; set; }
        public decimal CantidadTeorica { get; set; }
        public decimal? CantidadReal { get; set; }
        public decimal Disponible { get; set; }
        public decimal Faltante { get; set; }
        public decimal PrecioCompra { get; set; }
        public decimal CostoLinea { get; set; }
        public int? IdProveedor { get; set; }
        public string? NombreProveedor { get; set; }
    }

    public class ExplosionDto
    {
        public int IdReceta { get; set; }
        public int IdProductoTerminado { get; set; }
        public string? NombreProducto { get; set; }
        public decimal RendimientoBase { get; set; }
        public decimal Cantidad { get; set; }
        public decimal Factor { get; set; }
        public int? IdAlmacenOrigen { get; set; }
        public bool HayFaltantes { get; set; }
        public List<ExplosionMaterialDto> Materiales { get; set; } = new();
    }

    public class OrdenProduccionDto
    {
        public int IdOrdenProduccion { get; set; }
        public int IdEmpresa { get; set; }
        public string Numero { get; set; } = "";
        public int IdReceta { get; set; }
        public string? NombreReceta { get; set; }
        public int IdProductoTerminado { get; set; }
        public string? NombreProducto { get; set; }
        public decimal CantidadPlanificada { get; set; }
        public decimal? CantidadReal { get; set; }
        public int IdAlmacenOrigen { get; set; }
        public string? NombreAlmacenOrigen { get; set; }
        public int IdAlmacenDestino { get; set; }
        public string? NombreAlmacenDestino { get; set; }
        public DateTime Fecha { get; set; }
        public int? IdUsuarioResponsable { get; set; }
        public string? Observacion { get; set; }
        public string Estado { get; set; } = "BORRADOR";
        public int? IdMovimientoSalida { get; set; }
        public int? IdMovimientoEntrada { get; set; }
        public decimal CostoMateriales { get; set; }
        public decimal CostoUnitario { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaCompletado { get; set; }
        public bool HayFaltantes { get; set; }
        public List<ExplosionMaterialDto> Materiales { get; set; } = new();
    }

    public class GuardarOrdenProduccionRequest
    {
        public int IdOrdenProduccion { get; set; }
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public int IdReceta { get; set; }
        public decimal CantidadPlanificada { get; set; }
        public int IdAlmacenOrigen { get; set; }
        public int IdAlmacenDestino { get; set; }
        public DateTime? Fecha { get; set; }
        public int? IdUsuarioResponsable { get; set; }
        public string? Observacion { get; set; }
    }

    public class CompletarOrdenProduccionRequest
    {
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public decimal CantidadReal { get; set; }
        public List<ConsumoRealDto> Consumos { get; set; } = new();
    }

    public class ConsumoRealDto
    {
        public int IdProducto { get; set; }
        public decimal CantidadReal { get; set; }
    }

    public class RequerimientoCompraResultadoDto
    {
        public List<int> IdOrdenesCompra { get; set; } = new();
        public string Mensaje { get; set; } = "";
    }
}
