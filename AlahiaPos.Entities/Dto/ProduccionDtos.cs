namespace AlahiaPos.Entities.Dto
{
    public static class ProduccionConstantes
    {
        public const string CodigoModulo = "CENTRO_PRODUCCION";
        public const string TipoPosOrden = "POS_ORDEN";
        public const string EstacionGeneral = "GENERAL";
        public const string OrigenModuloPos = "POS";
        public const string OrigenModuloOnline = "ONLINE";
        public const string OrigenTipoFacturaHeader = "FacturaHeader";
        public const string PrioridadNormal = "Normal";
        public const string PrioridadAlta = "Alta";
        public const string PrioridadUrgente = "Urgente";
        public const string HistorialOrigenSistema = "Sistema";
        public const string HistorialOrigenUi = "UI";
        public const string HistorialOrigenEvento = "Evento";

        public const string SemaforoOk = "OK";
        public const string SemaforoAdvertencia = "ADVERTENCIA";
        public const string SemaforoRetrasado = "RETRASADO";
        public const string SemaforoCompletado = "COMPLETADO";
        /// <summary>SLA aún no arrancó (modo INICIO_PREPARACION sin FechaInicio).</summary>
        public const string SemaforoEnCola = "EN_COLA";

        /// <summary>SLA desde FechaCreacion del trabajo.</summary>
        public const string SlaModoCreacion = "CREACION";
        /// <summary>SLA desde FechaInicio (inicio de preparación).</summary>
        public const string SlaModoInicioPreparacion = "INICIO_PREPARACION";
    }

    public class ProduccionTrabajoItemSolicitudDto
    {
        public int? OrigenDetalleId { get; set; }
        public string? CodigoItem { get; set; }
        public string NombreItem { get; set; } = "";
        public decimal Cantidad { get; set; } = 1;
        public string? Observacion { get; set; }
        public string? VariacionesTexto { get; set; }
        public string? EstacionCodigo { get; set; }
    }

    public class ProduccionConfiguracionDto
    {
        public int IdEmpresa { get; set; }
        public bool Activo { get; set; }
        public bool UsarEstaciones { get; set; }
        public bool UsarEstadosPorItem { get; set; }
        public bool SonidoActivo { get; set; }
        public int TiempoAdvertenciaSegDefault { get; set; }
        public int TiempoCriticoSegDefault { get; set; }
        public bool PermitirCompletarDesdeEstacion { get; set; }
        public int? IdEstacionPredeterminada { get; set; }
        public bool ModoOscuroDefault { get; set; }
        public bool MostrarNombreCliente { get; set; }
        public bool MostrarUsuarioSolicita { get; set; }
    }

    public class ProduccionFlujoEstadoDto
    {
        public string Codigo { get; set; } = "";
        public string NombreVisible { get; set; } = "";
        public int Orden { get; set; }
        public bool EsInicial { get; set; }
        public bool EsTerminal { get; set; }
        public bool CuentaParaCompletar { get; set; }
        public string? ColorHint { get; set; }
    }

    public class ProduccionFlujoDto
    {
        public int IdFlujo { get; set; }
        public string TipoTrabajoCodigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public int Version { get; set; }
        public int SlaObjetivoSegundos { get; set; }
        public int SlaAdvertenciaSegundos { get; set; }
        /// <summary>CREACION | INICIO_PREPARACION</summary>
        public string SlaModoInicio { get; set; } = ProduccionConstantes.SlaModoCreacion;
        public List<ProduccionFlujoEstadoDto> Estados { get; set; } = new();
    }

    public class ProduccionTrabajoItemDto
    {
        public int IdTrabajoItem { get; set; }
        public int? OrigenDetalleId { get; set; }
        public int? IdEstacion { get; set; }
        public string? CodigoItem { get; set; }
        public string NombreItem { get; set; } = "";
        public decimal Cantidad { get; set; }
        public string? Observacion { get; set; }
        public string? VariacionesTexto { get; set; }
        public string CodigoEstado { get; set; } = "";
        public int Orden { get; set; }
        public string RowVersion { get; set; } = "";
    }

    public class ProduccionTrabajoDto
    {
        public int IdTrabajo { get; set; }
        public int IdEmpresa { get; set; }
        public string TipoTrabajoCodigo { get; set; } = "";
        public int IdFlujo { get; set; }
        public string CodigoEstadoActual { get; set; } = "";
        public string? NombreEstadoActual { get; set; }
        public string OrigenModulo { get; set; } = "";
        public string OrigenTipo { get; set; } = "";
        public int OrigenId { get; set; }
        public string NumeroVisible { get; set; } = "";
        public string NombreVisible { get; set; } = "";
        public string? Referencia { get; set; }
        public string? EtiquetaContexto { get; set; }
        public string? Observacion { get; set; }
        public int? IdUsuarioSolicita { get; set; }
        public string Prioridad { get; set; } = "";
        public int SlaObjetivoSegundosSnapshot { get; set; }
        public int SlaAdvertenciaSegundosSnapshot { get; set; }
        /// <summary>CREACION | INICIO_PREPARACION (snapshot al crear).</summary>
        public string SlaModoInicioSnapshot { get; set; } = ProduccionConstantes.SlaModoCreacion;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaLimiteObjetivo { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaCompletado { get; set; }
        public DateTime? FechaCancelacion { get; set; }
        public string? MotivoCancelacion { get; set; }
        public bool ActivoEnTablero { get; set; }
        public string? PlantillaCodigo { get; set; }
        public string SemaforoSla { get; set; } = "";
        /// <summary>Segundos del reloj SLA activo (según modo). 0 si EN_COLA.</summary>
        public int SegundosTranscurridos { get; set; }
        /// <summary>FechaCreacion → FechaInicio (o ahora si aún no inició).</summary>
        public int SegundosEnCola { get; set; }
        /// <summary>FechaInicio → FechaCompletado (o ahora si en curso).</summary>
        public int SegundosPreparacion { get; set; }
        /// <summary>FechaCreacion → FechaCompletado (o ahora).</summary>
        public int SegundosTotal { get; set; }
        public string RowVersion { get; set; } = "";
        public List<ProduccionTrabajoItemDto> Items { get; set; } = new();
    }

    public class ProduccionHistorialDto
    {
        public int IdHistorial { get; set; }
        public int IdTrabajo { get; set; }
        public int? IdTrabajoItem { get; set; }
        public string? CodigoEstadoAnterior { get; set; }
        public string CodigoEstadoNuevo { get; set; } = "";
        public int? IdUsuario { get; set; }
        public DateTime Fecha { get; set; }
        public string? Motivo { get; set; }
        public string Origen { get; set; } = "";
    }

    public class ProduccionTransicionRequest
    {
        public string CodigoEstadoEsperado { get; set; } = "";
        public string CodigoEstadoNuevo { get; set; } = "";
        public string RowVersion { get; set; } = "";
        public string? Motivo { get; set; }
        public int IdUsuario { get; set; }
    }

    public class ProduccionCancelarRequest
    {
        public string CodigoEstadoEsperado { get; set; } = "";
        public string RowVersion { get; set; } = "";
        public string Motivo { get; set; } = "";
        public int IdUsuario { get; set; }
    }

    public class ProduccionPrioridadRequest
    {
        public string Prioridad { get; set; } = "";
        public string RowVersion { get; set; } = "";
        public string CodigoEstadoEsperado { get; set; } = "";
        public int IdUsuario { get; set; }
    }

    public class ProduccionDashboardResumenDto
    {
        public int Pendientes { get; set; }
        public int EnEjecucion { get; set; }
        public int CompletadosHoy { get; set; }
        public int Retrasados { get; set; }
        public double? TiempoPromedioSegundosHoy { get; set; }
    }

    /// <summary>Estado liviano por documento origen (listado de órdenes / mesero).</summary>
    public class ProduccionEstadoOrigenDto
    {
        public int OrigenId { get; set; }
        public int IdTrabajo { get; set; }
        public string CodigoEstado { get; set; } = "";
        public string? NombreEstado { get; set; }
        public bool ActivoEnTablero { get; set; }
    }
}
