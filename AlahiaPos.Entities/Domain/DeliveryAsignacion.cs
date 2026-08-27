using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("DeliveryAsignacion")]
    public class DeliveryAsignacion
    {
        [Key]
        public int IdAsignacion { get; set; }

        public int IdEmpresa { get; set; }

        public int IdPedidoOnline { get; set; }

        public int IdUsuarioRepartidor { get; set; }

        public int? IdUsuarioAsigna { get; set; }

        [MaxLength(20)]
        public string Estado { get; set; } = DeliveryEstados.Asignado;

        public bool Activa { get; set; } = true;

        public DateTime FechaAsignacion { get; set; } = DateTime.Now;

        public DateTime? FechaRecogido { get; set; }

        public DateTime? FechaEnCamino { get; set; }

        public DateTime? FechaEntregado { get; set; }
    }

    public static class DeliveryEstados
    {
        public const string Asignado = "Asignado";
        public const string Recogido = "Recogido";
        public const string EnCamino = "EnCamino";
        public const string Entregado = "Entregado";
    }
}
