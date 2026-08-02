namespace AlahiaPos.Entities.Dto
{
    /// <summary>
    /// Línea editable del preview de extracto (antes de confirmar).
    /// </summary>
    public class ExtractoPreviewLineaDto
    {
        public int IdTesoreriaExtractoLinea { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public string? Descripcion { get; set; }
        public string? Referencia { get; set; }
        public decimal Debito { get; set; }
        public decimal Credito { get; set; }
        public decimal? Balance { get; set; }
        public string? CategoriaSugerida { get; set; }
    }

    /// <summary>
    /// Resultado de PreviewArchivo / GetPreview / ActualizarLineasPreview.
    /// El extracto queda persistido en estado PREVIEW: sin matching y sin
    /// tocar MovimientoFinanciero hasta que se confirme.
    /// </summary>
    public class ExtractoPreviewDto
    {
        public int IdTesoreriaExtractoImport { get; set; }
        public int IdEmpresa { get; set; }
        public int IdCuentaFinanciera { get; set; }
        public string Estado { get; set; } = "PREVIEW";
        public string NombreArchivo { get; set; } = string.Empty;
        public string Formato { get; set; } = "CSV";

        /// <summary>Adapter de parseo que interpretó el archivo (trazabilidad).</summary>
        public string? AdapterUsado { get; set; }

        public string? Banco { get; set; }
        public string? NumeroCuentaBanco { get; set; }
        public string? Moneda { get; set; }
        public DateTime? PeriodoDesde { get; set; }
        public DateTime? PeriodoHasta { get; set; }
        public decimal? SaldoInicial { get; set; }
        public decimal? SaldoFinal { get; set; }
        public decimal TotalDebitos { get; set; }
        public decimal TotalCreditos { get; set; }
        public int CantidadLineas { get; set; }

        /// <summary>Advertencias no bloqueantes del parseo/validación.</summary>
        public List<string> Warnings { get; set; } = new();

        public List<ExtractoPreviewLineaDto> Lineas { get; set; } = new();
    }

    public class NuevaLineaPreviewDto
    {
        public DateTime FechaMovimiento { get; set; }
        public string? Descripcion { get; set; }
        public string? Referencia { get; set; }
        public decimal Debito { get; set; }
        public decimal Credito { get; set; }
        public decimal? Balance { get; set; }
    }

    public class EditarLineaPreviewDto : NuevaLineaPreviewDto
    {
        public int IdTesoreriaExtractoLinea { get; set; }
    }

    /// <summary>
    /// Edición segura de líneas mientras el extracto está en PREVIEW.
    /// Si <see cref="ReemplazarTodas"/> viene con valor, sustituye el set completo
    /// e ignora Agregar/Editar/EliminarIds.
    /// </summary>
    public class ActualizarLineasPreviewDto
    {
        public int IdUsuario { get; set; }

        /// <summary>Metadatos opcionales editables en el wizard de preview.</summary>
        public string? Banco { get; set; }
        public string? NumeroCuentaBanco { get; set; }
        public string? Moneda { get; set; }
        public DateTime? PeriodoDesde { get; set; }
        public DateTime? PeriodoHasta { get; set; }
        public decimal? SaldoInicial { get; set; }
        public decimal? SaldoFinal { get; set; }
        public string? Observacion { get; set; }

        public List<NuevaLineaPreviewDto>? ReemplazarTodas { get; set; }
        public List<NuevaLineaPreviewDto>? Agregar { get; set; }
        public List<EditarLineaPreviewDto>? Editar { get; set; }
        public List<int>? EliminarIds { get; set; }
    }
}
