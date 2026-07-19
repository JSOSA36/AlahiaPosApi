using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AlahiaPos.Entities.Domain
{
    public class DescuentoHeader
    {
        [Key]
        public int IdDescuentoHeader { get; set; }

        public int IdEmpresa { get; set; }

        // Nombre de la promoción o descuento
        public string NombreEvento { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        // PORCENTAJE | MONTO
        public string TipoDescuento { get; set; } = "PORCENTAJE";

        // Valor del descuento (ej: 20% O 150 pesos)
        public decimal Valor { get; set; }

        // Fecha de inicio y fin del descuento (opcional)
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        // Días de la semana donde aplica
        // Ej: "1,2,3"  → Lunes, Martes, Miércoles
        public string DiasSemana { get; set; } = "";

        // Horario del descuento (Happy Hour)
        public TimeSpan? HoraInicio { get; set; }
        public TimeSpan? HoraFin { get; set; }

        // Si aplica a todos los servicios o no
        public bool AplicaATodos { get; set; } = false;

        // Estado del descuento
        public bool Activo { get; set; } = true;

        // Relación con los servicios incluidos
        public ICollection<DescuentoDetalle> Detalles { get; set; } = new List<DescuentoDetalle>();
        public bool AplicaATodasAreas { get; set; } = false;
        public ICollection<DescuentoAreaDetalle> Areas { get; set; } = new List<DescuentoAreaDetalle>();
        public ICollection<DescuentoCategoriaDetalle> Categorias { get; set; } = new List<DescuentoCategoriaDetalle>();

    }
}
