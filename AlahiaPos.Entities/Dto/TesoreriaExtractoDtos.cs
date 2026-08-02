using System.Text.Json.Serialization;

namespace AlahiaPos.Entities.Dto
{
    public class ImportarExtractoDto
    {
        public int IdEmpresa { get; set; }
        public int IdCuentaFinanciera { get; set; }
        public int IdUsuario { get; set; }
        public string NombreArchivo { get; set; } = "extracto.csv";
        public string ContenidoCsv { get; set; } = string.Empty;
        public byte[]? ContenidoArchivo { get; set; }
        public string? TextoExtraido { get; set; }
        public string? Formato { get; set; }
        public string? HashArchivo { get; set; }
        public int ToleranciaDiasMatch { get; set; } = 3;
        public int? IdTesoreriaConciliacion { get; set; }
    }

    public class ConfirmarExtractoMatchDto
    {
        public int IdTesoreriaExtractoLinea { get; set; }
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public int? IdMovimientoFinanciero { get; set; }
    }

    public class CrearMovimientoDesdeExtractoDto
    {
        public int IdTesoreriaExtractoLinea { get; set; }
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public string? Motivo { get; set; }
    }

    public class ExtractoLineaMatchDto
    {
        public int IdTesoreriaExtractoLinea { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public string? Descripcion { get; set; }
        public string? Referencia { get; set; }
        public decimal Debito { get; set; }
        public decimal Credito { get; set; }
        public decimal? Balance { get; set; }
        public decimal MontoNeto { get; set; }
        public string EstadoMatch { get; set; } = string.Empty;
        public int? IdMovimientoFinanciero { get; set; }
        public decimal? ScoreSugerido { get; set; }
        public string? CategoriaSugerida { get; set; }
        public string? AccionTomada { get; set; }
        public bool EsAutoConciliado { get; set; }
        public string? AccionRecomendada { get; set; }
        public MovimientoFinancieroListadoDto? MovimientoSugerido { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AccionExtractoPendiente
    {
        CREAR_GASTO,
        CREAR_INGRESO,
        CREAR_AJUSTE,
        ASOCIAR,
        IGNORAR,
        RECLASIFICAR_PAGO
    }

    public class ResolverExtractoLineaDto
    {
        public int IdTesoreriaExtractoLinea { get; set; }
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public AccionExtractoPendiente Accion { get; set; }
        public int? IdMovimientoFinanciero { get; set; }
        public string? Categoria { get; set; }
        public string? Motivo { get; set; }
    }

    public class ResolverExtractoLineaResultadoDto
    {
        public int IdTesoreriaExtractoLinea { get; set; }
        public AccionExtractoPendiente Accion { get; set; }
        public int? IdMovimientoFinanciero { get; set; }
        public string? Categoria { get; set; }
        // Las acciones CREAR_* crean el MovimientoFinanciero canónico. No se crean
        // Gastos/Ingresos incompletos porque esas entidades exigen documentos ajenos al extracto.
        public string EntidadCreada { get; set; } = "NINGUNA";
        public bool YaResuelta { get; set; }
    }

    public class ExtractoResumenDto
    {
        public int IdTesoreriaExtractoImport { get; set; }
        public decimal SaldoInicialEstado { get; set; }
        public decimal SaldoFinalEstado { get; set; }
        public decimal TotalDebitos { get; set; }
        public decimal TotalCreditos { get; set; }
        public int CantidadConciliada { get; set; }
        public decimal MontoConciliado { get; set; }
        public int CantidadPendiente { get; set; }
        public decimal MontoPendiente { get; set; }
        public int Gastos { get; set; }
        public int Ingresos { get; set; }
        public int Ajustes { get; set; }
        public int Ignorados { get; set; }
        public decimal SaldoLibrosFinalConciliado { get; set; }
        public decimal Diferencia { get; set; }
        public bool PuedeCerrar { get; set; }
    }
}
