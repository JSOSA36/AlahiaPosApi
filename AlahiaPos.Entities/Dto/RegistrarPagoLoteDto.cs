using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto
{
    public class AplicacionPagoFacturaDto
    {
        public int IdFacturaHeader { get; set; }
        public decimal Monto { get; set; }
    }

    public class RegistrarPagoLoteRequest
    {
        public int IdEmpresa { get; set; }
        public int IdCliente { get; set; }
        public string FormaPago { get; set; } = "";
        public string? Nota { get; set; }
        public List<AplicacionPagoFacturaDto> Aplicaciones { get; set; } = new();
    }

    public class RegistrarPagoLoteResult
    {
        public int FacturasAfectadas { get; set; }
        public decimal MontoTotal { get; set; }
        public string Message { get; set; } = "Pagos registrados correctamente";
    }
}
