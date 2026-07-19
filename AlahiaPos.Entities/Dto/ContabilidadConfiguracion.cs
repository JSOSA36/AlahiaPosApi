using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Dto
{
    [Table("ContabilidadConfiguracion")]
    public class ContabilidadConfiguracion
    {
        [Key]
        public int IdContabilidadConfiguracion { get; set; }

        public int IdEmpresa { get; set; }

        public bool IntegracionAutomatica { get; set; }

        public bool GenerarCOGSAutomatico { get; set; } = true;

        public bool SepararAsientoCOGS { get; set; } = true;

        public DateTime FechaInseccion { get; set; } = DateTime.Now;

        public DateTime? FechaActualizacion { get; set; }
    }

    public class ContabilidadConfiguracionDto
    {
        public int IdContabilidadConfiguracion { get; set; }
        public int IdEmpresa { get; set; }
        public bool ModuloContratado { get; set; }
        public bool IntegracionAutomatica { get; set; }
        public bool GenerarCOGSAutomatico { get; set; }
        public bool SepararAsientoCOGS { get; set; }
    }

    public class ActualizarContabilidadConfiguracionRequest
    {
        public int IdEmpresa { get; set; }
        public bool IntegracionAutomatica { get; set; }
        public bool GenerarCOGSAutomatico { get; set; } = true;
        public bool SepararAsientoCOGS { get; set; } = true;
    }
}
