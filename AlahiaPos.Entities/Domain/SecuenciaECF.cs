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

        // Facturación Electrónica (extensión)
        [MaxLength(100)]
        public string? Descripcion { get; set; }
        public int? TipoEcfDgii { get; set; }
        public int SecuenciaInicial { get; set; } = 1;
        [MaxLength(20)]
        public string Ambiente { get; set; } = "PRUEBAS";
        public DateTime? FechaAutorizacion { get; set; }
        [MaxLength(50)]
        public string? NumeroResolucion { get; set; }

        [ForeignKey(nameof(IdEmpresa))]
        public virtual Empresas? Empresa { get; set; } = null!;
    }
}