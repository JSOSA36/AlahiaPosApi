using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    public static class EmpleadoFamiliarTipos
    {
        public const string Conyuge = "CONYUGE";
        public const string Hijo = "HIJO";
        public const string Emergencia = "EMERGENCIA";
        public const string Otro = "OTRO";
    }

    /// <summary>Ficha personal 1:1 de EmpleadosP. No duplica cédula ni el puesto de nómina.</summary>
    [Table("EmpleadoFichaPersonal")]
    public class EmpleadoFichaPersonal
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdFichaPersonal { get; set; }

        public int IdEmpresa { get; set; }
        public int IdEmpleados { get; set; }

        [Column(TypeName = "date")]
        public DateTime? FechaNacimiento { get; set; }

        [StringLength(8)]
        public string? Sexo { get; set; }

        [StringLength(80)]
        public string? Nacionalidad { get; set; }

        [StringLength(40)]
        public string? EstadoCivil { get; set; }

        [StringLength(120)]
        public string? Profesion { get; set; }

        [StringLength(30)]
        public string? Nss { get; set; }

        [Column(TypeName = "date")]
        public DateTime? FechaSalida { get; set; }

        [StringLength(500)]
        public string? Alergias { get; set; }

        [StringLength(8)]
        public string? TipoSangre { get; set; }

        [StringLength(500)]
        public string? ObservacionesMedicas { get; set; }

        public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
        public int? IdUsuario { get; set; }
    }

    [Table("EmpleadoFamiliar")]
    public class EmpleadoFamiliar
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdFamiliar { get; set; }

        public int IdEmpresa { get; set; }
        public int IdEmpleados { get; set; }

        /// <summary>CONYUGE | HIJO | EMERGENCIA | OTRO</summary>
        [Required, StringLength(20)]
        public string Tipo { get; set; } = EmpleadoFamiliarTipos.Otro;

        [Required, StringLength(150)]
        public string Nombre { get; set; } = "";

        [StringLength(20)]
        public string? Cedula { get; set; }

        [Column(TypeName = "date")]
        public DateTime? FechaNacimiento { get; set; }

        [StringLength(8)]
        public string? Sexo { get; set; }

        [StringLength(40)]
        public string? Parentesco { get; set; }

        [StringLength(30)]
        public string? Telefono { get; set; }

        [StringLength(30)]
        public string? Celular { get; set; }

        [StringLength(250)]
        public string? Direccion { get; set; }

        [StringLength(120)]
        public string? Ocupacion { get; set; }

        public bool ViveConEmpleado { get; set; }
        public bool EsDependiente { get; set; }

        [StringLength(250)]
        public string? Nota { get; set; }

        public int Orden { get; set; }
    }
}
