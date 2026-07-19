using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("ECFEncabezado")]
    public class ECFEncabezado
    {
        [Key]
        public int IdECF { get; set; }

        // ======================================================
        // 🔗 RELACIONES
        // ======================================================

        [Required]
        public int IdEmpresa { get; set; }

        /// <summary>
        /// Relación con factura interna del sistema
        /// </summary>
        public int? IdFacturaInterna { get; set; }

        // ======================================================
        // 🧾 DATOS DEL e-CF
        // ======================================================

        /// <summary>
        /// 31, 32, 33, 34
        /// </summary>
        [Required]
        [MaxLength(5)]
        public string TipoECF { get; set; } = string.Empty;

        /// <summary>
        /// Número Electrónico (E3100000000001)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ENCF { get; set; } = string.Empty;

        [Required]
        public DateTime FechaEmision { get; set; }

        [MaxLength(20)]
        public string RncEmisor { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? RncReceptor { get; set; }

        [MaxLength(200)]
        public string? NombreReceptor { get; set; }

        // ======================================================
        // 💰 MONTOS
        // ======================================================

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoGravado { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalITBIS { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalGeneral { get; set; }

        // ======================================================
        // 📡 RESPUESTA DGII
        // ======================================================

        /// <summary>
        /// TrackId devuelto por DGII
        /// </summary>
        [MaxLength(100)]
        public string? TrackId { get; set; }

        /// <summary>Id del job en Transmission Engine del proveedor (Alahia.eCF / compatible).</summary>
        [MaxLength(100)]
        public string? TransmissionJobId { get; set; }

        /// <summary>
        /// Pendiente / Enviado / EnProceso / Aceptado / Rechazado
        /// </summary>
        [MaxLength(50)]
        public string EstadoDGII { get; set; } = "Pendiente";

        [MaxLength(50)]
        public string? CodigoError { get; set; }

        public string? MensajeRespuesta { get; set; }

        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaRespuesta { get; set; }

        // ======================================================
        // 📂 CONTROL INTERNO
        // ======================================================

        public bool EnContingencia { get; set; } = false;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // ======================================================
        // Facturación Electrónica (extensión)
        // ======================================================

        /// <summary>
        /// Origen genérico: enum OrigenDocumento persistido como int.
        /// Reemplaza IdFacturaInterna para desacoplar del modelo comercial.
        /// </summary>
        public int OrigenDocumento { get; set; } = 1;

        /// <summary>
        /// PK del documento en su tabla de origen.
        /// </summary>
        public int IdOrigen { get; set; }

        [MaxLength(20)]
        public string? SecurityCode { get; set; }

        [MaxLength(500)]
        public string? UrlQR { get; set; }

        public DateTime? FechaFirma { get; set; }

        [MaxLength(50)]
        public string? NumeroFacturaInterna { get; set; }

        /// <summary>
        /// BORRADOR, PENDIENTE_ENVIO, ENVIADO, ACEPTADO, RECHAZADO, ERROR, ANULADO, CONTINGENCIA
        /// </summary>
        [MaxLength(30)]
        public string EstadoDocumento { get; set; } = EstadoDocumentoElectronico.Borrador;

        // ======================================================
        // 🔗 NAVIGATION PROPERTIES
        // ======================================================

        [ForeignKey(nameof(IdEmpresa))]
        public Empresas Empresa { get; set; } = null!;

        public ICollection<ECFDetalle> Detalles { get; set; } = new List<ECFDetalle>();
        public ICollection<ECFXml> Xmls { get; set; } = new List<ECFXml>();
        public ICollection<ECFHistorialEstado> HistorialEstados { get; set; } = new List<ECFHistorialEstado>();
    }
}