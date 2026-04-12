using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    public class ECFXml
    {
        [Key]
        public int IdXml { get; set; }

        [Required]
        public int IdECF { get; set; }

        /// <summary>
        /// XML generado antes de firma digital
        /// </summary>
        public string XmlSinFirmar { get; set; } = string.Empty;

        /// <summary>
        /// XML firmado digitalmente
        /// </summary>
        public string? XmlFirmado { get; set; }

        /// <summary>
        /// XML enviado a DGII (puede incluir ajustes)
        /// </summary>
        public string? XmlEnviado { get; set; }

        /// <summary>
        /// Respuesta XML completa de DGII
        /// </summary>
        public string? XmlRespuestaDGII { get; set; }

        /// <summary>
        /// Hash SHA256 del XML firmado (opcional pero recomendado)
        /// </summary>
        [MaxLength(200)]
        public string? HashDocumento { get; set; }

        /// <summary>
        /// Número de intento de envío
        /// </summary>
        public int IntentoEnvio { get; set; } = 1;

        public DateTime FechaGeneracion { get; set; } = DateTime.Now;

        public DateTime? FechaEnvio { get; set; }

        public DateTime? FechaRespuesta { get; set; }

        // ======================================================
        // 🔗 RELACIÓN
        // ======================================================

        [ForeignKey(nameof(IdECF))]
        public ECFEncabezado ECFEncabezado { get; set; } = null!;
    }
}