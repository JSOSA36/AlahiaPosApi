using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("ProduccionFlujo")]
    public class ProduccionFlujo
    {
        [Key]
        public int IdFlujo { get; set; }

        public int? IdEmpresa { get; set; }

        [Required, MaxLength(40)]
        public string TipoTrabajoCodigo { get; set; } = "";

        [Required, MaxLength(120)]
        public string Nombre { get; set; } = "";

        public bool Activo { get; set; } = true;
        public int Version { get; set; } = 1;
        public int SlaObjetivoSegundos { get; set; }
        public int SlaAdvertenciaSegundos { get; set; }

        /// <summary>CREACION | INICIO_PREPARACION — define el origen del reloj SLA.</summary>
        [Required, MaxLength(30)]
        public string SlaModoInicio { get; set; } = "CREACION";

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public ICollection<ProduccionFlujoEstado> Estados { get; set; } = new List<ProduccionFlujoEstado>();
        public ICollection<ProduccionFlujoTransicion> Transiciones { get; set; } = new List<ProduccionFlujoTransicion>();
    }
}
