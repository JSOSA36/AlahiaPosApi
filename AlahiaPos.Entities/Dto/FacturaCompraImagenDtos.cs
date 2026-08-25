namespace AlahiaPos.Entities.Dto
{
    public class FacturaCompraInterpretarRequest
    {
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        /// <summary>Texto extraído de un PDF digital (e-CF, factura generada).</summary>
        public string? TextoDocumento { get; set; }
        public List<FacturaCompraInterpretarPagina> Paginas { get; set; } = new();
    }

    public class FacturaCompraInterpretarPagina
    {
        public byte[] Bytes { get; set; } = Array.Empty<byte>();
        public string Mime { get; set; } = "image/jpeg";
    }

    public class FacturaCompraImagenLineaDto
    {
        public string Descripcion { get; set; } = string.Empty;
        public string? Codigo { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Itbis { get; set; }
        public decimal Importe { get; set; }
        public int? IdProducto { get; set; }
        public string? NombreProducto { get; set; }
        public string? TipoComportamiento { get; set; }
        public bool Emparejado { get; set; }
        public string? MotivoEmparejado { get; set; }
    }

    public class FacturaCompraImagenResultadoDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? RncEmisor { get; set; }
        public string? NombreEmisor { get; set; }
        public int? IdProveedor { get; set; }
        public string? ProveedorNombre { get; set; }
        public bool ProveedorEncontrado { get; set; }
        public string? Ncf { get; set; }
        public DateTime? Fecha { get; set; }
        public string CondicionPago { get; set; } = "Contado";
        public DateTime? FechaVencimiento { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Itbis { get; set; }
        public decimal Total { get; set; }
        public bool PreciosIncluyenItbis { get; set; }
        public List<FacturaCompraImagenLineaDto> Lineas { get; set; } = new();
        public int LineasEmparejadas { get; set; }
        public int LineasSinProducto { get; set; }
        public string Provider { get; set; } = string.Empty;
    }
}
