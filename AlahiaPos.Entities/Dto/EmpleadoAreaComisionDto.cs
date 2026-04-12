using AlahiaPos.Entities.Domain;
using System.ComponentModel.DataAnnotations;

namespace AlahiaPos.Entities.Dto
{
    public class EmpleadoAreaComisionDto
    {
        [Key]
        public int IdEmpleadoAreaComision { get; set; }

        public int IdEmpleado { get; set; }

        public int IdEmpresa { get; set; }

        public int IdArea { get; set; }

        // 🔥 NUEVO
        public string? TipoComision { get; set; } = "PORCIENTO";

        // 🔥 % tradicional
        public decimal? PorcientoComision { get; set; }

        // 🔥 monto fijo estilo carwash
        public decimal? MontoComision { get; set; }

        public virtual Area? Area { get; set; }
    }
}