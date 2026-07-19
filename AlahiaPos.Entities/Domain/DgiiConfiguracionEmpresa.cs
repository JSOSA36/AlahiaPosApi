using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("DgiiConfiguracionEmpresa")]
    public class DgiiConfiguracionEmpresa
    {
        [Key]
        public int IdEmpresa { get; set; }
        public string RegimenTributarioCodigo { get; set; } = "ORDINARIO";
        public bool EsConstructor { get; set; }
        public bool EsComisionista { get; set; }
        public bool ObligadoLibroVentasSF { get; set; }
        public string? RazonSocial { get; set; }
        public string? DeclaranteNombre { get; set; }
        public string? DeclaranteCalidad { get; set; }
        public string VersionInstructivoPreferida { get; set; } = "IT-1-2020";
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; }

        // Sprint B — activación por empresa (default off)
        public bool FiscalActivo { get; set; }
        public bool Generar606 { get; set; }
        public bool Generar607 { get; set; }
        public bool GenerarIt1 { get; set; }
        public bool FacturacionElectronicaActiva { get; set; }
    }
}
