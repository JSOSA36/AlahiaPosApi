namespace AlahiaPos.Entities.Dto
{
    /// <summary>Casilla del IR-17 2026 (Julio 2026 en adelante) o del anexo R9C.</summary>
    public class Ir17CasillaDto
    {
        public int Numero { get; set; }
        public string Codigo { get; set; } = "";
        public string Etiqueta { get; set; } = "";
        public string Seccion { get; set; } = "";
        public decimal? Tasa { get; set; }
        public int? Cantidad { get; set; }
        public decimal MontoImponible { get; set; }
        public decimal Impuesto { get; set; }
        /// <summary>AUTO_606 | FORMULA | MANUAL_PENDIENTE | NO_APLICA | R9C</summary>
        public string Origen { get; set; } = "FORMULA";
        public bool EsCalculada { get; set; }
        public bool EsEditableUsuario { get; set; }
        public string? FormulaAplicada { get; set; }
        public int? TipoRetencionIsr606 { get; set; }
        public List<string> Alertas { get; set; } = new();
    }

    public class Ir17LineaOrigenDto
    {
        public int IdOrdenCompraHeader { get; set; }
        public string? Ncf { get; set; }
        public string? NumeroDocumento { get; set; }
        public int? TipoRetencionIsr { get; set; }
        public int Casilla { get; set; }
        public decimal MontoImponible { get; set; }
        public decimal Impuesto { get; set; }
        public DateTime? FechaPagoFiscal { get; set; }
    }

    public class ReporteIr17Dto
    {
        public int IdEmpresa { get; set; }
        public string? RncEmpresa { get; set; }
        public string? RazonSocial { get; set; }
        public string? NombreComercial { get; set; }
        public string? CorreoElectronico { get; set; }
        public string? Telefono { get; set; }
        /// <summary>Periodo AAAAMM.</summary>
        public string Periodo { get; set; } = "";
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public DateTime? FechaLimitePago { get; set; }
        public string TipoDeclaracion { get; set; } = "Original";
        public string VersionInstructivo { get; set; } = "IR-17-2026";

        public int CantidadRetenciones { get; set; }
        public decimal TotalMontoImponible { get; set; }
        public decimal TotalOtrasRetenciones { get; set; }
        public decimal ImpuestoAPagar { get; set; }
        public decimal SaldoAFavor { get; set; }
        public decimal TotalGeneralAPagar { get; set; }
        public int CantidadAlertas { get; set; }
        public List<string> AlertasGlobales { get; set; } = new();

        public List<Ir17CasillaDto> OtrasRetenciones { get; set; } = new();
        public List<Ir17CasillaDto> Liquidacion { get; set; } = new();
        public List<Ir17LineaOrigenDto> LineasOrigen { get; set; } = new();

        /// <summary>CSV de apoyo para cargar en Oficina Virtual / Excel.</summary>
        public string ContenidoCsv { get; set; } = "";
    }
}
