using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [ApiController]
    [Route("api/rrhh")]
    public class RrhhOperacionController : ControllerBase
    {
        private readonly IRrhhCatalogoService _catalogos;
        private readonly IRrhhPonchadorService _ponchador;
        private readonly IRrhhKioscoService _kiosco;
        private readonly IRrhhDispositivoService _dispositivos;
        private readonly IRrhhAusenciaService _ausencias;
        private readonly IRrhhAsistenciaService _asistencia;
        private readonly INominaProcesoService _nomina;
        private readonly IEmpleadoLaboralService _laboral;
        private readonly INominaReciboEnvioService _recibos;

        public RrhhOperacionController(
            IRrhhCatalogoService catalogos,
            IRrhhPonchadorService ponchador,
            IRrhhKioscoService kiosco,
            IRrhhDispositivoService dispositivos,
            IRrhhAusenciaService ausencias,
            IRrhhAsistenciaService asistencia,
            INominaProcesoService nomina,
            IEmpleadoLaboralService laboral,
            INominaReciboEnvioService recibos)
        {
            _catalogos = catalogos;
            _ponchador = ponchador;
            _kiosco = kiosco;
            _dispositivos = dispositivos;
            _ausencias = ausencias;
            _asistencia = asistencia;
            _nomina = nomina;
            _laboral = laboral;
            _recibos = recibos;
        }

        private int IdUsuario() =>
            int.TryParse(Request.Headers["X-IdUsuario"].FirstOrDefault(), out var id) ? id : 0;

        private IActionResult Fail(Exception ex) =>
            ex is ArgumentException or InvalidOperationException
                ? BadRequest(new { message = ex.Message })
                : StatusCode(500, new { message = ex.Message });

        [HttpGet("departamentos/{idEmpresa:int}")]
        public async Task<IActionResult> Departamentos(int idEmpresa) =>
            Ok(await _catalogos.GetDepartamentosAsync(idEmpresa));

        [HttpPut("departamentos")]
        public async Task<IActionResult> UpsertDepto([FromBody] RrhhDepartamento body)
        {
            try { return Ok(await _catalogos.UpsertDepartamentoAsync(body)); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("cargos/{idEmpresa:int}")]
        public async Task<IActionResult> Cargos(int idEmpresa) =>
            Ok(await _catalogos.GetCargosAsync(idEmpresa));

        [HttpPut("cargos")]
        public async Task<IActionResult> UpsertCargo([FromBody] RrhhCargoDto body)
        {
            try
            {
                var saved = await _catalogos.UpsertCargoAsync(body, IdUsuario());
                await _laboral.SincronizarEmpleadosDelCargoAsync(saved.IdEmpresa, saved.IdCargo, IdUsuario());
                return Ok(saved);
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("beneficios/{idEmpresa:int}")]
        public async Task<IActionResult> Beneficios(int idEmpresa) =>
            Ok(await _catalogos.GetBeneficiosAsync(idEmpresa));

        [HttpPut("beneficios")]
        public async Task<IActionResult> UpsertBeneficio([FromBody] RrhhBeneficio body)
        {
            try
            {
                var saved = await _catalogos.UpsertBeneficioAsync(body);
                var cargos = await _catalogos.GetCargoIdsConBeneficioAsync(saved.IdBeneficio);
                foreach (var idCargo in cargos)
                    await _laboral.SincronizarEmpleadosDelCargoAsync(saved.IdEmpresa, idCargo, IdUsuario());
                return Ok(saved);
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("jornadas/{idEmpresa:int}")]
        public async Task<IActionResult> Jornadas(int idEmpresa) =>
            Ok(await _catalogos.GetJornadasAsync(idEmpresa));

        [HttpPut("jornadas")]
        public async Task<IActionResult> UpsertJornada([FromBody] RrhhJornadaDto body)
        {
            try { return Ok(await _catalogos.UpsertJornadaAsync(body)); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("turnos/{idEmpresa:int}")]
        public async Task<IActionResult> Turnos(int idEmpresa) =>
            Ok(await _catalogos.GetTurnosAsync(idEmpresa));

        [HttpPut("turnos")]
        public async Task<IActionResult> UpsertTurno([FromBody] RrhhTurnoDto body)
        {
            try { return Ok(await _catalogos.UpsertTurnoAsync(body)); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("horarios/{idEmpresa:int}")]
        public async Task<IActionResult> Horarios(int idEmpresa, [FromQuery] int? idEmpleados) =>
            Ok(await _catalogos.GetHorariosAsync(idEmpresa, idEmpleados));

        [HttpPut("horarios")]
        public async Task<IActionResult> UpsertHorario([FromBody] RrhhEmpleadoHorarioDto body)
        {
            try { return Ok(await _catalogos.UpsertHorarioAsync(body)); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("tipos-ausencia/{idEmpresa:int}")]
        public async Task<IActionResult> TiposAusencia(int idEmpresa) =>
            Ok(await _catalogos.GetTiposAusenciaAsync(idEmpresa));

        [HttpPost("ponchar")]
        public async Task<IActionResult> Ponchar([FromBody] RrhhPoncharRequest body)
        {
            try
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                return Ok(await _ponchador.PoncharAsync(body, IdUsuario(), ip));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("ponchadas/{idEmpresa:int}/{idEmpleados:int}")]
        public async Task<IActionResult> Ponchadas(int idEmpresa, int idEmpleados, [FromQuery] DateTime desde, [FromQuery] DateTime hasta)
        {
            var efectivas = await _ponchador.GetEfectivasAsync(idEmpresa, idEmpleados, desde, hasta);
            var originales = await _ponchador.GetOriginalesAsync(idEmpresa, idEmpleados, desde, hasta);
            return Ok(new { efectivas, originales });
        }

        [HttpGet("mi-empleado/{idEmpresa:int}")]
        public IActionResult MiEmpleado(int idEmpresa)
        {
            var id = _ponchador.ResolverIdEmpleados(idEmpresa, IdUsuario(), 0);
            return Ok(new { idEmpleados = id });
        }

        [HttpGet("kiosco/rostros/{idEmpresa:int}")]
        public async Task<IActionResult> Rostros(int idEmpresa) =>
            Ok(await _kiosco.ListarRostrosAsync(idEmpresa));

        [HttpPost("kiosco/enrolar")]
        public async Task<IActionResult> Enrolar([FromBody] RrhhEnrolarRostroRequest body)
        {
            try { return Ok(await _kiosco.EnrolarAsync(body, IdUsuario())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("kiosco/rostro/estado")]
        public async Task<IActionResult> EstadoRostro([FromBody] RrhhRostroEstadoRequest body)
        {
            try { return Ok(await _kiosco.CambiarEstadoAsync(body, IdUsuario())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("kiosco/ponchar-facial")]
        public async Task<IActionResult> PoncharFacial([FromBody] RrhhKioscoFacialRequest body)
        {
            try
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                return Ok(await _kiosco.PoncharFacialAsync(body, IdUsuario(), ip));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("kiosco/ponchar-pin")]
        public async Task<IActionResult> PoncharPin([FromBody] RrhhKioscoPinRequest body)
        {
            try
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                return Ok(await _kiosco.PoncharPinAsync(body, IdUsuario(), ip));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("kiosco/recientes/{idEmpresa:int}")]
        public async Task<IActionResult> Recientes(int idEmpresa) =>
            Ok(await _kiosco.RecientesAsync(idEmpresa));

        [HttpPost("correcciones")]
        public async Task<IActionResult> SolicitarCorreccion([FromBody] RrhhCorreccionRequest body)
        {
            try { return Ok(await _ponchador.SolicitarCorreccionAsync(body, IdUsuario())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("correcciones/{idEmpresa:int}")]
        public async Task<IActionResult> Correcciones(int idEmpresa, [FromQuery] string? estado) =>
            Ok(await _ponchador.GetCorreccionesAsync(idEmpresa, estado));

        [HttpPost("correcciones/{id:int}/decidir")]
        public async Task<IActionResult> DecidirCorreccion(int id, [FromQuery] bool aprobar, [FromBody] RrhhDecisionRequest? body)
        {
            try { return Ok(await _ponchador.DecidirCorreccionAsync(id, aprobar, IdUsuario(), body?.Comentario)); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("ausencias")]
        public async Task<IActionResult> SolicitarAusencia([FromBody] RrhhSolicitudAusencia body)
        {
            try { return Ok(await _ausencias.SolicitarAsync(body, IdUsuario())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("ausencias/{idEmpresa:int}")]
        public async Task<IActionResult> Ausencias(
            int idEmpresa, [FromQuery] int? idEmpleados, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] string? estado) =>
            Ok(await _ausencias.ListarAsync(idEmpresa, idEmpleados, desde, hasta, estado));

        [HttpPost("ausencias/{id:int}/decidir")]
        public async Task<IActionResult> DecidirAusencia(int id, [FromQuery] bool aprobar, [FromBody] RrhhDecisionRequest? body)
        {
            try { return Ok(await _ausencias.DecidirAsync(id, aprobar, IdUsuario(), body?.Comentario)); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("prestamos/{idEmpresa:int}")]
        public async Task<IActionResult> Prestamos(int idEmpresa, [FromQuery] int? idEmpleados) =>
            Ok(await _ausencias.GetPrestamosAsync(idEmpresa, idEmpleados));

        [HttpPost("prestamos")]
        public async Task<IActionResult> CrearPrestamo([FromBody] RrhhPrestamo body)
        {
            try { return Ok(await _ausencias.CrearPrestamoAsync(body, IdUsuario())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("anticipos/{idEmpresa:int}")]
        public async Task<IActionResult> Anticipos(int idEmpresa, [FromQuery] int? idEmpleados) =>
            Ok(await _ausencias.GetAnticiposAsync(idEmpresa, idEmpleados));

        [HttpPost("anticipos")]
        public async Task<IActionResult> CrearAnticipo([FromBody] RrhhAnticipo body)
        {
            try { return Ok(await _ausencias.CrearAnticipoAsync(body, IdUsuario())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("asistencia/calcular")]
        public async Task<IActionResult> CalcularAsistencia([FromBody] RrhhCalcularAsistenciaRequest body)
        {
            try { return Ok(await _asistencia.CalcularAsync(body)); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("asistencia/{idEmpresa:int}")]
        public async Task<IActionResult> ListarAsistencia(
            int idEmpresa, [FromQuery] DateTime desde, [FromQuery] DateTime hasta, [FromQuery] int? idEmpleados) =>
            Ok(await _asistencia.ListarAsync(idEmpresa, desde, hasta, idEmpleados));

        [HttpGet("nomina/{idEmpresa:int}")]
        public async Task<IActionResult> Nominas(int idEmpresa) =>
            Ok(await _nomina.ListarAsync(idEmpresa));

        [HttpGet("nomina/{idEmpresa:int}/{id:int}")]
        public async Task<IActionResult> Nomina(int idEmpresa, int id)
        {
            try { return Ok(await _nomina.GetAsync(idEmpresa, id)); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("nomina")]
        public async Task<IActionResult> CrearNomina([FromBody] NominaProcesoCrearDto body)
        {
            try { return Ok(await _nomina.CrearAsync(body, IdUsuario())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("nomina/{idEmpresa:int}/{id:int}/generar")]
        public async Task<IActionResult> Generar(int idEmpresa, int id)
        {
            try { return Ok(await _nomina.GenerarPrenominaAsync(idEmpresa, id, IdUsuario())); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("nomina/{idEmpresa:int}/{id:int}/estado")]
        public async Task<IActionResult> EstadoNomina(int idEmpresa, int id, [FromQuery] string estado, [FromBody] RrhhDecisionRequest? body)
        {
            try
            {
                var vista = await _nomina.CambiarEstadoAsync(
                    idEmpresa, id, estado, IdUsuario(), body?.Comentario, body?.IdCuentaFinanciera);
                if (string.Equals(estado, RrhhEstados.NominaPagada, StringComparison.OrdinalIgnoreCase))
                {
                    try { vista.EnvioRecibos = await _recibos.EnviarAsync(idEmpresa, id); }
                    catch (Exception ex)
                    {
                        vista.EnvioRecibos = new NominaRecibosEnvioResultadoDto
                        {
                            Mensaje = "La nómina quedó pagada, pero no se pudieron enviar los recibos: " + ex.Message
                        };
                    }
                }
                return Ok(vista);
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("nomina/{idEmpresa:int}/{id:int}/enviar-recibos")]
        public async Task<IActionResult> EnviarRecibos(int idEmpresa, int id)
        {
            try { return Ok(await _recibos.EnviarAsync(idEmpresa, id)); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("dispositivos/{idEmpresa:int}")]
        public async Task<IActionResult> Dispositivos(int idEmpresa) =>
            Ok(await _dispositivos.ListarAsync(idEmpresa));

        [HttpPut("dispositivos")]
        public async Task<IActionResult> UpsertDispositivo([FromBody] RrhhDispositivoDto body)
        {
            try { return Ok(await _dispositivos.UpsertAsync(body)); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("dispositivos/{idEmpresa:int}/personas")]
        public async Task<IActionResult> DispositivoPersonas(int idEmpresa) =>
            Ok(await _dispositivos.ListarPersonasAsync(idEmpresa));

        [HttpPut("dispositivos/personas")]
        public async Task<IActionResult> UpsertDispositivoPersona([FromBody] RrhhDispositivoPersonaDto body)
        {
            try { return Ok(await _dispositivos.UpsertPersonaAsync(body)); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpGet("dispositivos/{idEmpresa:int}/ingestas")]
        public async Task<IActionResult> DispositivoIngestas(int idEmpresa) =>
            Ok(await _dispositivos.ListarIngestasAsync(idEmpresa));

        [HttpPost("dispositivos/probar")]
        public async Task<IActionResult> ProbarDispositivo([FromBody] RrhhDispositivoProbarRequest body)
        {
            try { return Ok(await _dispositivos.ProbarConexionAsync(body)); }
            catch (Exception ex) { return Fail(ex); }
        }
    }
}
