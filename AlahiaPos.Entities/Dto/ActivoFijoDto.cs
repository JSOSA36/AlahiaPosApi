namespace AlahiaPos.Entities.Dto
{
    public class ActivoFijoDto
    {
        public int IdActivoFijo { get; set; }
        public int IdEmpresa { get; set; }
        public int? IdProducto { get; set; }
        public string? NombreProducto { get; set; }
        public int? IdOrdenCompraHeader { get; set; }
        public string? NumeroDocumentoCompra { get; set; }
        public int? IdOrdenCompraDetalle { get; set; }
        public string CodigoActivo { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public string? NumeroSerie { get; set; }
        public DateTime FechaAdquisicion { get; set; }
        public DateTime? FechaRecepcion { get; set; }
        public decimal ValorAdquisicion { get; set; }
        public decimal ValorResidual { get; set; }
        public int? VidaUtilMeses { get; set; }
        public string Estado { get; set; } = "";
        public int? IdCuentaContable { get; set; }
        public int? IdAlmacenRecepcion { get; set; }
        public string? NombreAlmacenRecepcion { get; set; }
        public string? Ubicacion { get; set; }
        public string? Responsable { get; set; }
        public string? Observacion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public bool Activo { get; set; }
    }

    public class ActualizarActivoFijoRequest
    {
        public int IdEmpresa { get; set; }
        public string? Descripcion { get; set; }
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public string? NumeroSerie { get; set; }
        public decimal? ValorResidual { get; set; }
        public int? VidaUtilMeses { get; set; }
        public string? Estado { get; set; }
        public string? Ubicacion { get; set; }
        public string? Responsable { get; set; }
        public string? Observacion { get; set; }
        public int? IdCuentaContable { get; set; }
    }

    public class ResumenActivosFijosDto
    {
        public int IdEmpresa { get; set; }
        public int CantidadActivos { get; set; }
        public int CantidadPendienteDatos { get; set; }
        public int CantidadDadosDeBaja { get; set; }
        /// <summary>Suma ValorAdquisicion de activos no dados de baja (Activo=true).</summary>
        public decimal ValorActivosFijos { get; set; }
        /// <summary>Suma ValorAdquisicion con Estado=ACTIVO.</summary>
        public decimal ValorActivosOperativos { get; set; }
    }
}
