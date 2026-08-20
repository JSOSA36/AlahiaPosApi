using AlahiaPos.Entities.Domain;

namespace AlahiaPos.Entities.Dto
{
    public class RrhhCargoDto
    {
        public int IdCargo { get; set; }
        public int IdEmpresa { get; set; }
        public int? IdJornada { get; set; }
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string? Descripcion { get; set; }
        public decimal SalarioBase { get; set; }
        public string Moneda { get; set; } = "DOP";
        public string FrecuenciaPago { get; set; } = "QUINCENAL";
        public string TipoEmpleado { get; set; } = "FIJO";
        public bool Activo { get; set; } = true;
        public string? NombreJornada { get; set; }
        public string? ResumenHorario { get; set; }
        public List<RrhhJornadaDiaDto> Dias { get; set; } = new();
        public int MinutosTardanzaGracia { get; set; } = 10;
        public List<int> IdBeneficios { get; set; } = new();
        public List<RrhhBeneficio> Beneficios { get; set; } = new();
    }

    public class EmpleadoLaboralVistaDto
    {
        public int IdEmpleadoLaboral { get; set; }
        public int IdEmpresa { get; set; }
        public int IdEmpleados { get; set; }
        public string TipoEmpleado { get; set; } = "FIJO";
        public int? IdDepartamento { get; set; }
        public int? IdCargo { get; set; }
        public DateTime? FechaIngreso { get; set; }
        public string EstadoLaboral { get; set; } = "ACTIVO";
        public string? Correo { get; set; }
        public string? Departamento { get; set; }
        public string? Cargo { get; set; }
        public RrhhCargoPaqueteDto? PaqueteCargo { get; set; }
    }

    public class RrhhCargoPaqueteDto
    {
        public int IdCargo { get; set; }
        public string Nombre { get; set; } = "";
        public decimal SalarioBase { get; set; }
        public string Moneda { get; set; } = "DOP";
        public string FrecuenciaPago { get; set; } = "QUINCENAL";
        public string TipoEmpleado { get; set; } = "FIJO";
        public int? IdJornada { get; set; }
        public string? NombreJornada { get; set; }
        public string? ResumenHorario { get; set; }
        public List<RrhhJornadaDiaDto> Dias { get; set; } = new();
        public List<RrhhBeneficio> Beneficios { get; set; } = new();
    }

    public class RrhhJornadaDto
    {
        public int IdJornada { get; set; }
        public int IdEmpresa { get; set; }
        public string Nombre { get; set; } = "";
        public decimal HorasSemanales { get; set; } = 44;
        public int MinutosTardanzaGracia { get; set; } = 10;
        public bool Activo { get; set; } = true;
        public List<RrhhJornadaDiaDto> Dias { get; set; } = new();
    }

    public class RrhhJornadaDiaDto
    {
        public int IdJornadaDia { get; set; }
        public byte DiaSemana { get; set; }
        public bool EsLaborable { get; set; } = true;
        public string? HoraEntrada { get; set; }
        public string? HoraSalida { get; set; }
        public string? RecesoInicio { get; set; }
        public string? RecesoFin { get; set; }
        public int MinutosEsperados { get; set; }
    }

    public class RrhhTurnoDto
    {
        public int IdTurno { get; set; }
        public int IdEmpresa { get; set; }
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string HoraEntrada { get; set; } = "08:00";
        public string HoraSalida { get; set; } = "17:00";
        public string? RecesoInicio { get; set; }
        public string? RecesoFin { get; set; }
        public bool Activo { get; set; } = true;
    }

    public class RrhhEmpleadoHorarioDto
    {
        public int IdEmpleadoHorario { get; set; }
        public int IdEmpresa { get; set; }
        public int IdEmpleados { get; set; }
        public int IdJornada { get; set; }
        public int? IdTurno { get; set; }
        public DateTime VigenteDesde { get; set; }
        public DateTime? VigenteHasta { get; set; }
        public bool Activo { get; set; } = true;
        public string? NombreJornada { get; set; }
        public string? NombreTurno { get; set; }
        public string? NombreEmpleado { get; set; }
    }

    public class RrhhPoncharRequest
    {
        public int IdEmpresa { get; set; }
        public int IdEmpleados { get; set; }
        public string? Tipo { get; set; }
        public string Origen { get; set; } = "WEB";
        public string? Dispositivo { get; set; }
        public string? Nota { get; set; }
        public DateTime? FechaHora { get; set; }
        public string? ClaveExterna { get; set; }
    }

    public class RrhhPonchadaVistaDto
    {
        public int? IdPonchada { get; set; }
        public int IdEmpleados { get; set; }
        public DateTime FechaHora { get; set; }
        public string Tipo { get; set; } = "";
        public string Origen { get; set; } = "WEB";
        public bool EsCorreccion { get; set; }
        public bool Anulada { get; set; }
        public string? Nota { get; set; }
    }

    public class RrhhCorreccionRequest
    {
        public int IdEmpresa { get; set; }
        public int IdEmpleados { get; set; }
        public int? IdPonchada { get; set; }
        public string TipoCorreccion { get; set; } = RrhhEstados.CorrCambioHora;
        public DateTime? FechaHoraNueva { get; set; }
        public string? TipoNuevo { get; set; }
        public string Motivo { get; set; } = "";
    }

    public class RrhhDecisionRequest
    {
        public string? Comentario { get; set; }
        public int? IdCuentaFinanciera { get; set; }
    }

    public class RrhhCalcularAsistenciaRequest
    {
        public int IdEmpresa { get; set; }
        public int? IdEmpleados { get; set; }
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
    }

    public class RrhhAsistenciaDiaDto
    {
        public int IdAsistenciaDia { get; set; }
        public int IdEmpleados { get; set; }
        public string? NombreEmpleado { get; set; }
        public DateTime Fecha { get; set; }
        public string Estado { get; set; } = "";
        public string? HoraEntradaEsperada { get; set; }
        public string? HoraSalidaEsperada { get; set; }
        public DateTime? HoraEntradaReal { get; set; }
        public DateTime? HoraSalidaReal { get; set; }
        public int MinutosTrabajados { get; set; }
        public int MinutosEsperados { get; set; }
        public int MinutosTardanza { get; set; }
        public int MinutosSalidaAnticipada { get; set; }
        public int MinutosExtra { get; set; }
        public bool EsJustificado { get; set; }
        public int? IdSolicitudAusencia { get; set; }
        public string? Observacion { get; set; }
    }

    public class NominaProcesoCrearDto
    {
        public int IdEmpresa { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Frecuencia { get; set; } = "QUINCENAL";
        public string? Observacion { get; set; }
        public string Intent { get; set; } = "REGULAR";
    }

    public class NominaProcesoVistaDto
    {
        public int IdNominaProceso { get; set; }
        public int IdEmpresa { get; set; }
        public string PeriodKey { get; set; } = "";
        public string Intent { get; set; } = "REGULAR";
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Frecuencia { get; set; } = "";
        public string Estado { get; set; } = "";
        public Guid? PayrollRunId { get; set; }
        public string? Observacion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public int Empleados { get; set; }
        public decimal TotalBruto { get; set; }
        public decimal TotalNeto { get; set; }
        public int? IdCuentaFinanciera { get; set; }
        public int? IdMovimientoFinanciero { get; set; }
        public List<NominaProcesoEmpleado>? Detalle { get; set; }
        public List<NominaProcesoEvento>? Eventos { get; set; }
        public NominaRecibosEnvioResultadoDto? EnvioRecibos { get; set; }
        public string? AdvertenciaContabilidad { get; set; }
    }

    public class NominaRecibosEnvioResultadoDto
    {
        public int Enviados { get; set; }
        public int SinCorreo { get; set; }
        public int Errores { get; set; }
        public string? Mensaje { get; set; }
        public List<NominaReciboEnvioItemDto> Detalle { get; set; } = new();
    }

    public class NominaReciboEnvioItemDto
    {
        public int IdEmpleados { get; set; }
        public string? Nombre { get; set; }
        public string? Correo { get; set; }
        public string Estado { get; set; } = "";
        public string? Error { get; set; }
    }

    public class ConsumoColaboradorDto
    {
        public int IdEmpleados { get; set; }
        public string Nombre { get; set; } = "";
        public decimal Porcentaje { get; set; }
        public bool DescontarNomina { get; set; }
    }

    public class RrhhEmpleadoRostroEstadoDto
    {
        public int IdEmpleados { get; set; }
        public string Nombre { get; set; } = "";
        public bool Enrolado { get; set; }
        public bool Activo { get; set; }
        public bool PermitirPinExcepcion { get; set; }
        public bool TienePin { get; set; }
        public DateTime? FechaEnrolamiento { get; set; }
        public int Muestras { get; set; }
    }

    public class RrhhEnrolarRostroRequest
    {
        public int IdEmpresa { get; set; }
        public int IdEmpleados { get; set; }
        public List<double[]> Embeddings { get; set; } = new();
        public bool Consentimiento { get; set; }
        public bool PermitirPinExcepcion { get; set; }
        public string? Pin { get; set; }
    }

    public class RrhhRostroEstadoRequest
    {
        public int IdEmpresa { get; set; }
        public int IdEmpleados { get; set; }
        public bool Activo { get; set; }
    }

    public class RrhhKioscoFacialRequest
    {
        public int IdEmpresa { get; set; }
        public double[] Embedding { get; set; } = Array.Empty<double>();
        public bool LivenessOk { get; set; }
        public string? Dispositivo { get; set; }
    }

    public class RrhhKioscoPinRequest
    {
        public int IdEmpresa { get; set; }
        public int IdEmpleados { get; set; }
        public string Pin { get; set; } = "";
        public string Motivo { get; set; } = "";
        public string? Dispositivo { get; set; }
    }

    public class RrhhKioscoPoncharResultDto
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
        public int IdEmpleados { get; set; }
        public string Nombre { get; set; } = "";
        public string Tipo { get; set; } = "";
        public DateTime FechaHora { get; set; }
        public double Distancia { get; set; }
        public bool Duplicada { get; set; }
        public string Origen { get; set; } = "";
    }

    public class RrhhPonchadaRecienteDto
    {
        public int IdPonchada { get; set; }
        public int IdEmpleados { get; set; }
        public string Nombre { get; set; } = "";
        public DateTime FechaHora { get; set; }
        public string Tipo { get; set; } = "";
        public string Origen { get; set; } = "";
    }

    public class RrhhDispositivoDto
    {
        public int IdDispositivo { get; set; }
        public int IdEmpresa { get; set; }
        public string Serial { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Proveedor { get; set; } = "ZKTECO";
        public string? Token { get; set; }
        public string? DireccionIp { get; set; }
        public int? Puerto { get; set; }
        public string? ClaveComunicacion { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime? UltimaComunicacion { get; set; }
    }

    public class RrhhDispositivoPersonaDto
    {
        public int IdPersonaDispositivo { get; set; }
        public int IdEmpresa { get; set; }
        public int IdEmpleados { get; set; }
        public string NombreEmpleado { get; set; } = "";
        public string CodigoDispositivo { get; set; } = "";
        public bool Activo { get; set; } = true;
    }

    public class RrhhDispositivoIngestaDto
    {
        public int IdIngesta { get; set; }
        public string Serial { get; set; } = "";
        public string? CodigoDispositivo { get; set; }
        public int? IdEmpleados { get; set; }
        public string? NombreEmpleado { get; set; }
        public DateTime? FechaHoraReloj { get; set; }
        public string Estado { get; set; } = "";
        public string? Detalle { get; set; }
        public DateTime Fecha { get; set; }
    }

    public class RrhhDispositivoProbarRequest
    {
        public string? DireccionIp { get; set; }
        public int? Puerto { get; set; }
        public int? IdDispositivo { get; set; }
        public int IdEmpresa { get; set; }
    }

    public class RrhhDispositivoConexionDto
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
        public string? DireccionIp { get; set; }
        public int Puerto { get; set; }
        public int TiempoMs { get; set; }
    }

    public class RrhhDispositivoJsonIngestaRequest
    {
        public string Serial { get; set; } = "";
        public string? Token { get; set; }
        public List<RrhhDispositivoJsonMarcacion> Marcaciones { get; set; } = new();
    }

    public class RrhhDispositivoJsonMarcacion
    {
        public string Codigo { get; set; } = "";
        public DateTime FechaHora { get; set; }
        public string? Tipo { get; set; }
        public int? Status { get; set; }
    }

    public class RrhhDispositivoIngestaResumenDto
    {
        public int Recibidas { get; set; }
        public int Integradas { get; set; }
        public int Duplicadas { get; set; }
        public int SinMapa { get; set; }
        public int Ignoradas { get; set; }
    }
}
