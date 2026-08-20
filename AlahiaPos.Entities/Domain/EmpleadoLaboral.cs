using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    /// <summary>Expediente laboral satélite de EmpleadosP (sin duplicar persona).</summary>
    [Table("EmpleadoLaboral")]
    public class EmpleadoLaboral
    {
        [Key]
        public int IdEmpleadoLaboral { get; set; }

        public int IdEmpresa { get; set; }
        public int IdEmpleados { get; set; }

        [StringLength(40)]
        public string TipoEmpleado { get; set; } = "FIJO";

        /// <summary>Organización: a qué departamento pertenece la persona.</summary>
        public int? IdDepartamento { get; set; }

        /// <summary>El cargo es la fuente de salario, jornada y beneficios.</summary>
        public int? IdCargo { get; set; }

        /// <summary>Obsoleto: el horario vive en el cargo. Se conserva por compatibilidad.</summary>
        public int? IdJornada { get; set; }

        [StringLength(120)]
        public string? Cargo { get; set; }

        [StringLength(120)]
        public string? Departamento { get; set; }

        public DateTime? FechaIngreso { get; set; }

        [StringLength(40)]
        public string EstadoLaboral { get; set; } = "ACTIVO";

        /// <summary>Correo del colaborador para recibo de nómina. Si falta, se usa el del usuario vinculado.</summary>
        [StringLength(150)]
        public string? Correo { get; set; }

        /// <summary>MENSUAL | QUINCENAL | SEMANAL</summary>
        [StringLength(20)]
        public string FrecuenciaPago { get; set; } = "QUINCENAL";

        public decimal SalarioBase { get; set; }

        [StringLength(8)]
        public string Moneda { get; set; } = "DOP";

        public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
    }

    [Table("EmpleadoSalarioHistorial")]
    public class EmpleadoSalarioHistorial
    {
        [Key]
        public int IdSalarioHistorial { get; set; }

        public int IdEmpresa { get; set; }
        public int IdEmpleados { get; set; }

        public decimal SalarioAnterior { get; set; }
        public decimal SalarioNuevo { get; set; }

        [StringLength(20)]
        public string FrecuenciaPago { get; set; } = "QUINCENAL";

        public DateTime VigenteDesde { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        [StringLength(250)]
        public string? Motivo { get; set; }

        public int? IdUsuario { get; set; }
    }

    /// <summary>Catálogo de conceptos configurables (ingresos, beneficios, deducciones).</summary>
    [Table("NominaConcepto")]
    public class NominaConcepto
    {
        [Key]
        public int IdNominaConcepto { get; set; }

        public int IdEmpresa { get; set; }

        [Required, StringLength(80)]
        public string ConceptCode { get; set; } = "";

        [Required, StringLength(120)]
        public string Nombre { get; set; } = "";

        /// <summary>INGRESO | BENEFICIO | DEDUCCION</summary>
        [StringLength(20)]
        public string Categoria { get; set; } = "INGRESO";

        public bool EsLegal { get; set; }
        public bool Activo { get; set; } = true;

        [StringLength(250)]
        public string? Descripcion { get; set; }
    }

    [Table("NominaConceptoAsignacion")]
    public class NominaConceptoAsignacion
    {
        [Key]
        public int IdAsignacion { get; set; }

        public int IdEmpresa { get; set; }
        public int IdEmpleados { get; set; }

        [Required, StringLength(80)]
        public string ConceptCode { get; set; } = "";

        public decimal? MontoFijo { get; set; }
        public decimal? Tasa { get; set; }

        /// <summary>MENSUAL | QUINCENAL | SEMANAL | ANUAL | POR_PERIODO</summary>
        [StringLength(20)]
        public string Periodicidad { get; set; } = "QUINCENAL";

        public DateTime? VigenteDesde { get; set; }
        public DateTime? VigenteHasta { get; set; }

        public bool Activo { get; set; } = true;

        [StringLength(250)]
        public string? Nota { get; set; }
    }
}
