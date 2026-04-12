using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    public class ECFHistorialEstado
    {
        [Key]
        public int IdHistorial { get; set; }

        [Required]
        public int IdECF { get; set; }

        /// <summary>
        /// Estado del documento en ese momento
        /// Ej: Generado, Firmado, Enviado, EnProceso, Aceptado, Rechazado, Contingencia
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Estado { get; set; } = string.Empty;

        /// <summary>
        /// Código de respuesta (si aplica)
        /// </summary>
        [MaxLength(50)]
        public string? Codigo { get; set; }

        /// <summary>
        /// Mensaje detallado devuelto por DGII o sistema
        /// </summary>
        public string? Mensaje { get; set; }

        /// <summary>
        /// Usuario que realizó la acción (opcional pero recomendado)
        /// </summary>
        [MaxLength(100)]
        public string? UsuarioAccion { get; set; }

        /// <summary>
        /// IP o dispositivo desde donde se ejecutó la acción (opcional)
        /// </summary>
        [MaxLength(100)]
        public string? Origen { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        // ======================================================
        // 🔗 RELACIÓN
        // ======================================================

        [ForeignKey(nameof(IdECF))]
        public ECFEncabezado ECFEncabezado { get; set; } = null!;
    }
}