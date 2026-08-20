using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    public static class RrhhEstados
    {
        public const string Pendiente = "PENDIENTE";
        public const string Aprobada = "APROBADA";
        public const string Rechazada = "RECHAZADA";
        public const string Cancelada = "CANCELADA";

        public const string Entrada = "ENTRADA";
        public const string Salida = "SALIDA";
        public const string SalidaReceso = "SALIDA_RECESO";
        public const string RetornoReceso = "RETORNO_RECESO";

        public const string CorrCambioHora = "CAMBIO_HORA";
        public const string CorrCambioTipo = "CAMBIO_TIPO";
        public const string CorrAnulacion = "ANULACION";
        public const string CorrInsercion = "INSERCION_OMITIDA";

        public const string Presente = "PRESENTE";
        public const string Ausente = "AUSENTE";
        public const string Permiso = "PERMISO";
        public const string Vacaciones = "VACACIONES";
        public const string Licencia = "LICENCIA";
        public const string Descanso = "DESCANSO";
        public const string Incompleto = "INCOMPLETO";

        public const string NominaBorrador = "BORRADOR";
        public const string NominaEnRevision = "EN_REVISION";
        public const string NominaAprobada = "APROBADA";
        public const string NominaPagada = "PAGADA";
        public const string NominaCerrada = "CERRADA";

        public static bool NominaBloqueada(string? estado) =>
            estado is NominaAprobada or NominaPagada or NominaCerrada;
    }

    [Table("RrhhDepartamento")]
    public class RrhhDepartamento
    {
        [Key]
        public int IdDepartamento { get; set; }
        public int IdEmpresa { get; set; }
        [Required, StringLength(40)]
        public string Codigo { get; set; } = "";
        [Required, StringLength(120)]
        public string Nombre { get; set; } = "";
        [StringLength(400)]
        public string? Descripcion { get; set; }
        public int? IdResponsable { get; set; }
        [StringLength(200)]
        public string? Ubicacion { get; set; }
        [StringLength(40)]
        public string? Telefono { get; set; }
        [StringLength(120)]
        public string? Email { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }

    [Table("RrhhCargo")]
    public class RrhhCargo
    {
        [Key]
        public int IdCargo { get; set; }
        public int IdEmpresa { get; set; }
        public int? IdDepartamento { get; set; }
        public int? IdJornada { get; set; }
        [Required, StringLength(40)]
        public string Codigo { get; set; } = "";
        [Required, StringLength(120)]
        public string Nombre { get; set; } = "";
        [StringLength(400)]
        public string? Descripcion { get; set; }
        public decimal SalarioBase { get; set; }
        [StringLength(8)]
        public string Moneda { get; set; } = "DOP";
        [StringLength(20)]
        public string FrecuenciaPago { get; set; } = "QUINCENAL";
        [StringLength(40)]
        public string TipoEmpleado { get; set; } = "FIJO";
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public List<RrhhCargoBeneficio> Beneficios { get; set; } = new();
    }

    [Table("RrhhBeneficio")]
    public class RrhhBeneficio
    {
        [Key]
        public int IdBeneficio { get; set; }
        public int IdEmpresa { get; set; }
        [Required, StringLength(40)]
        public string Codigo { get; set; } = "";
        [Required, StringLength(120)]
        public string Nombre { get; set; } = "";
        [StringLength(400)]
        public string? Descripcion { get; set; }
        /// <summary>MONTO_FIJO | PORCENTAJE_SALARIO | DESCUENTO_CONSUMO</summary>
        [StringLength(30)]
        public string TipoCalculo { get; set; } = "MONTO_FIJO";
        public decimal Monto { get; set; }
        [StringLength(20)]
        public string Periodicidad { get; set; } = "MENSUAL";
        public bool AfectaNomina { get; set; } = true;
        /// <summary>false = en dinero; true = servicios (comida, seguro, descuento de colaborador, etc.).</summary>
        public bool EnEspecie { get; set; }
        /// <summary>NOMINA | PAGO_APARTE | NINGUNO</summary>
        [StringLength(20)]
        public string FormaDesembolso { get; set; } = RrhhBeneficioFormas.Nomina;
        /// <summary>Día 1-28 cuando FormaDesembolso = PAGO_APARTE.</summary>
        public byte? DiaPagoMes { get; set; }
        /// <summary>EFECTIVO | TRANSFERENCIA cuando se paga aparte.</summary>
        [StringLength(30)]
        public string? MetodoPago { get; set; }
        /// <summary>
        /// Si TipoCalculo = DESCUENTO_CONSUMO: el consumo (ya con descuento) se descuenta en la quincena.
        /// </summary>
        public bool DescontarConsumoNomina { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }

    public static class RrhhBeneficioFormas
    {
        public const string Nomina = "NOMINA";
        public const string PagoAparte = "PAGO_APARTE";
        public const string Ninguno = "NINGUNO";
    }

    [Table("RrhhCargoBeneficio")]
    public class RrhhCargoBeneficio
    {
        [Key]
        public int IdCargoBeneficio { get; set; }
        public int IdCargo { get; set; }
        public int IdBeneficio { get; set; }
        [ForeignKey(nameof(IdCargo))]
        public RrhhCargo? Cargo { get; set; }
        [ForeignKey(nameof(IdBeneficio))]
        public RrhhBeneficio? Beneficio { get; set; }
    }

    [Table("RrhhCargoSalarioHistorial")]
    public class RrhhCargoSalarioHistorial
    {
        [Key]
        public int IdCargoSalarioHistorial { get; set; }
        public int IdCargo { get; set; }
        public int IdEmpresa { get; set; }
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

    [Table("RrhhJornada")]
    public class RrhhJornada
    {
        [Key]
        public int IdJornada { get; set; }
        public int IdEmpresa { get; set; }
        [Required, StringLength(120)]
        public string Nombre { get; set; } = "";
        public decimal HorasSemanales { get; set; } = 44;
        public int MinutosTardanzaGracia { get; set; } = 10;
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public List<RrhhJornadaDia> Dias { get; set; } = new();
    }

    [Table("RrhhJornadaDia")]
    public class RrhhJornadaDia
    {
        [Key]
        public int IdJornadaDia { get; set; }
        public int IdJornada { get; set; }
        public byte DiaSemana { get; set; }
        public bool EsLaborable { get; set; } = true;
        public TimeSpan? HoraEntrada { get; set; }
        public TimeSpan? HoraSalida { get; set; }
        public TimeSpan? RecesoInicio { get; set; }
        public TimeSpan? RecesoFin { get; set; }
        public int MinutosEsperados { get; set; }
        [ForeignKey(nameof(IdJornada))]
        public RrhhJornada? Jornada { get; set; }
    }

    [Table("RrhhTurno")]
    public class RrhhTurno
    {
        [Key]
        public int IdTurno { get; set; }
        public int IdEmpresa { get; set; }
        [Required, StringLength(40)]
        public string Codigo { get; set; } = "";
        [Required, StringLength(120)]
        public string Nombre { get; set; } = "";
        public TimeSpan HoraEntrada { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public TimeSpan? RecesoInicio { get; set; }
        public TimeSpan? RecesoFin { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }

    [Table("RrhhEmpleadoHorario")]
    public class RrhhEmpleadoHorario
    {
        [Key]
        public int IdEmpleadoHorario { get; set; }
        public int IdEmpresa { get; set; }
        public int IdEmpleados { get; set; }
        public int IdJornada { get; set; }
        public int? IdTurno { get; set; }
        public DateTime VigenteDesde { get; set; }
        public DateTime? VigenteHasta { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
