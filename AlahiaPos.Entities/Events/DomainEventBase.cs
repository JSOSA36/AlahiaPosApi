namespace AlahiaPos.Entities.Events
{
    /// <summary>
    /// Evento de dominio del ERP Core. No depende del módulo de Contabilidad.
    /// </summary>
    public abstract class DomainEventBase
    {
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public int ReferenciaId { get; set; }
        public string ReferenciaTipo { get; set; } = string.Empty;
        public abstract string TipoEvento { get; }
    }

    public static class DomainEventTypes
    {
        public const string VentaConfirmada = "VentaConfirmada";
        public const string VentaAnulada = "VentaAnulada";
        public const string NotaCreditoCreada = "NotaCreditoCreada";
        public const string CobroClienteRegistrado = "CobroClienteRegistrado";
        public const string GastoRegistrado = "GastoRegistrado";
        public const string GastoAnulado = "GastoAnulado";
        public const string IngresoExtraRegistrado = "IngresoExtraRegistrado";
        public const string IngresoExtraAnulado = "IngresoExtraAnulado";
        public const string CompraConfirmada = "CompraConfirmada";
        public const string CompraAnulada = "CompraAnulada";
        public const string NotaCreditoAnulada = "NotaCreditoAnulada";
        public const string PagoProveedorRegistrado = "PagoProveedorRegistrado";
        public const string MovimientoBancarioRegistrado = "MovimientoBancarioRegistrado";
        public const string InventarioMovimientoRegistrado = "InventarioMovimientoRegistrado";
        public const string InventarioMovimientoAnulado = "InventarioMovimientoAnulado";
        public const string NominaPagada = "NominaPagada";
        /// <summary>Post-commit comercial para consumidores opcionales (DGII fiscal, etc.).</summary>
        public const string DocumentoComercialConfirmado = "DocumentoComercialConfirmado";
        /// <summary>Trabajo fiscal asíncrono (Sprint B.1). No es contable.</summary>
        public const string FiscalFotografiaPendiente = "FiscalFotografiaPendiente";
        /// <summary>Solicitud de trabajo al Centro de Producción (contrato estándar).</summary>
        public const string ProduccionTrabajoSolicitado = "ProduccionTrabajoSolicitado";
        /// <summary>Actualización de snapshot desde el origen (edición de orden, etc.).</summary>
        public const string ProduccionTrabajoActualizado = "ProduccionTrabajoActualizado";
    }
}
