using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto
{
    public class CrearNotaCreditoDto
    {
        public int IdFacturaHeader { get; set; }

        public int IdEmpresa { get; set; }

        public int IdUsuario { get; set; }

        public string? Observacion { get; set; }

        public List<CrearNotaCreditoLineaDto> Lineas { get; set; }
            = new();
    }

    public class CrearNotaCreditoLineaDto
    {
        public int IdFacturaDetalle { get; set; }

        public decimal Cantidad { get; set; }
    }
}
