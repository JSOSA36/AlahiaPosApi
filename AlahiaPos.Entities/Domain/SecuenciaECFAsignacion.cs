using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    /// <summary>
    /// Tramo de una secuencia e-CF de la empresa asignado a una sucursal.
    /// La autorización DGII sigue en <see cref="SecuenciaECF"/>; aquí solo se parte el rango.
    /// </summary>
    [Table("SecuenciaECFAsignacion")]
    public class SecuenciaECFAsignacion
    {
        [Key]
        public int IdAsignacion { get; set; }

        public int IdSecuencia { get; set; }

        public int IdEmpresa { get; set; }

        public int TipoEcfDgii { get; set; }

        public int IdSucursal { get; set; }

        public int SecuenciaInicial { get; set; }

        public int SecuenciaFinal { get; set; }

        /// <summary>Próximo número a emitir en este tramo.</summary>
        public int SecuenciaActual { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [ForeignKey(nameof(IdSecuencia))]
        public virtual SecuenciaECF? Secuencia { get; set; }

        [ForeignKey(nameof(IdSucursal))]
        public virtual Sucursal? Sucursal { get; set; }
    }
}
