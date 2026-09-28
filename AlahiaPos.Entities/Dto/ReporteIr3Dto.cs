namespace AlahiaPos.Entities.Dto
{
    public class Ir3CasillaDto
    {
        public int Numero { get; set; }
        public string Codigo { get; set; } = "";
        public string Etiqueta { get; set; } = "";
        public string Seccion { get; set; } = "";
        public int? Cantidad { get; set; }
        public decimal Monto { get; set; }
        /// <summary>AUTO_NOMINA | FORMULA | MANUAL_PENDIENTE</summary>
        public string Origen { get; set; } = "FORMULA";
        public bool EsCalculada { get; set; }
        public bool EsEditableUsuario { get; set; }
        public string? FormulaAplicada { get; set; }
        public List<string> Alertas { get; set; } = new();
    }

    public class Ir3LineaAsalariadoDto
    {
        public int IdNominaProceso { get; set; }
        public string PeriodKey { get; set; } = "";
        public string EstadoNomina { get; set; } = "";
        public int IdEmpleados { get; set; }
        public string? Cedula { get; set; }
        public string? Nombre { get; set; }
        public decimal SalarioBase { get; set; }
        public decimal HorasExtra { get; set; }
        public decimal Comisiones { get; set; }
        public decimal Bonificaciones { get; set; }
        public decimal OtrosIngresos { get; set; }
        public decimal Bruto { get; set; }
        public decimal AfpEmpleado { get; set; }
        public decimal SfsEmpleado { get; set; }
        public decimal BaseImponibleIsr { get; set; }
        public decimal IsrRetenido { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public DateTime? FechaPago { get; set; }
    }

    public class Ir3NominaIncluidaDto
    {
        public int IdNominaProceso { get; set; }
        public string PeriodKey { get; set; } = "";
        public string Estado { get; set; } = "";
        public string Frecuencia { get; set; } = "";
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public DateTime? FechaPago { get; set; }
        public int Empleados { get; set; }
        public decimal TotalBruto { get; set; }
        public decimal TotalIsr { get; set; }
    }

    public class ReporteIr3Dto
    {
        public int IdEmpresa { get; set; }
        public string? RncEmpresa { get; set; }
        public string? RazonSocial { get; set; }
        public string? NombreComercial { get; set; }
        public string? CorreoElectronico { get; set; }
        public string? Telefono { get; set; }
        public string Periodo { get; set; } = "";
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public DateTime? FechaLimitePago { get; set; }
        public string TipoDeclaracion { get; set; } = "Original";
        public string VersionInstructivo { get; set; } = "IR-3";

        public int CantidadEmpleados { get; set; }
        public int CantidadNominas { get; set; }
        public decimal TotalRemuneraciones { get; set; }
        public decimal TotalAfpEmpleado { get; set; }
        public decimal TotalSfsEmpleado { get; set; }
        public decimal TotalBaseImponible { get; set; }
        public decimal TotalIsrRetenido { get; set; }
        public decimal ImpuestoAPagar { get; set; }
        public decimal SaldoAFavor { get; set; }
        public decimal TotalGeneralAPagar { get; set; }
        public int CantidadAlertas { get; set; }
        public List<string> AlertasGlobales { get; set; } = new();

        public List<Ir3CasillaDto> Resumen { get; set; } = new();
        public List<Ir3CasillaDto> Liquidacion { get; set; } = new();
        public List<Ir3NominaIncluidaDto> NominasIncluidas { get; set; } = new();
        public List<Ir3LineaAsalariadoDto> LineasAsalariados { get; set; } = new();
        public string ContenidoCsv { get; set; } = "";
    }
}
