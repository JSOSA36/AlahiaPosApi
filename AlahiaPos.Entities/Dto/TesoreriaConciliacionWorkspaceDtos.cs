namespace AlahiaPos.Entities.Dto
{
    public class ConciliacionWorkspaceDto
    {
        public TesoreriaConciliacion Conciliacion { get; set; } = new();
        public string? NombreCuenta { get; set; }
        public TesoreriaExtractoImport? Extracto { get; set; }
        public ConciliacionSaldosDto Saldos { get; set; } = new();
        public ConciliacionEstadisticasDto Estadisticas { get; set; } = new();
        public bool PuedeCerrar { get; set; }
        public string MotivoNoCerrar { get; set; } = string.Empty;
        public List<string> Bloqueos { get; set; } = new();
        public List<ConciliacionLineaBancoDto> LineasBanco { get; set; } = new();
        public List<MovimientoFinancieroListadoDto> MovimientosLibroPendientes { get; set; } = new();
        public List<TesoreriaConciliacionAuditoria> AuditoriaReciente { get; set; } = new();
    }

    public class ConciliacionSaldosDto
    {
        public decimal SaldoLibrosInicial { get; set; }
        public decimal SaldoLibrosFinal { get; set; }
        public decimal? SaldoBancoInicial { get; set; }
        public decimal? SaldoBancoFinal { get; set; }
        public decimal MontoConciliadoBanco { get; set; }
        public decimal MontoPendienteBanco { get; set; }
        public decimal MontoPendienteLibro { get; set; }

        /// <summary>
        /// Brecha de saldo de cuenta: cierre extracto − saldo libros acumulado.
        /// Puede incluir historia (apertura / movimientos previos). No es por sí sola
        /// una "diferencia de conciliación" del período.
        /// </summary>
        public decimal Diferencia { get; set; }

        /// <summary>Alias semántico de <see cref="Diferencia"/> para la UI.</summary>
        public decimal BrechaBalanceCuenta { get; set; }

        /// <summary>
        /// Saldo ERP inmediatamente antes del marco del extracto
        /// (BalanceInicial + movimientos con fecha &lt; inicio extracto).
        /// </summary>
        public decimal SaldoLibrosInicioExtracto { get; set; }

        /// <summary>Saldo inicial declarado por el extracto, si existe.</summary>
        public decimal? SaldoBancoInicioExtracto { get; set; }

        /// <summary>
        /// Parte del gap atribuible a historia fuera del extracto
        /// (apertura + movimientos previos − saldo inicial del extracto).
        /// </summary>
        public decimal VariacionHistorica { get; set; }

        /// <summary>
        /// Diferencia de conciliación del período/extracto.
        /// 0 cuando no hay pendientes de banco ni de libro del período.
        /// </summary>
        public decimal DiferenciaPeriodo { get; set; }

        public decimal ToleranciaDiferencia { get; set; }

        /// <summary>True si el extracto no tiene líneas pendientes de banco.</summary>
        public bool ExtractoCompletamenteResuelto { get; set; }
    }

    public class ConciliacionEstadisticasDto
    {
        public int LineasBancoTotal { get; set; }
        public int LineasConciliadas { get; set; }
        public int LineasSugeridas { get; set; }
        public int LineasPendientes { get; set; }
        public int LineasAmbiguas { get; set; }
        public int LineasBancariasPurasPendientes { get; set; }
        public int LineasOperativasPendientes { get; set; }
        public int MovimientosLibroPendientes { get; set; }
        public int AutoMatches { get; set; }
        public int MatchesManuales { get; set; }
        public int MovimientosCreados { get; set; }
        public int Ignorados { get; set; }
    }

    public class ConciliacionLineaBancoDto
    {
        public int IdTesoreriaExtractoLinea { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public string? Descripcion { get; set; }
        public string? Referencia { get; set; }
        public decimal Debito { get; set; }
        public decimal Credito { get; set; }
        public decimal MontoNeto { get; set; }
        public decimal? Balance { get; set; }
        public string EstadoMatch { get; set; } = string.Empty;
        public int? IdMovimientoFinanciero { get; set; }
        public decimal? ScoreSugerido { get; set; }
        public string? CategoriaSugerida { get; set; }
        public string? AccionTomada { get; set; }
        public bool EsAutoConciliado { get; set; }
        public string? AccionRecomendada { get; set; }
        public string? ClasificacionLinea { get; set; }
        public string? ModuloOrigenSugerido { get; set; }
        public string? ReglaMatch { get; set; }
        public string? ExplicacionMatch { get; set; }
        public string? InstruccionUsuario { get; set; }
        public MovimientoFinancieroListadoDto? MovimientoSugerido { get; set; }

        /// <summary>Sugerencia de reclasificación Caja→Banco (nunca auto).</summary>
        public bool EsCandidatoReclasificacion { get; set; }
        public string? ConfianzaReclasificacion { get; set; }
        public List<string> EvidenciasReclasificacion { get; set; } = new();
        public int? IdCuentaOrigenSugerida { get; set; }
        public string? NombreCuentaOrigenSugerida { get; set; }
        public string? MetodoPagoOriginalSugerido { get; set; }
        public string? MetodoPagoEfectivoSugerido { get; set; }
        public int? IdFacturaHeaderSugerida { get; set; }
        public string? NumeroDocumentoSugerido { get; set; }
        public string? ClienteSugerido { get; set; }
        public string? TratamientoPrevisto { get; set; }
        public bool CajaOrigenCerrada { get; set; }
        public bool PeriodoOriginalCerrado { get; set; }
        public int? IdPagoReclasificacion { get; set; }
    }

    public class ReclasificarPagoConciliacionDto
    {
        public int IdTesoreriaConciliacion { get; set; }
        public int IdTesoreriaExtractoLinea { get; set; }
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public int IdMovimientoFinanciero { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string? MetodoPagoEfectivo { get; set; }
    }

    public class ReversarReclasificacionPagoDto
    {
        public int IdPagoReclasificacion { get; set; }
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }

    public class ReclasificarPagoResultadoDto
    {
        public int IdPagoReclasificacion { get; set; }
        public int IdMovimientoReclasificacion { get; set; }
        public int? IdAsientoContable { get; set; }
        public string Tratamiento { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string MetodoPagoOriginal { get; set; } = string.Empty;
        public string MetodoPagoEfectivo { get; set; } = string.Empty;
        public DateTime FechaContable { get; set; }
        public bool YaAplicada { get; set; }
    }

    public class AdjuntarExtractoAConciliacionDto
    {
        public int IdTesoreriaConciliacion { get; set; }
        public int IdTesoreriaExtractoImport { get; set; }
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public bool EjecutarMatching { get; set; } = true;
    }

    public class EjecutarMatchingConciliacionDto
    {
        public int IdTesoreriaConciliacion { get; set; }
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public int ToleranciaDias { get; set; } = 3;
        public bool PreservarMatchesManuales { get; set; } = true;
    }

    public class ResolverLineaConciliacionDto
    {
        public int IdTesoreriaConciliacion { get; set; }
        public int IdTesoreriaExtractoLinea { get; set; }
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public AccionExtractoPendiente Accion { get; set; }
        public int? IdMovimientoFinanciero { get; set; }
        public string? Categoria { get; set; }
        public string? Motivo { get; set; }
    }

    public class DeshacerMatchConciliacionDto
    {
        public int IdTesoreriaConciliacion { get; set; }
        public int IdTesoreriaExtractoLinea { get; set; }
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public string? Motivo { get; set; }
    }

    public class BuscarCandidatosMatchDto
    {
        public int IdTesoreriaConciliacion { get; set; }
        public int IdTesoreriaExtractoLinea { get; set; }
        public int IdEmpresa { get; set; }
        public string? Search { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public decimal? Monto { get; set; }
        public int Top { get; set; } = 50;
        /// <summary>Incluye movimientos de otras cuentas (p. ej. Caja) para RECLASIFICAR_PAGO.</summary>
        public bool IncluirOtrasCuentas { get; set; }
    }
}
