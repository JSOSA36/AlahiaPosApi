using System;
using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto
{
    public class FacturaPublicaDto
    {
        public string NumeroDocumento { get; set; } = "";
        public string? Ncf { get; set; }
        public DateTime Fecha { get; set; }
        public string NombreEmpresa { get; set; } = "";
        public string? TelefonoEmpresa { get; set; }
        public string? DireccionEmpresa { get; set; }
        public string? RncEmpresa { get; set; }
        public string ClienteNombre { get; set; } = "";
        public string? ClienteTelefono { get; set; }
        public string? ClienteRnc { get; set; }
        public string? ClienteCorreo { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TotalItbis { get; set; }
        public decimal TotalDescuento { get; set; }
        public decimal Total { get; set; }
        public string? Nota { get; set; }
        public string? Moneda { get; set; }
        public List<CotizacionPublicaLineaDto> Lineas { get; set; } = new();
    }

    public class EnviarFacturaCorreoRequest
    {
        public string? Correo { get; set; }
        public string? PublicBaseUrl { get; set; }
    }
}
