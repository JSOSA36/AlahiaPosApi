using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("RrhhTipoAusencia")]
    public class RrhhTipoAusencia
    {
        [Key]
        public int IdTipoAusencia { get; set; }
        public int IdEmpresa { get; set; }
        [Required, StringLength(40)]
        public string Codigo { get; set; } = "";
        [Required, StringLength(120)]
        public string Nombre { get; set; } = "";
        [Required, StringLength(20)]
        public string Categoria { get; set; } = "PERMISO";
        [StringLength(10)]
        public string UnidadDefault { get; set; } = "DIAS";
        public bool ConGoceSueldo { get; set; } = true;
        public bool RequiereAprobacion { get; set; } = true;
        public bool AfectaAsistencia { get; set; } = true;
        public bool Activo { get; set; } = true;
    }

    [Table("RrhhSolicitudAusencia")]
    public class RrhhSolicitudAusencia
    {
        [Key]
        public int IdSolicitud { get; set; }
        public int IdEmpresa { get; set; }
        public int IdEmpleados { get; set; }
        public int IdTipoAusencia { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public TimeSpan? HoraInicio { get; set; }
        public TimeSpan? HoraFin { get; set; }
        [Required, StringLength(10)]
        public string Unidad { get; set; } = "DIAS";
        public decimal Cantidad { get; set; }
        public bool ConGoceSueldo { get; set; }
        [StringLength(20)]
        public string Estado { get; set; } = RrhhEstados.Pendiente;
        [StringLength(400)]
        public string? Motivo { get; set; }
        public int IdUsuarioSolicita { get; set; }
        public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
        public int? IdUsuarioAprueba { get; set; }
        public DateTime? FechaDecision { get; set; }
        [StringLength(400)]
        public string? ComentarioDecision { get; set; }
    }

    [Table("RrhhPrestamo")]
    public class RrhhPrestamo
    {
        [Key]
        public int IdPrestamo { get; set; }
        public int IdEmpresa { get; set; }
        public int IdEmpleados { get; set; }
        public decimal Monto { get; set; }
        public decimal Saldo { get; set; }
        public int Cuotas { get; set; }
        public decimal MontoCuota { get; set; }
        public DateTime FechaInicio { get; set; }
        [StringLength(20)]
        public string Estado { get; set; } = "ACTIVO";
        [StringLength(250)]
        public string? Motivo { get; set; }
        public int IdUsuarioCrea { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public List<RrhhPrestamoCuota> CuotasDetalle { get; set; } = new();
    }

    [Table("RrhhPrestamoCuota")]
    public class RrhhPrestamoCuota
    {
        [Key]
        public int IdCuota { get; set; }
        public int IdPrestamo { get; set; }
        public int Numero { get; set; }
        public DateTime FechaProgramada { get; set; }
        public decimal Monto { get; set; }
        [StringLength(40)]
        public string? PeriodKey { get; set; }
        [StringLength(20)]
        public string Estado { get; set; } = "PENDIENTE";
        [ForeignKey(nameof(IdPrestamo))]
        public RrhhPrestamo? Prestamo { get; set; }
    }

    [Table("RrhhAnticipo")]
    public class RrhhAnticipo
    {
        [Key]
        public int IdAnticipo { get; set; }
        public int IdEmpresa { get; set; }
        public int IdEmpleados { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; }
        [StringLength(40)]
        public string? PeriodKey { get; set; }
        [StringLength(20)]
        public string Estado { get; set; } = "PENDIENTE";
        [StringLength(250)]
        public string? Motivo { get; set; }
        public int IdUsuarioCrea { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
