using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("SuscripcionAvisoLog")]
    public class SuscripcionAvisoLog
    {
        [Key]
        public int IdAvisoLog { get; set; }
        public int IdEmpresa { get; set; }
        public int? IdCiclo { get; set; }

        [MaxLength(40)]
        public string TipoAviso { get; set; } = "";

        [MaxLength(40)]
        public string Canal { get; set; } = "";

        public DateTime FechaEnvio { get; set; } = DateTime.Now;
    }
}
