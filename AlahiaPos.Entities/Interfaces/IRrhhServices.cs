using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IRrhhCatalogoService
    {
        Task<IReadOnlyList<RrhhDepartamento>> GetDepartamentosAsync(int idEmpresa);
        Task<RrhhDepartamento> UpsertDepartamentoAsync(RrhhDepartamento row);
        Task<IReadOnlyList<RrhhCargoDto>> GetCargosAsync(int idEmpresa);
        Task<RrhhCargoDto> UpsertCargoAsync(RrhhCargoDto row, int? idUsuario = null);
        Task<IReadOnlyList<RrhhBeneficio>> GetBeneficiosAsync(int idEmpresa);
        Task<RrhhBeneficio> UpsertBeneficioAsync(RrhhBeneficio row);
        Task<IReadOnlyList<int>> GetCargoIdsConBeneficioAsync(int idBeneficio);
        Task<IReadOnlyList<RrhhJornadaDto>> GetJornadasAsync(int idEmpresa);
        Task<RrhhJornadaDto> UpsertJornadaAsync(RrhhJornadaDto dto);
        Task<IReadOnlyList<RrhhTurnoDto>> GetTurnosAsync(int idEmpresa);
        Task<RrhhTurnoDto> UpsertTurnoAsync(RrhhTurnoDto dto);
        Task<IReadOnlyList<RrhhEmpleadoHorarioDto>> GetHorariosAsync(int idEmpresa, int? idEmpleados = null);
        Task<RrhhEmpleadoHorarioDto> UpsertHorarioAsync(RrhhEmpleadoHorarioDto dto);
        Task<IReadOnlyList<RrhhTipoAusencia>> GetTiposAusenciaAsync(int idEmpresa);
    }

    public interface IRrhhPonchadorService
    {
        Task<RrhhPonchada> PoncharAsync(RrhhPoncharRequest req, int idUsuario, string? ip);
        Task<IReadOnlyList<RrhhPonchadaVistaDto>> GetEfectivasAsync(int idEmpresa, int idEmpleados, DateTime desde, DateTime hasta);
        Task<IReadOnlyList<RrhhPonchada>> GetOriginalesAsync(int idEmpresa, int idEmpleados, DateTime desde, DateTime hasta);
        Task<RrhhPonchadaCorreccion> SolicitarCorreccionAsync(RrhhCorreccionRequest req, int idUsuario);
        Task<IReadOnlyList<RrhhPonchadaCorreccion>> GetCorreccionesAsync(int idEmpresa, string? estado = null);
        Task<RrhhPonchadaCorreccion> DecidirCorreccionAsync(int idCorreccion, bool aprobar, int idUsuario, string? comentario);
        int ResolverIdEmpleados(int idEmpresa, int idUsuario, int idEmpleadosSolicitado);
    }

    public interface IRrhhKioscoService
    {
        Task<IReadOnlyList<RrhhEmpleadoRostroEstadoDto>> ListarRostrosAsync(int idEmpresa);
        Task<RrhhEmpleadoRostroEstadoDto> EnrolarAsync(RrhhEnrolarRostroRequest req, int idUsuario);
        Task<RrhhEmpleadoRostroEstadoDto> CambiarEstadoAsync(RrhhRostroEstadoRequest req, int idUsuario);
        Task<RrhhKioscoPoncharResultDto> PoncharFacialAsync(RrhhKioscoFacialRequest req, int idUsuario, string? ip);
        Task<RrhhKioscoPoncharResultDto> PoncharPinAsync(RrhhKioscoPinRequest req, int idUsuario, string? ip);
        Task<IReadOnlyList<RrhhPonchadaRecienteDto>> RecientesAsync(int idEmpresa, int take = 8);
    }

    public interface IRrhhDispositivoService
    {
        Task<IReadOnlyList<RrhhDispositivoDto>> ListarAsync(int idEmpresa);
        Task<RrhhDispositivoDto> UpsertAsync(RrhhDispositivoDto dto);
        Task<IReadOnlyList<RrhhDispositivoPersonaDto>> ListarPersonasAsync(int idEmpresa);
        Task<RrhhDispositivoPersonaDto> UpsertPersonaAsync(RrhhDispositivoPersonaDto dto);
        Task<IReadOnlyList<RrhhDispositivoIngestaDto>> ListarIngestasAsync(int idEmpresa, int take = 40);
        Task HeartbeatAsync(string serial, string? ip);
        Task<RrhhDispositivoIngestaResumenDto> IngestarAttLogAsync(string serial, string body, string? ip);
        Task<RrhhDispositivoIngestaResumenDto> IngestarJsonAsync(RrhhDispositivoJsonIngestaRequest req, string? ip);
        Task<RrhhDispositivoConexionDto> ProbarConexionAsync(RrhhDispositivoProbarRequest req);
    }

    public interface IRrhhAusenciaService
    {
        Task<RrhhSolicitudAusencia> SolicitarAsync(RrhhSolicitudAusencia row, int idUsuario);
        Task<IReadOnlyList<RrhhSolicitudAusencia>> ListarAsync(int idEmpresa, int? idEmpleados, DateTime? desde, DateTime? hasta, string? estado);
        Task<RrhhSolicitudAusencia> DecidirAsync(int idSolicitud, bool aprobar, int idUsuario, string? comentario);
        Task<IReadOnlyList<RrhhSolicitudAusencia>> GetAprobadasEnRangoAsync(int idEmpresa, int idEmpleados, DateTime desde, DateTime hasta);
        Task<RrhhPrestamo> CrearPrestamoAsync(RrhhPrestamo row, int idUsuario);
        Task<IReadOnlyList<RrhhPrestamo>> GetPrestamosAsync(int idEmpresa, int? idEmpleados);
        Task<RrhhAnticipo> CrearAnticipoAsync(RrhhAnticipo row, int idUsuario);
        Task<IReadOnlyList<RrhhAnticipo>> GetAnticiposAsync(int idEmpresa, int? idEmpleados);
    }

    public interface IRrhhAsistenciaService
    {
        Task<IReadOnlyList<RrhhAsistenciaDiaDto>> CalcularAsync(RrhhCalcularAsistenciaRequest req);
        Task<IReadOnlyList<RrhhAsistenciaDiaDto>> ListarAsync(int idEmpresa, DateTime desde, DateTime hasta, int? idEmpleados);
    }

    public interface INominaProcesoService
    {
        Task<IReadOnlyList<NominaProcesoVistaDto>> ListarAsync(int idEmpresa);
        Task<NominaProcesoVistaDto> GetAsync(int idEmpresa, int id);
        Task<NominaProcesoVistaDto> CrearAsync(NominaProcesoCrearDto dto, int idUsuario);
        Task<NominaProcesoVistaDto> GenerarPrenominaAsync(int idEmpresa, int id, int idUsuario);
        Task<NominaProcesoVistaDto> CambiarEstadoAsync(
            int idEmpresa, int id, string nuevoEstado, int idUsuario, string? comentario, int? idCuentaFinanciera = null);
    }

    public interface INominaReciboEnvioService
    {
        Task<NominaRecibosEnvioResultadoDto> EnviarAsync(int idEmpresa, int idNominaProceso);
    }

    public interface IEmpleadoFichaPersonalService
    {
        Task<EmpleadoFichaPersonalDto> GetAsync(int idEmpresa, int idEmpleados);
        Task<EmpleadoFichaPersonalDto> UpsertAsync(EmpleadoFichaPersonalDto dto, int? idUsuario = null);
    }
}
