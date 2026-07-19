namespace AlahiaPos.Entities.Dto
{
    /// <summary>Flags fiscales evaluados de forma centralizada (nunca dispersar if FiscalActivo).</summary>
    public sealed class FiscalFeatureFlags
    {
        public int IdEmpresa { get; init; }
        public bool TieneConfiguracion { get; init; }
        public bool FiscalActivo { get; init; }
        public bool Generar606 { get; init; }
        public bool Generar607 { get; init; }
        public bool GenerarIt1 { get; init; }
        public bool FacturacionElectronicaActiva { get; init; }

        public static FiscalFeatureFlags Apagado(int idEmpresa) => new()
        {
            IdEmpresa = idEmpresa,
            TieneConfiguracion = false,
            FiscalActivo = false,
            Generar606 = false,
            Generar607 = false,
            GenerarIt1 = false,
            FacturacionElectronicaActiva = false
        };

        public bool It1Activo => FiscalActivo && GenerarIt1;
        public bool FotoVentaRelevante => FiscalActivo && (Generar607 || GenerarIt1);
        public bool FotoCompraRelevante => FiscalActivo && (Generar606 || GenerarIt1);
    }

    public sealed class FiscalDocumentoRequest
    {
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public int ReferenciaId { get; set; }
        public string TipoDocumento { get; set; } = "Venta"; // Venta | NotaCredito | Compra
        /// <summary>True en reproceso explícito (ERROR_FISCAL / PENDIENTE_GENERAR).</summary>
        public bool ForceReprocess { get; set; }
        public string? Motivo { get; set; }
    }

    public sealed class DgiiConfiguracionEmpresaDto
    {
        public int IdEmpresa { get; set; }
        public bool FiscalActivo { get; set; }
        public bool Generar606 { get; set; }
        public bool Generar607 { get; set; }
        public bool GenerarIt1 { get; set; }
        public bool FacturacionElectronicaActiva { get; set; }
        public string RegimenTributarioCodigo { get; set; } = "ORDINARIO";
        public string VersionInstructivoPreferida { get; set; } = "IT-1-2020";
        public bool Activo { get; set; } = true;
        public string? RazonSocial { get; set; }
        public string? DeclaranteNombre { get; set; }
        public string? DeclaranteCalidad { get; set; }
        public bool EsConstructor { get; set; }
        public bool EsComisionista { get; set; }
        public bool ObligadoLibroVentasSF { get; set; }
        /// <summary>Motivo opcional para auditoría en PUT.</summary>
        public string? MotivoCambio { get; set; }
    }

    public sealed class FiscalReconciliacionItemDto
    {
        public string TipoDocumento { get; set; } = string.Empty;
        public int ReferenciaId { get; set; }
        public int IdEmpresa { get; set; }
        public string EstadoFiscalDocumento { get; set; } = string.Empty;
        public DateTime? Fecha { get; set; }
        public string? NumeroDocumento { get; set; }
    }
}
