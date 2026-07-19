using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Dto
{
    [Table("PeriodosContables")]
    public class PeriodoContable
    {
        [Key]
        public int IdPeriodoContable { get; set; }

        public int IdEmpresa { get; set; }

        public int Anio { get; set; }

        public int Mes { get; set; }

        [MaxLength(20)]
        public string Estado { get; set; } = ContabilidadConstantes.PeriodoAbierto;

        public DateTime? FechaCierre { get; set; }

        public int? IdUsuarioCierre { get; set; }

        [MaxLength(500)]
        public string? Observacion { get; set; }

        public DateTime FechaInseccion { get; set; } = DateTime.Now;
    }

    public class PeriodoContableDto
    {
        public int IdPeriodoContable { get; set; }
        public int IdEmpresa { get; set; }
        public int Anio { get; set; }
        public int Mes { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaCierre { get; set; }
        public string? Observacion { get; set; }
        public string NombreMes { get; set; } = string.Empty;
        public bool BloqueoActivo { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }

    public class CerrarPeriodoRequest
    {
        public int IdEmpresa { get; set; }
        public int Anio { get; set; }
        public int Mes { get; set; }
        public int IdUsuario { get; set; }
        public string? Observacion { get; set; }
    }
}
