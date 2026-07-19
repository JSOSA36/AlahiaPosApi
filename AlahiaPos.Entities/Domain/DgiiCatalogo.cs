using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("DgiiCatalogo")]
    public class DgiiCatalogo
    {
        [Key]
        public int IdDgiiCatalogo { get; set; }
        public string TipoCatalogo { get; set; } = "";
        public string CodigoDGII { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public DateTime FechaInicioVigencia { get; set; }
        public DateTime? FechaFinVigencia { get; set; }
        public bool Activo { get; set; } = true;
        public string VersionInstructivo { get; set; } = "IT-1-2020";
        public int Orden { get; set; }
        public string? MetaJson { get; set; }
    }
}
