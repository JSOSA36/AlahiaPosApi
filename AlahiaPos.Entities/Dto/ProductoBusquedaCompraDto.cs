namespace AlahiaPos.Entities.Dto
{
    public class ProductoBusquedaCompraDto
    {
        public int IdProducto { get; set; }
        public string? CodigoBarra { get; set; }
        public string? Nombre { get; set; }
        public decimal Cantidad { get; set; }
        public decimal ExistenciaAlmacen { get; set; }
        public decimal PrecioCompra { get; set; }
        public bool ControlarStock { get; set; }
        public bool EsServicio { get; set; }
        public string? TipoComportamiento { get; set; }
        /// <summary>true = el precio/costo del producto incluye ITBIS (desglosar en compras).</summary>
        public bool Itbis { get; set; }
    }

    public class ProductoBusquedaCompraResultDto
    {
        public List<ProductoBusquedaCompraDto> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool HasMore { get; set; }
    }
}
