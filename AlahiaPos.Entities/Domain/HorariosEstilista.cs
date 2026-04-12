using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace AlahiaPos.Entities.Domain
{
    
 

    public class HorariosEstilista:BaseEntity
    {
        [Key]
        public int IdHorario { get; set; }

        [Required]
        public int IdEmpleado { get; set; }   // FK hacia empleado

        [ForeignKey("IdEmpleado")]
        public Empleados Estilista { get; set; }

        [Required]
        [Range(1, 7)]
        public int DiaSemana { get; set; } // 1 = Lunes, 7 = Domingo

        [Required]
        public TimeSpan HoraInicio { get; set; }

        [Required]
        public TimeSpan HoraFin { get; set; }
        public TimeSpan? RecesoInicio { get; set; }
        public TimeSpan? RecesoFin { get; set; }

    }

}
