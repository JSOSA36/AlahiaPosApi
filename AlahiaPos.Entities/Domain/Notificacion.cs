using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("Notificaciones")]
    public class Notificacion
    {
        [Key]
        public int IdNotificacion { get; set; }

        public int IdEmpresa { get; set; }

        /// <summary>USUARIO | EMPRESA | ROL</summary>
        [Required, MaxLength(20)]
        public string DestinoTipo { get; set; } = NotificacionDestinos.Empresa;

        public int? IdUsuarioDestino { get; set; }

        /// <summary>Reservado: IdPerfil / código de rol destino (futuro).</summary>
        public int? IdRolDestino { get; set; }

        [MaxLength(60)]
        public string? RolCodigo { get; set; }

        [Required, MaxLength(60)]
        public string Tipo { get; set; } = "";

        /// <summary>INFO | ADVERTENCIA | ERROR | EXITO</summary>
        [Required, MaxLength(20)]
        public string Prioridad { get; set; } = NotificacionPrioridades.Info;

        [Required, MaxLength(200)]
        public string Titulo { get; set; } = "";

        [Required, MaxLength(500)]
        public string Mensaje { get; set; } = "";

        [MaxLength(200)]
        public string? Ruta { get; set; }

        [MaxLength(60)]
        public string? ReferenciaTipo { get; set; }

        public int? ReferenciaId { get; set; }
        public string? MetadataJson { get; set; }

        public bool Leida { get; set; }

        /// <summary>Soft-archive; nunca se elimina físicamente.</summary>
        public bool Archivada { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaLeida { get; set; }
        public DateTime? FechaArchivada { get; set; }
    }

    [Table("NotificacionCanalLog")]
    public class NotificacionCanalLog
    {
        [Key]
        public int Id { get; set; }
        public int IdNotificacion { get; set; }

        [MaxLength(40)]
        public string Canal { get; set; } = "";

        public DateTime FechaEnvio { get; set; } = DateTime.Now;
        public bool Exito { get; set; } = true;

        [MaxLength(500)]
        public string? Detalle { get; set; }
    }

    public static class NotificacionDestinos
    {
        public const string Usuario = "USUARIO";
        public const string Empresa = "EMPRESA";
        public const string Rol = "ROL";
    }

    public static class NotificacionPrioridades
    {
        public const string Info = "INFO";
        public const string Advertencia = "ADVERTENCIA";
        public const string Error = "ERROR";
        public const string Exito = "EXITO";
    }

    public static class NotificacionTipos
    {
        public const string TicketNuevo = "TICKET_NUEVO";
        public const string TicketNuevoMensaje = "TICKET_NUEVO_MENSAJE";
        public const string TicketEstadoCambiado = "TICKET_ESTADO_CAMBIADO";

        public const string PagoPendiente = "PAGO_PENDIENTE";
        public const string PagoAprobado = "PAGO_APROBADO";
        public const string PagoRechazado = "PAGO_RECHAZADO";
        public const string ServicioSuspendido = "SERVICIO_SUSPENDIDO";
        public const string CambioPlan = "CAMBIO_PLAN";

        public const string EcfAceptado = "ECF_ACEPTADO";
        public const string EcfRechazado = "ECF_RECHAZADO";

        public const string InventarioBajo = "INVENTARIO_BAJO";
        public const string CxcVencida = "CXC_VENCIDA";
        public const string CompraPendiente = "COMPRA_PENDIENTE";
        public const string AvisoAdministrativo = "AVISO_ADMINISTRATIVO";
        public const string PedidoDeliveryAsignado = "PEDIDO_DELIVERY_ASIGNADO";
        public const string CierreCaja = "CIERRE_CAJA";
    }

    public static class NotificacionCanales
    {
        public const string InApp = "IN_APP";
        public const string Email = "EMAIL";
        public const string Push = "PUSH";
        public const string WhatsApp = "WHATSAPP";
    }
}
