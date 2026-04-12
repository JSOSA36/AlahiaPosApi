using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AlahiaPos.Entities.Dto
{
    public class HorarioEstilistaListaDto
    {
        [Required]
        public List<HorarioEstilistaDto> Horarios { get; set; } = new();
    }
}
