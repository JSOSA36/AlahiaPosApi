using System;
using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto
{
    /// <summary>
    /// Vista pública de una cotización POS (IdTipoDocumentos = 2) para compartir con el cliente final.
    /// </summary>
    public class CotizacionPublicaDto
    {
        public string NumeroDocumento { get; set; } = "";
        public DateTime Fecha { get; set; }
        public DateTime FechaValidez { get; set; }
        public string NombreEmpresa { get; set; } = "";
        public string? TelefonoEmpresa { get; set; }
        public string? DireccionEmpresa { get; set; }
        public string? LogoEmpresa { get; set; }
        public string? RncEmpresa { get; set; }
        public string ClienteNombre { get; set; } = "";
        public decimal SubTotal { get; set; }
        public decimal TotalItbis { get; set; }
        public decimal TotalDescuento { get; set; }
        public decimal Total { get; set; }
        public string? Nota { get; set; }
        public string? Moneda { get; set; }
        public List<CotizacionPublicaLineaDto> Lineas { get; set; } = new();
    }

    public class CotizacionPublicaLineaDto
    {
        public decimal Cantidad { get; set; }
        public string Descripcion { get; set; } = "";
        public decimal PrecioUnitario { get; set; }
        public decimal SubTotal { get; set; }
    }

    public class CotizacionPublicaLinkDto
    {
        public string Token { get; set; } = "";
        public int IdFacturaHeader { get; set; }
        public string NumeroDocumento { get; set; } = "";
        /// <summary>URL completa del frontend (puede ser localhost).</summary>
        public string? Url { get; set; }
        /// <summary>URL https corta clickeable en WhatsApp (si se pudo generar).</summary>
        public string? UrlCorta { get; set; }
    }

    public class CotizacionCompartirRequest
    {
        /// <summary>Origen del frontend, ej. https://app.alahiapos.com o http://localhost:8100</summary>
        public string? PublicBaseUrl { get; set; }
    }
}
