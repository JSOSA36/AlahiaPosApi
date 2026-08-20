using Microsoft.AspNetCore.Http;
using System;

namespace AlahiaPos.Entities.Dto
{
    public class CrearPagoDto
    {
        public int IdEmpresa { get; set; }
        /// <summary>Monto oficial del ciclo (USD). Si viene 0, el servicio lo calcula.</summary>
        public decimal Monto { get; set; }
        public IFormFile? Imagen { get; set; }
        public string? ArchivoUrl { get; set; }
        public DateTime? FechaPago { get; set; }
        public string? Banco { get; set; }
        public string? Referencia { get; set; }
        public int? IdUsuarioReporta { get; set; }

        /// <summary>Bytes del voucher (controller); la IA lee el monto de aquí.</summary>
        public byte[]? ImagenBytes { get; set; }
        public string? ImagenContentType { get; set; }
        public string? ImagenFileName { get; set; }
    }
}
