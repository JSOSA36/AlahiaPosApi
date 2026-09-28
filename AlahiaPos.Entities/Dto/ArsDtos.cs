using System;
using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto
{
    public class ArsAseguradoraDto
    {
        public int IdArs { get; set; }
        public int IdEmpresa { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? RNC { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public string? Email { get; set; }
        public string? Contacto { get; set; }
        public string? Observaciones { get; set; }
        public bool Activo { get; set; } = true;
    }

    public class ArsCuentaPorCobrarDto
    {
        public int IdArs { get; set; }
        public string NombreArs { get; set; } = string.Empty;
        public string? RNC { get; set; }
        public string? Telefono { get; set; }
        public decimal TotalOriginal { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal TotalPendiente { get; set; }
        public int Documentos { get; set; }
    }

    public class ArsDocumentoCxCDto
    {
        public int IdFacturaHeader { get; set; }
        public int IdArs { get; set; }
        public string NombreArs { get; set; } = string.Empty;
        public string? NumeroDocumento { get; set; }
        public string? NCF { get; set; }
        public DateTime Fecha { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public int? IdCliente { get; set; }
        public string? NombreCliente { get; set; }
        public decimal TotalFactura { get; set; }
        public decimal MontoCubiertoArs { get; set; }
        public decimal PagadoArs { get; set; }
        public decimal PendienteArs { get; set; }
        public string EstadoArs { get; set; } = string.Empty;
        public int IdEmpresa { get; set; }
    }

    public class ArsPagoRequest
    {
        public int IdEmpresa { get; set; }
        public int IdFacturaHeader { get; set; }
        public decimal Monto { get; set; }
        public string? FormaPago { get; set; }
        public string? Nota { get; set; }
        public int? IdUsuario { get; set; }
    }

    public class ArsResumenFiltroRequest
    {
        public int IdEmpresa { get; set; }
        public int IdArs { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public string? Estado { get; set; }
        public int IdSucursal { get; set; }
    }

    public class ArsAntiguedadBucketDto
    {
        public int IdArs { get; set; }
        public string NombreArs { get; set; } = string.Empty;
        public decimal Dias0A30 { get; set; }
        public decimal Dias31A60 { get; set; }
        public decimal Dias61A90 { get; set; }
        public decimal DiasMas90 { get; set; }
        public decimal TotalPendiente { get; set; }
    }

    public class ArsVentasResumenDto
    {
        public int IdArs { get; set; }
        public string NombreArs { get; set; } = string.Empty;
        public int Documentos { get; set; }
        public decimal TotalVentas { get; set; }
        public decimal CoberturaArs { get; set; }
        /// <summary>Lo que pagó el cliente (IDCliente) de esa venta, no un paciente aparte.</summary>
        public decimal PagadoPaciente { get; set; }
        public decimal PagadoArs { get; set; }
        public decimal PendienteArs { get; set; }
    }

    public class ArsPagoLoteRequest
    {
        public int IdEmpresa { get; set; }
        public List<int> IdFacturaHeaders { get; set; } = new();
        public decimal Monto { get; set; }
        public string? FormaPago { get; set; }
        public string? Nota { get; set; }
        public int? IdUsuario { get; set; }
    }

    public class ArsPagoLoteResultadoDto
    {
        public int Documentos { get; set; }
        public decimal TotalAplicado { get; set; }
        public string NombreArs { get; set; } = string.Empty;
    }

    public class ArsPagoHistorialDto
    {
        public int IdPago { get; set; }
        public int IdFacturaHeader { get; set; }
        public int? IdArs { get; set; }
        public string? NumeroDocumento { get; set; }
        public string? FormaPago { get; set; }
        public decimal Monto { get; set; }
        public string? Nota { get; set; }
        public bool EsCoberturaArs { get; set; }
        public DateTime Fecha { get; set; }
    }

    public class ArsDesgloseCajaDto
    {
        public int IdArs { get; set; }
        public string NombreArs { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }
}
