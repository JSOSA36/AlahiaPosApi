using System;

namespace AlahiaPos.Entities.Dto
{
    public class DocumentoClinicoDto
    {
        public int IdDocumentoClinico { get; set; }

        public int IdEmpresa { get; set; }

        public int IdCliente { get; set; }

        public int IdPlantilla { get; set; }

        public string TipoDocumento { get; set; } = string.Empty;

        public string NumeroDocumento { get; set; } = string.Empty;

        public DateTime FechaEmision { get; set; }

        public string? NombreDoctor { get; set; }

        public int? HorasReposo { get; set; }

        public string? Procedimiento { get; set; }

        public string? Observaciones { get; set; }

        public string ContenidoHTMLFinal { get; set; } = string.Empty;

        public string? DatosJSON { get; set; }

        public int IdUsuarioCreacion { get; set; }

        public string Estado { get; set; } = "EMITIDO";

        public DateTime? FechaCreacion { get; set; }

        public string? NombreCliente { get; set; }
    }
}
