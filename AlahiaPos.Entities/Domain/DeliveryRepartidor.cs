using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("DeliveryRepartidor")]
    public class DeliveryRepartidor
    {
        [Key]
        public int IdRepartidor { get; set; }

        public int IdEmpresa { get; set; }

        public int IdUsuario { get; set; }

        public bool Activo { get; set; } = true;

        public bool Disponible { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
