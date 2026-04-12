using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    public class SecuenciaECF
    {
        [Key]
        public int IdSecuencia { get; set; }
        public int IdEmpresa { get; set; }
        public string TipoNCF { get; set; }
        public string Serie { get; set; }
        public int SecuenciaActual { get; set; }
        public int SecuenciaFinal { get; set; }
        public DateTime fechaVencimiento { get; set; }
        public int stockMinimo { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }

        // ======================================================
        // 🔗 RELACIÓN
        // ======================================================

        [ForeignKey(nameof(IdEmpresa))]
        public virtual Empresas? Empresa { get; set; } = null!;
    }
}