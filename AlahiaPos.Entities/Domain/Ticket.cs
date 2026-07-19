using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("Tickets")]
    public class Ticket
    {
        [Key]
        public int IdTicket { get; set; }

        [Required, MaxLength(30)]
        public string Numero { get; set; } = "";

        public int IdEmpresa { get; set; }
        public int IdUsuarioCrea { get; set; }

        [MaxLength(200)]
        public string? NombreCliente { get; set; }

        [Required, MaxLength(250)]
        public string Asunto { get; set; } = "";

        [Required]
        public string Descripcion { get; set; } = "";

        [Required, MaxLength(60)]
        public string Categoria { get; set; } = "";

        [Required, MaxLength(20)]
        public string Prioridad { get; set; } = TicketEstados.PrioridadMedia;

        [Required, MaxLength(30)]
        public string Estado { get; set; } = TicketEstados.Abierto;

        [MaxLength(40)]
        public string? VersionSistema { get; set; }

        [MaxLength(80)]
        public string? Dispositivo { get; set; }

        [MaxLength(120)]
        public string? Navegador { get; set; }

        [MaxLength(120)]
        public string? SistemaOperativo { get; set; }

        public DateTime? FechaUltimaRespuestaAdmin { get; set; }
        public DateTime? FechaResolucion { get; set; }
        public DateTime? FechaCierre { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;

        /// <summary>Estimado inferior de atención (horas). Piso: 2.</summary>
        public int? HorasEstimadasMin { get; set; }

        /// <summary>Estimado superior de atención (horas). Techo: 24.</summary>
        public int? HorasEstimadasMax { get; set; }

        /// <summary>Fecha tope del estimado (creación + HorasEstimadasMax).</summary>
        public DateTime? FechaEstimadaResolucion { get; set; }
    }

    [Table("TicketMensajes")]
    public class TicketMensaje
    {
        [Key]
        public int IdMensaje { get; set; }
        public int IdTicket { get; set; }
        public int IdUsuario { get; set; }
        public bool EsRespuestaAdmin { get; set; }
        public string Mensaje { get; set; } = "";
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }

    [Table("TicketAdjuntos")]
    public class TicketAdjunto
    {
        [Key]
        public int IdAdjunto { get; set; }
        public int IdTicket { get; set; }
        public int? IdMensaje { get; set; }

        [MaxLength(260)]
        public string NombreArchivo { get; set; } = "";

        [MaxLength(500)]
        public string Url { get; set; } = "";

        [MaxLength(120)]
        public string? ContentType { get; set; }

        public long? TamanoBytes { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }

    [Table("TicketNotificaciones")]
    public class TicketNotificacion
    {
        [Key]
        public int IdNotificacion { get; set; }
        public int IdEmpresa { get; set; }
        public int? IdUsuarioDestino { get; set; }
        public int IdTicket { get; set; }

        [MaxLength(40)]
        public string Tipo { get; set; } = "";

        [MaxLength(200)]
        public string Titulo { get; set; } = "";

        [MaxLength(500)]
        public string Mensaje { get; set; } = "";

        public bool Leida { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }

    [Table("TicketSecuencia")]
    public class TicketSecuencia
    {
        [Key]
        public int Anio { get; set; }
        public int Ultimo { get; set; }
    }

    public static class TicketEstados
    {
        public const string Abierto = "ABIERTO";
        public const string EnProceso = "EN_PROCESO";
        public const string PendienteCliente = "PENDIENTE_CLIENTE";
        public const string Resuelto = "RESUELTO";
        public const string Cerrado = "CERRADO";
        public const string Reabierto = "REABIERTO";

        public const string PrioridadBaja = "BAJA";
        public const string PrioridadMedia = "MEDIA";
        public const string PrioridadAlta = "ALTA";
        public const string PrioridadCritica = "CRITICA";
    }
}
