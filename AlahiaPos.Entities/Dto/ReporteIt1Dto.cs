namespace AlahiaPos.Entities.Dto
{
    /// <summary>Casilla de Anexo A o IT-1 (liquidación mensual DGII IT-1 2020).</summary>
    public class It1CasillaDto
    {
        public int Numero { get; set; }
        public string Codigo { get; set; } = "";
        public string Etiqueta { get; set; } = "";
        public string Seccion { get; set; } = "";
        public int? Cantidad { get; set; }
        public decimal Monto { get; set; }
        /// <summary>Anexo A IX — compras locales.</summary>
        public decimal? MontoLocal { get; set; }
        public decimal? MontoServicios { get; set; }
        public decimal? MontoImportaciones { get; set; }
        /// <summary>AUTO_607 | AUTO_606 | FORMULA | MANUAL_PENDIENTE | NO_APLICA | USUARIO</summary>
        public string Origen { get; set; } = "FORMULA";
        public bool EsCalculada { get; set; }
        public bool EsEditableUsuario { get; set; }
        public string? FormulaAplicada { get; set; }
        public List<string> Alertas { get; set; } = new();
    }

    public class ReporteIt1Dto
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
        public string VersionInstructivo { get; set; } = "IT-1-2020";

        public decimal TotalOperacionesPeriodo { get; set; }
        public decimal TotalItbisCobrado { get; set; }
        public decimal TotalItbisDeducible { get; set; }
        public decimal ImpuestoAPagar { get; set; }
        public decimal SaldoAFavor { get; set; }
        public decimal TotalGeneralAPagar { get; set; }
        public int CantidadAlertas { get; set; }
        public List<string> AlertasGlobales { get; set; } = new();

        public List<It1CasillaDto> AnexoA { get; set; } = new();
        public List<It1CasillaDto> It1 { get; set; } = new();

        /// <summary>CSV de apoyo (Anexo A + IT-1) para Excel.</summary>
        public string ContenidoCsv { get; set; } = "";
    }
}
