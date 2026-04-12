using AlahiaPos.Entities.Domain;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AlahiaPos.Entities.Dto
{
    public class HorarioEstilistaDto:BaseEntity
    {
        public int IdHorario { get; set; }

        [Required]
        public int IdEmpleado { get; set; }   // 🔹 Estilista asignado

        

        [Required]
        [Range(0, 6, ErrorMessage = "El día debe estar entre 0 (Domingo) y 6 (Sábado)")]
        public int DiaSemana { get; set; }    // 🔹 0=Domingo, 1=Lunes, ... 6=Sábado

        [Required]
        public TimeSpan HoraInicio { get; set; }   // 🔹 Ejemplo: 09:00

        [Required]
        public TimeSpan HoraFin { get; set; }      // 🔹 Ejemplo: 18:00
        public TimeSpan? RecesoInicio { get; set; }
        public TimeSpan? RecesoFin { get; set; }
    }
}
