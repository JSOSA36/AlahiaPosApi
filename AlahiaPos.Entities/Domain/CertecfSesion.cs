using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("CertecfSesion")]
    public class CertecfSesion
    {
        [Key]
        public int IdSesion { get; set; }

        public int IdEmpresa { get; set; }

        public int? IdUsuario { get; set; }

        [MaxLength(260)]
        public string NombreArchivo { get; set; } = "";

        [MaxLength(20)]
        public string TipoSet { get; set; } = "ECF";

        [MaxLength(30)]
        public string Estado { get; set; } = "Cargado";

        [MaxLength(20)]
        public string Ambiente { get; set; } = "certecf";

        [MaxLength(1000)]
        public string? Mensaje { get; set; }

        public int PasoActual { get; set; } = 1;

        [MaxLength(80)]
        public string? NombreSoftware { get; set; }

        [MaxLength(20)]
        public string? VersionSoftware { get; set; }

        [MaxLength(40)]
        public string? TipoSoftware { get; set; }

        [MaxLength(500)]
        public string? UrlRecepcion { get; set; }

        [MaxLength(500)]
        public string? UrlAprobacion { get; set; }

        [MaxLength(500)]
        public string? UrlAutenticacion { get; set; }

        [MaxLength(500)]
        public string? UrlRecepcionProd { get; set; }

        [MaxLength(500)]
        public string? UrlAprobacionProd { get; set; }

        [MaxLength(500)]
        public string? UrlAutenticacionProd { get; set; }

        public string? JsonPasos { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime? FechaActualizacion { get; set; }

        public ICollection<CertecfCaso> Casos { get; set; } = new List<CertecfCaso>();
    }
}
