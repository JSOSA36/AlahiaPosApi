using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto.Fiscal
{
    /// <summary>
    /// Modelo genérico que todos los resolvers producen.
    /// El Motor Fiscal solo trabaja con este modelo.
    /// Nunca ve entidades comerciales directamente.
    /// </summary>
    public class DocumentoOrigenInfo
    {
        public OrigenDocumento Origen { get; set; }
        public int IdOrigen { get; set; }
        public int IdEmpresa { get; set; }
        public int? IdSucursal { get; set; }

        public DateTime FechaDocumento { get; set; }
        public string? RncCliente { get; set; }
        public string? NombreCliente { get; set; }
        public string? DireccionCliente { get; set; }
        public string? CorreoCliente { get; set; }
        public string? NumeroDocumentoInterno { get; set; }

        public decimal SubTotal { get; set; }
        public decimal TotalItbis { get; set; }
        public decimal TotalDescuento { get; set; }
        public decimal Total { get; set; }

        public List<DocumentoOrigenLinea> Lineas { get; set; } = new();
        public List<DocumentoOrigenPago> FormasPago { get; set; } = new();

        public string? NcfModificado { get; set; }
        public DateTime? FechaDocumentoModificado { get; set; }
        public int? CodigoModificacion { get; set; }
        public string? RazonModificacion { get; set; }
    }

    public class DocumentoOrigenLinea
    {
        public int NumeroLinea { get; set; }
        public string Descripcion { get; set; } = "";
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal MontoItem { get; set; }
        public decimal? TasaItbis { get; set; }
        public decimal MontoItbis { get; set; }
        public bool EsBien { get; set; } = true;
    }

    public class DocumentoOrigenPago
    {
        public int FormaPagoDgii { get; set; }
        public decimal Monto { get; set; }
    }
}
