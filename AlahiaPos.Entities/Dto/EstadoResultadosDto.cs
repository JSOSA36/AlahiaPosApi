namespace AlahiaPos.Entities.Dto
{
    public class EstadoResultadosSeccionDto
    {
        public string Titulo { get; set; } = string.Empty;
        public List<EstadoResultadosLineaDto> Lineas { get; set; } = new();
        public decimal Total { get; set; }
    }

    public class EstadoResultadosLineaDto
    {
        public string CodigoCuenta { get; set; } = string.Empty;
        public string NombreCuenta { get; set; } = string.Empty;
        public decimal Monto { get; set; }
    }

    public class EstadoResultadosDto
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public EstadoResultadosSeccionDto Ingresos { get; set; } = new() { Titulo = "Ingresos" };
        public EstadoResultadosSeccionDto Costos { get; set; } = new() { Titulo = "Costos" };
        public decimal UtilidadBruta { get; set; }
        public EstadoResultadosSeccionDto Gastos { get; set; } = new() { Titulo = "Gastos" };
        public decimal UtilidadNeta { get; set; }
    }
}
