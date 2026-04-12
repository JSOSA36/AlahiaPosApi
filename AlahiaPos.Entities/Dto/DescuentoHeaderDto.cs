using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class DescuentoHeaderDto
    {
        public int IdDescuentoHeader { get; set; }
        public int IdEmpresa { get; set; }

        public string NombreEvento { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;

        public string TipoDescuento { get; set; } = "PORCENTAJE";
        public decimal Valor { get; set; }

        public string DiasSemana { get; set; } = "";
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        public TimeSpan? HoraInicio { get; set; }
        public TimeSpan? HoraFin { get; set; }

        public bool AplicaATodos { get; set; } = false;
        public bool Activo { get; set; } = true;

        public List<int> Servicios { get; set; } = new();
        public List<int> Areas { get; set; } = new();
    }

}
