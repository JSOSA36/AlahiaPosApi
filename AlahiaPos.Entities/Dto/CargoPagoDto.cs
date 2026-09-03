using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto
{
    public class CargoPagoCalcularRequest
    {
        public int IdEmpresa { get; set; }
        public decimal BaseCalculo { get; set; }
        public List<string> Metodos { get; set; } = new();
    }

    public class CargoPagoAplicadoDto
    {
        public int? IdCargoPagoRegla { get; set; }
        public string Nombre { get; set; } = "";
        public string Tipo { get; set; } = "";
        public decimal Valor { get; set; }
        public decimal BaseCalculo { get; set; }
        public decimal Monto { get; set; }
        public string? MetodoPago { get; set; }
    }

    public class CargoPagoCalcularResult
    {
        public decimal BaseCalculo { get; set; }
        public decimal MontoCargo { get; set; }
        public decimal TotalConCargo { get; set; }
        public List<CargoPagoAplicadoDto> Cargos { get; set; } = new();
    }
}
