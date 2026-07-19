using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("OrdenCompraHeaders")]
    public class OrdenCompraHeader : BaseEntity
    {
        [Key]
        public int IdOrdenCompraHeader { get; set; }
        public string? NumeroDocumento { get; set; }
        public int? IdTipoDocumentos { get; set; }
        public int? IdTipoBienesServicios { get; set; }
        public decimal TotalItbis { get; set; }
        public decimal Total { get; set; }
        public decimal Pagado { get; set; }
        public decimal Pendiente { get; set; }
        public int IdProveedor { get; set; }
        public string? CondicionFactura { get; set; }
        public string? Comentario { get; set; }
        public string? NCF { get; set; }
        [NotMapped]
        public virtual Proveedores? Proveedores { get; set; }
        [NotMapped]
        public virtual TipoDocumentos? TipoDocumentos { get; set; }

        public decimal TotalDescuento { get; set; }
        public string? Estado { get; set; }
        public string? TipoFacturaGasto { get; set; }
        public DateTime FechaBencimiento { get; set; }
        public bool Seleccione { get; set; }

        /// <summary>
        /// Legacy: true cuando la recepción física quedó completa (EstadoRecepcion = RECIBIDA).
        /// Ya no se setea al confirmar la factura.
        /// </summary>
        public bool AjustadaInventario { get; set; }

        public int? IdAlmacen { get; set; }

        /// <summary>
        /// NO_APLICA | PENDIENTE_RECEPCION | PARCIALMENTE_RECIBIDA | RECIBIDA
        /// Independiente del estado de pago (Estado).
        /// </summary>
        public string? EstadoRecepcion { get; set; }

        public DateTime? FechaUltimaRecepcion { get; set; }

        /// <summary>
        /// En FACTC: Id de la Orden de Compra (tipo 5) origen.
        /// </summary>
        public int? IdDocumentoOrigen { get; set; }

        /// <summary>Cuando se marcó/envió la OC al proveedor.</summary>
        public DateTime? FechaEnvioProveedor { get; set; }

        // ---- Campos fiscales Formato 606 (DGII) ----

        /// <summary>NCF o documento modificado por NC/ND (campo 5 del 606).</summary>
        public string? NcfModificado { get; set; }

        /// <summary>Forma de pago DGII 1-7 (campo 23 del 606).</summary>
        public int? FormaPagoDgii { get; set; }

        /// <summary>Monto facturado en servicios sin ITBIS (campo 8).</summary>
        public decimal MontoFacturadoServicios { get; set; }

        /// <summary>Monto facturado en bienes sin ITBIS (campo 9).</summary>
        public decimal MontoFacturadoBienes { get; set; }

        public decimal ItbisRetenido { get; set; }

        /// <summary>ITBIS sujeto a proporcionalidad Art. 349 (campo 13).</summary>
        public decimal ItbisProporcionalidad { get; set; }

        public decimal ItbisLlevadoAlCosto { get; set; }

        /// <summary>Tipo retención ISR 1-9 (campo 17). Null = no aplica.</summary>
        public int? TipoRetencionIsr { get; set; }

        public decimal MontoRetencionRenta { get; set; }

        /// <summary>Fecha de pago fiscal del comprobante (campo 7). Obligatoria si hay retenciones.</summary>
        public DateTime? FechaPagoFiscal { get; set; }

        // --- Destino ITBIS / foto (Sprint A). No altera TXT 606. ---
        /// <summary>Destino confirmado. Null mientras PENDIENTE_VALIDAR.</summary>
        public byte? DestinoItbis { get; set; }
        public byte? DestinoItbisSugerido { get; set; }
        public string EstadoClasificacionItbis { get; set; } = "NO_APLICA";
        public bool ClasificacionConfirmada { get; set; }
        public DateTime? FechaClasificacion { get; set; }
        public int? IdUsuarioClasificacion { get; set; }
        public decimal ItbisComprasLocales { get; set; }
        public decimal ItbisServicios { get; set; }
        public decimal ItbisImportaciones { get; set; }
        public decimal? TasaItbis { get; set; }
        public string? CodigoNormaRetencionItbis { get; set; }
        public decimal BaseRetencionItbis { get; set; }
        public string? RegimenFiscalProveedorCodigo { get; set; }
        public bool EsImportacion { get; set; }
        public int FotografiaFiscalVersion { get; set; }
        public string? EstadoFiscalDocumento { get; set; }

        [NotMapped]
        public virtual ICollection<OrdenCompraDetalle>? Detalles { get; set; }
    }
}
