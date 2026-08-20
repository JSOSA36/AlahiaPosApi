using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    /// <summary>Marcación original. No se actualiza ni se borra; las correcciones van en <see cref="RrhhPonchadaCorreccion"/>.</summary>
    [Table("RrhhPonchada")]
    public class RrhhPonchada
    {
        [Key]
        public int IdPonchada { get; set; }
        public int IdEmpresa { get; set; }
        public int IdEmpleados { get; set; }
        public DateTime FechaHora { get; set; }
        [Required, StringLength(20)]
        public string Tipo { get; set; } = RrhhEstados.Entrada;
        [StringLength(20)]
        public string Origen { get; set; } = "WEB";
        [StringLength(120)]
        public string? Dispositivo { get; set; }
        [StringLength(60)]
        public string? Ip { get; set; }
        public int IdUsuarioRegistra { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
        [StringLength(250)]
        public string? Nota { get; set; }
        [StringLength(80)]
        public string? ClaveExterna { get; set; }
    }

    [Table("RrhhPonchadaCorreccion")]
    public class RrhhPonchadaCorreccion
    {
        [Key]
        public int IdCorreccion { get; set; }
        public int IdEmpresa { get; set; }
        public int IdEmpleados { get; set; }
        public int? IdPonchada { get; set; }
        [Required, StringLength(30)]
        public string TipoCorreccion { get; set; } = RrhhEstados.CorrCambioHora;
        [StringLength(20)]
        public string Estado { get; set; } = RrhhEstados.Pendiente;
        public DateTime? FechaHoraOriginal { get; set; }
        [StringLength(20)]
        public string? TipoOriginal { get; set; }
        public DateTime? FechaHoraNueva { get; set; }
        [StringLength(20)]
        public string? TipoNuevo { get; set; }
        [Required, StringLength(400)]
        public string Motivo { get; set; } = "";
        public int IdUsuarioSolicita { get; set; }
        public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
        public int? IdUsuarioAprueba { get; set; }
        public DateTime? FechaDecision { get; set; }
        [StringLength(400)]
        public string? MotivoDecision { get; set; }
    }

    [Table("RrhhAsistenciaDia")]
    public class RrhhAsistenciaDia
    {
        [Key]
        public int IdAsistenciaDia { get; set; }
        public int IdEmpresa { get; set; }
        public int IdEmpleados { get; set; }
        public DateTime Fecha { get; set; }
        public int? IdJornada { get; set; }
        public int? IdTurno { get; set; }
        public TimeSpan? HoraEntradaEsperada { get; set; }
        public TimeSpan? HoraSalidaEsperada { get; set; }
        public DateTime? HoraEntradaReal { get; set; }
        public DateTime? HoraSalidaReal { get; set; }
        public int MinutosTrabajados { get; set; }
        public int MinutosEsperados { get; set; }
        public int MinutosTardanza { get; set; }
        public int MinutosSalidaAnticipada { get; set; }
        public int MinutosExtra { get; set; }
        [Required, StringLength(30)]
        public string Estado { get; set; } = RrhhEstados.Ausente;
        public int? IdSolicitudAusencia { get; set; }
        public bool EsJustificado { get; set; }
        public DateTime CalculadoEn { get; set; } = DateTime.UtcNow;
        [StringLength(400)]
        public string? Observacion { get; set; }
    }

    /// <summary>Embedding facial del colaborador. No se guardan fotos.</summary>
    [Table("RrhhEmpleadoRostro")]
    public class RrhhEmpleadoRostro
    {
        [Key]
        public int IdRostro { get; set; }
        public int IdEmpresa { get; set; }
        public int IdEmpleados { get; set; }
        [Required]
        public string Embedding { get; set; } = "[]";
        public int Muestras { get; set; } = 1;
        public bool Consentimiento { get; set; }
        public DateTime FechaEnrolamiento { get; set; } = DateTime.UtcNow;
        public int IdUsuarioEnrolamiento { get; set; }
        public bool Activo { get; set; } = true;
        public bool PermitirPinExcepcion { get; set; }
        [StringLength(128)]
        public string? PinHash { get; set; }
        [StringLength(64)]
        public string? PinSalt { get; set; }
        public int IntentosPinFallidos { get; set; }
        public DateTime? PinBloqueadoHasta { get; set; }
    }

    [Table("RrhhKioscoEvento")]
    public class RrhhKioscoEvento
    {
        [Key]
        public int IdEvento { get; set; }
        public int IdEmpresa { get; set; }
        public int? IdEmpleados { get; set; }
        [Required, StringLength(30)]
        public string Tipo { get; set; } = "";
        public double? Distancia { get; set; }
        public int IdUsuario { get; set; }
        [StringLength(120)]
        public string? Dispositivo { get; set; }
        [StringLength(60)]
        public string? Ip { get; set; }
        [StringLength(400)]
        public string? Detalle { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }

    [Table("RrhhPonchadorDispositivo")]
    public class RrhhPonchadorDispositivo
    {
        [Key]
        public int IdDispositivo { get; set; }
        public int IdEmpresa { get; set; }
        [Required, StringLength(80)]
        public string Serial { get; set; } = "";
        [Required, StringLength(120)]
        public string Nombre { get; set; } = "";
        [Required, StringLength(30)]
        public string Proveedor { get; set; } = "ZKTECO";
        [StringLength(80)]
        public string? Token { get; set; }
        [StringLength(80)]
        public string? DireccionIp { get; set; }
        public int? Puerto { get; set; }
        [StringLength(40)]
        public string? ClaveComunicacion { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime? UltimaComunicacion { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }

    [Table("RrhhPonchadorPersona")]
    public class RrhhPonchadorPersona
    {
        [Key]
        public int IdPersonaDispositivo { get; set; }
        public int IdEmpresa { get; set; }
        public int IdEmpleados { get; set; }
        [Required, StringLength(40)]
        public string CodigoDispositivo { get; set; } = "";
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }

    [Table("RrhhPonchadorIngesta")]
    public class RrhhPonchadorIngesta
    {
        [Key]
        public int IdIngesta { get; set; }
        public int? IdEmpresa { get; set; }
        public int? IdDispositivo { get; set; }
        [Required, StringLength(80)]
        public string Serial { get; set; } = "";
        [StringLength(40)]
        public string? CodigoDispositivo { get; set; }
        public int? IdEmpleados { get; set; }
        public DateTime? FechaHoraReloj { get; set; }
        [Required, StringLength(30)]
        public string Estado { get; set; } = "";
        [StringLength(80)]
        public string? ClaveExterna { get; set; }
        public int? IdPonchada { get; set; }
        [StringLength(400)]
        public string? Detalle { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }
}
