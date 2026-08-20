using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    /// <summary>
    /// Proceso ERP de nómina. El cálculo legal vive en Payroll_Runs (Draft/Approved).
    /// Estados Pagada/Cerrada son de este host, no del motor.
    /// </summary>
    [Table("NominaProceso")]
    public class NominaProceso
    {
        [Key]
        public int IdNominaProceso { get; set; }
        public int IdEmpresa { get; set; }
        [Required, StringLength(40)]
        public string PeriodKey { get; set; } = "";
        [StringLength(40)]
        public string Intent { get; set; } = "REGULAR";
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        [StringLength(20)]
        public string Frecuencia { get; set; } = "QUINCENAL";
        [StringLength(20)]
        public string Estado { get; set; } = RrhhEstados.NominaBorrador;
        public Guid? PayrollRunId { get; set; }
        [StringLength(400)]
        public string? Observacion { get; set; }
        public int IdUsuarioCrea { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public int? IdUsuarioRevision { get; set; }
        public DateTime? FechaRevision { get; set; }
        public int? IdUsuarioAprueba { get; set; }
        public DateTime? FechaAprobacion { get; set; }
        public int? IdUsuarioPaga { get; set; }
        public DateTime? FechaPago { get; set; }
        public int? IdCuentaFinanciera { get; set; }
        public int? IdMovimientoFinanciero { get; set; }
        public int? IdUsuarioCierra { get; set; }
        public DateTime? FechaCierre { get; set; }
        public List<NominaProcesoEmpleado> Empleados { get; set; } = new();
        public List<NominaProcesoEvento> Eventos { get; set; } = new();
    }

    [Table("NominaProcesoEmpleado")]
    public class NominaProcesoEmpleado
    {
        [Key]
        public int IdNominaProcesoEmpleado { get; set; }
        public int IdNominaProceso { get; set; }
        public int IdEmpleados { get; set; }
        [StringLength(200)]
        public string? NombreEmpleado { get; set; }
        public decimal SalarioBase { get; set; }
        public decimal HorasExtra { get; set; }
        public decimal Comisiones { get; set; }
        public decimal Bonificaciones { get; set; }
        public decimal DescuentosAsistencia { get; set; }
        public decimal Prestamos { get; set; }
        public decimal Anticipos { get; set; }
        public decimal OtrosIngresos { get; set; }
        public decimal OtrosDescuentos { get; set; }
        public decimal DeduccionesLegales { get; set; }
        public decimal Bruto { get; set; }
        public decimal Neto { get; set; }
        public decimal DiasAusenteSinGoce { get; set; }
        public int MinutosExtra { get; set; }
        public string? LineasJson { get; set; }
        [ForeignKey(nameof(IdNominaProceso))]
        public NominaProceso? Proceso { get; set; }
    }

    [Table("NominaProcesoEvento")]
    public class NominaProcesoEvento
    {
        [Key]
        public int IdEvento { get; set; }
        public int IdNominaProceso { get; set; }
        [StringLength(20)]
        public string? EstadoAnterior { get; set; }
        [Required, StringLength(20)]
        public string EstadoNuevo { get; set; } = "";
        public int IdUsuario { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        [StringLength(400)]
        public string? Comentario { get; set; }
    }
}
