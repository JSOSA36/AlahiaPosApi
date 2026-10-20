using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("PedidoOnline")]
    public class PedidoOnline
    {
        [Key]
        public int IdPedidoOnline { get; set; }

        public int IdEmpresa { get; set; }

        public int? IdSucursal { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal CostoDelivery { get; set; }

        [Column(TypeName = "decimal(10,3)")]
        public decimal DistanciaDeliveryKm { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? TarifaBaseAplicada { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? PrecioKmAplicado { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? RedondeoAplicado { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? MinimoAplicado { get; set; }

        [MaxLength(40)]
        public string? ProveedorDistancia { get; set; }

        [Column(TypeName = "decimal(10,7)")]
        public decimal? OrigenLatitud { get; set; }

        [Column(TypeName = "decimal(10,7)")]
        public decimal? OrigenLongitud { get; set; }

        [Column(TypeName = "decimal(10,7)")]
        public decimal? DestinoLatitud { get; set; }

        [Column(TypeName = "decimal(10,7)")]
        public decimal? DestinoLongitud { get; set; }

        public DateTime? FechaCalculoDelivery { get; set; }

        public bool DeliveryCobrarAlEntregar { get; set; }

        public int IdCanal { get; set; }

        public int IdFacturaHeader { get; set; }

        public int? IdCliente { get; set; }

        [MaxLength(120)]
        public string NombreCliente { get; set; } = "";

        [MaxLength(30)]
        public string Telefono { get; set; } = "";

        [MaxLength(20)]
        public string TipoEntrega { get; set; } = "Delivery";

        [MaxLength(300)]
        public string? Direccion { get; set; }

        [MaxLength(300)]
        public string? ReferenciaDireccion { get; set; }

        [Column(TypeName = "decimal(10,7)")]
        public decimal? Latitud { get; set; }

        [Column(TypeName = "decimal(10,7)")]
        public decimal? Longitud { get; set; }

        [MaxLength(40)]
        public string MetodoPago { get; set; } = "Efectivo";

        [MaxLength(260)]
        public string? VoucherRuta { get; set; }

        public bool PagoValidado { get; set; }

        public DateTime? FechaValidacionPago { get; set; }

        public int? IdUsuarioValidaPago { get; set; }

        public DateTime? FechaEnvioCocina { get; set; }

        [MaxLength(500)]
        public string? Observacion { get; set; }

        [MaxLength(30)]
        public string EstadoLogistico { get; set; } = PedidoOnlineEstados.Nuevo;

        [MaxLength(80)]
        public string? IdempotencyKey { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }

    public static class PedidoOnlineEstados
    {
        public const string Nuevo = "Nuevo";
        public const string Listo = "Listo";
        public const string Asignado = "Asignado";
        public const string Recogido = "Recogido";
        public const string EnCamino = "EnCamino";
        public const string Entregado = "Entregado";
        public const string Cancelado = "Cancelado";
    }

    public static class PedidoOnlineTiposEntrega
    {
        public const string Delivery = "Delivery";
        public const string Recoger = "Llevar";
    }
}
