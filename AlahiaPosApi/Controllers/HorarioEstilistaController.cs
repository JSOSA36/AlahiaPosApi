using AlahiaPos.Entities.Domain;

using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HorarioEstilistaController : ControllerBase
    {
        private readonly IHorariosEstilista _IHorarios;
        private readonly IMapper _Mapper;
        private readonly ICitas _Citas;
        
        public HorarioEstilistaController(IHorariosEstilista iHorarios, IMapper mapper, ICitas citas)
        {
            _IHorarios = iHorarios;
            _Mapper = mapper;
            _Citas = citas;
        }

        // 🔹 GET: api/HorarioEstilista/empleado/5
        [HttpGet("GetByEmpleadoEmpresa/{idEmpleado}/{idEmpresa}")]
        public async Task<IEnumerable<HorariosEstilista>> GetByEmpleadoEmpresa(int idEmpleado, int idEmpresa)
        {

            var Datos = await _IHorarios.GetHorariosByEmpleadoByEmpresa(idEmpleado, idEmpresa);
            return Datos;
        }



        // 🔹 GET: api/HorarioEstilista/empresa/2
        [HttpGet("GetByEmpresa/{idEmpresa}")]
        public async Task<IEnumerable<HorariosEstilista>> GetByEmpresa(int idEmpresa)
        {
            return await _IHorarios.GetHorariosByEmpresa(idEmpresa);
        }

        // 🔹 POST: api/HorarioEstilista
        [HttpPost]
        public async Task<IActionResult> Post([FromForm] HorarioEstilistaDto value)
        {
            try
            {
                var horario = _Mapper.Map<HorariosEstilista>(value);
                await _IHorarios.InsertHorario(horario);

                return Ok(new { message = "✅ Horario registrado correctamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error al crear horario", error = ex.Message });
            }
        }

        // 🔹 PUT: api/HorarioEstilista
        [HttpPut]
        public IActionResult Put([FromForm] HorarioEstilistaDto value)
        {
            var horario = _Mapper.Map<HorariosEstilista>(value);
            _IHorarios.UpdateHorario(horario);

            return Ok(new { message = "✅ Horario actualizado correctamente" });
        }

        // 🔹 DELETE: api/HorarioEstilista/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            _IHorarios.DeleteHorario(id);
            return Ok(new { message = "✅ Horario eliminado correctamente" });
        }

        // 🔹 GET: api/HorarioEstilista/disponibilidad/5/2025-10-07

        [HttpGet("GetDisponibilidad/{idEmpleado}/{idEmpresa}/{fecha}")]
        public async Task<IActionResult> GetDisponibilidad(int idEmpleado, int idEmpresa, DateTime fecha)
        {
            try
            {
                var horarios = await _IHorarios.GetHorariosByEmpleado(idEmpleado);
                var diaSemana = (int)fecha.DayOfWeek;

                var horarioDia = horarios
                    .Where(h => h.DiaSemana == diaSemana && h.IdEmpresa == idEmpresa)
                    .ToList();

                if (!horarioDia.Any())
                    return Ok(new { fecha, horasDisponibles = new List<string>() });

                // 🔒 CITAS YA TOMADAS
                var citas = await _Citas.GetCitasByEmpleado(idEmpleado, idEmpresa);
                var citasDelDia = citas.Where(c => c.Fecha.Date == fecha.Date).ToList();

                var horasBloqueadas = new HashSet<string>();

                foreach (var c in citasDelDia)
                {
                    var actual = c.Hora;
                    var fin = c.HoraFin;

                    while (actual < fin)
                    {
                        horasBloqueadas.Add(actual.ToString(@"hh\:mm"));
                        actual = actual + TimeSpan.FromMinutes(30);
                    }
                }

                // 🟢 GENERAR DISPONIBLES
                var disponibles = new List<string>();

                foreach (var h in horarioDia)
                {
                    var horaActual = h.HoraInicio;

                    while (horaActual < h.HoraFin)
                    {
                        // 🚨 BLOQUEAR HORA DE COMIDA
                        if (h.RecesoInicio.HasValue && h.RecesoFin.HasValue)
                        {
                            if (horaActual >= h.RecesoInicio.Value &&
                                horaActual < h.RecesoFin.Value)
                            {
                                horaActual = horaActual + TimeSpan.FromMinutes(30);
                                continue;
                            }
                        }

                        var horaStr = horaActual.ToString(@"hh\:mm");

                        if (!horasBloqueadas.Contains(horaStr))
                            disponibles.Add(horaStr);

                        horaActual = horaActual + TimeSpan.FromMinutes(30);
                    }
                }

                return Ok(new
                {
                    fecha = fecha.ToString("yyyy-MM-dd"),
                    horasDisponibles = disponibles.OrderBy(h => h)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Error obteniendo disponibilidad",
                    error = ex.Message
                });
            }
        }



        // 🔹 GET: api/HorarioEstilista/Disponible/5/2025-10-07/11:00
        [HttpGet("IsDisponible/{idEmpleado}/{fecha}/{hora}")]
        public async Task<bool> IsDisponible(int idEmpleado, DateTime fecha, string hora)
        {
            if (!TimeSpan.TryParse(hora, out var horaTs))
                return false;

            return await _IHorarios.IsHoraDisponible(idEmpleado, fecha, horaTs);
        }
        [HttpPost("GuardarLista")]
        public async Task<IActionResult> GuardarLista([FromBody] HorarioEstilistaListaDto dto)
        {
            if (dto == null || dto.Horarios == null || !dto.Horarios.Any())
                return BadRequest(new { message = "La lista de horarios está vacía." });

            try
            {
                // 🔹 Tomar referencia de empresa y empleado desde el primer item
                var idEmpleado = dto.Horarios.First().IdEmpleado;
                var idEmpresa = dto.Horarios.First().IdEmpresa;

                // 🔹 Eliminar todos los horarios previos de ese estilista en esa empresa
                var horariosExistentes = await _IHorarios.GetHorariosByEmpleado(idEmpleado);
                foreach (var h in horariosExistentes.Where(h => h.IdEmpresa == idEmpresa))
                {
                    _IHorarios.DeleteHorario(h.IdHorario);
                }

                // 🔹 Insertar todos los nuevos
                foreach (var h in dto.Horarios)
                {
                    var horario = _Mapper.Map<HorariosEstilista>(h);
                    horario.FechaInseccion = DateTime.Now;
                    await _IHorarios.InsertHorario(horario);
                }

                return Ok(new { message = "Horarios actualizados correctamente ✅" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error al guardar horarios", error = ex.Message });
            }
        }



    }
}
