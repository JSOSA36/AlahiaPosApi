using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("CertificadoDigital")]
    public class CertificadoDigital
    {
        [Key]
        public int IdCertificado { get; set; }

        [Required]
        public int IdEmpresa { get; set; }

        /// <summary>
        /// Nombre original del archivo (.p12)
        /// </summary>
        [MaxLength(200)]
        public string NombreArchivo { get; set; } = string.Empty;

        /// <summary>
        /// Ruta física en servidor (si no guardas en DB)
        /// </summary>
        [MaxLength(500)]
        public string? RutaArchivo { get; set; }

        /// <summary>
        /// Si decides guardar el archivo en base de datos (opcional)
        /// </summary>
        public byte[]? ArchivoBytes { get; set; }

        /// <summary>
        /// Password encriptado (NUNCA plano)
        /// </summary>
        [Required]
        public string PasswordEncriptado { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de expiración del certificado
        /// </summary>
        public DateTime FechaExpiracion { get; set; }

        /// <summary>
        /// Indica si es el certificado activo actual
        /// </summary>
        public bool Activo { get; set; } = true;

        /// <summary>
        /// Ambiente donde aplica (PRUEBA / PRODUCCION)
        /// </summary>
        [MaxLength(20)]
        public string? Ambiente { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // ======================================================
        // 🔗 RELACIÓN
        // ======================================================

        [ForeignKey(nameof(IdEmpresa))]
        public Empresas Empresa { get; set; } = null!;
    }
}