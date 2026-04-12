using AlahiaPos.DataAccess.Servicios;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Enum;
using AlahiaPos.Entities.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using PrinterLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twilio;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Twilio.Types;


namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CitasController : ControllerBase
    {
        private readonly ICitas _ICitas;
        private readonly IMapper _Mapper;
        private readonly IHorariosEstilista _HorariosEstilista;
        private readonly IEmpresas _Empresas;
        private readonly IClientes _Clientes;
        private readonly INotification notification;
        private readonly IProductos _products;
        private readonly TwilioService _twilio;
        public CitasController(
            ICitas iCitas,
            IMapper mapper,
            IHorariosEstilista horariosEstilista,
            IEmpresas empresas,
            INotification notificacionService,
            IProductos products,
            IClientes clientes,
            TwilioService twilio)
        {
            _ICitas = iCitas;
            _Mapper = mapper;
            _HorariosEstilista = horariosEstilista;
            _Empresas = empresas;
            notification = notificacionService;
            _products = products;
            _Clientes = clientes;
            _twilio = twilio;
        }

        // ============================================================
        // ✅ Citas del día con estilista
        // ============================================================
        [HttpGet("GetCitasConEmpleado/{idEmpresa}")]
        public async Task<IActionResult> GetCitasConEmpleado(int idEmpresa)
        {
            try
            {
                var citas = await _ICitas.GetCitasConEmpleado(idEmpresa);

                if (citas == null || !citas.Any())
                {
                    return Ok(new
                    {
                        message = "No hay citas programadas para hoy.",
                        citas = new List<CitaDto>()
                    });
                }

                return Ok(citas);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Error al obtener las citas",
                    error = ex.Message
                });
            }
        }

        // GET: api/citas/IdEmpresa
        [HttpGet("{IdEmpresa}")]
        public async Task<IActionResult> Get(int IdEmpresa)
        {
            var data = await _ICitas.GetAllCitas(IdEmpresa);
            return Ok(data);   // Ahora sí: IActionResult permite Ok()
        }

        [HttpGet("probar-whatsapp")]
        public IActionResult ProbarWhatsApp()
        {
            var accountSid = "AC4ba7e334fedf5097df85197274cdcb9f";
            var authToken = "af1d6b392e24c5f585ef684d4e876916";

            TwilioClient.Init(accountSid, authToken);

            var message = MessageResource.Create(
            body: @"📅 *Nueva cita recibida*

           👤 Cliente: María López
           💇 Servicio: Uñas Acrílicas
           🧑‍🎨 Estilista: Ana
           📆 Fecha: 18/02/2026
           ⏰ Hora: 2:30 PM

            ⚠️ Esta cita está pendiente de confirmación.
            Por favor ingrese al sistema para aprobar o reprogramar.",
             from: new PhoneNumber("whatsapp:+14155238886"),
                to: new PhoneNumber("whatsapp:+18495787517")
            );

            return Ok("Mensaje enviado por WhatsApp");
        }

        // GET api/citas/GetById/5
        [HttpGet("GetById/{id}")]
        public async Task<Cita> GetById(int id)
        {
            return await _ICitas.GetCitaById(id);
        }

        // GET api/citas/GetByEmpleado/5/1
        [HttpGet("GetByEmpleado/{idEmpleado}/{idEmpresa}")]
        public async Task<IEnumerable<Cita>> GetByEmpleado(int idEmpleado, int idEmpresa)
        {
            return await _ICitas.GetCitasByEmpleado(idEmpleado, idEmpresa);
        }

        // ============================================================
        // POST api/citas → Crear cita con duración y bloqueo
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Post([FromForm] CitaDto value)
        {
            try
            {

                if(value.esSeguimiento==null)
                {
                    value.esSeguimiento = false;
                }
                var cita = _Mapper.Map<Cita>(value);

                // ================================
                // 👤 CLIENTE
                // ================================
                if (value.IdCliente == 0 || value.IdCliente == null)
                {
                    Clientes clientes = new Clientes();
                    clientes.NombreComercial = value.NombreCliente;
                    clientes.Telefono = value.Telefono;
                    clientes.Email = value.Correo;
                    clientes.Estado = true;
                    clientes.FechaInseccion = DateTime.Now;
                    clientes.IdEmpresa = value.IdEmpresa;
                    clientes.LimiteCredito = 0;

                    await _Clientes.InsertClientes(clientes);
                    cita.IdCliente = clientes.IDCliente;
                }
                else
                {
                    cita.IdCliente = value.IdCliente;
                }

                // ================================
                // ⏰ HORA
                // ================================
                cita.Hora = value.Hora;

                // ================================
                // 🔵 SEGUIMIENTO
                // ================================
                cita.esSeguimiento = value.esSeguimiento;

                if (value.esSeguimiento != true)
                {
                    // 🔹 SOLO SI NO ES SEGUIMIENTO
                    cita.Abono = value.Abono ?? 0;
                    cita.Banco = value.Banco;
                }
                else
                {
                    // 🔥 LIMPIAR SI ES SEGUIMIENTO
                    cita.Abono = 0;
                    cita.Banco = null;
                    cita.RutaReciboPago = null;
                }

                // ================================
                // ⏱ DURACIÓN
                // ================================
                cita.DuracionMinutos = value.DuracionMinutos > 0
                    ? value.DuracionMinutos
                    : 60;

                cita.HoraFin = value.HoraFin != TimeSpan.Zero
                    ? value.HoraFin
                    : cita.Hora.Add(TimeSpan.FromMinutes(cita.DuracionMinutos));

                cita.IdProducto = value.IdProducto;
                cita.FechaInseccion = DateTime.Now;

                // ================================
                // 🕒 VALIDAR HORARIO
                // ================================
                var horarios = await _HorariosEstilista.GetHorariosByEmpleado(cita.IdEmpleado);
                var diaSemana = (int)cita.Fecha.DayOfWeek;

                var horarioValido = horarios.Any(h =>
                    h.DiaSemana == diaSemana &&
                    cita.Hora >= h.HoraInicio &&
                    cita.HoraFin <= h.HoraFin
                );

                if (!horarioValido)
                    return BadRequest(new { message = "El estilista no trabaja en este rango horario." });

                // ================================
                // ❌ VALIDAR CRUCE
                // ================================
                var citasEmpleado = await _ICitas.GetCitasByEmpleado(cita.IdEmpleado, cita.IdEmpresa);

                var existeCruce = citasEmpleado.Any(c =>
                    c.Fecha.Date == cita.Fecha.Date &&
                    cita.Hora < c.HoraFin &&
                    cita.HoraFin > c.Hora
                );

                if (existeCruce)
                    return BadRequest(new { message = "Este horario ya está ocupado." });

                // ================================
                // 📎 RECIBO SOLO SI NO ES SEGUIMIENTO
                // ================================
                if (value.esSeguimiento != true
                     && value.ReciboPago != null
                     && value.ReciboPago.Length > 0)
                { 

                    using var ms = new MemoryStream();
                    await value.ReciboPago.CopyToAsync(ms);

                    var nombreArchivo =
                        $"{Guid.NewGuid()}{Path.GetExtension(value.ReciboPago.FileName)}";

                    var ruta = Utility.UploadFileFtp(
                        ms.ToArray(),
                        nombreArchivo
                    );

                    cita.RutaReciboPago = ruta;
                }

                // ================================
                // 💾 GUARDAR
                // ================================
                await _ICitas.InsertCita(cita);

                // ================================
                // 🔔 NOTIFICACIÓN
                // ================================
                string nombreCliente = string.IsNullOrWhiteSpace(cita.NombreCliente)
                    ? "Un cliente"
                    : cita.NombreCliente;

                string fechaTxt = cita.Fecha.ToString("dd/MM/yyyy");
                string horaTxt = cita.Hora.ToString(@"hh\:mm");

                await notification.EnviarNotificacionPorTagAsync(
                    "empresa_id",
                    cita.IdEmpresa.ToString(),
                    "📅 Nueva cita creada",
                    $"{nombreCliente} reservó el servicio el {fechaTxt} a las {horaTxt}"
                );

                return Ok(new { message = "Cita creada correctamente ✅" });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Error al crear cita",
                    error = ex.Message
                });
            }
        }




        [HttpPost("test-notificacion")]
        public async Task<IActionResult> EnviarNotificacionTest([FromForm] int empresaId)
        {
            try
            {
                await notification.EnviarNotificacionPorTagAsync(
                    "empresa_id",
                    empresaId.ToString(),
                    "🔥 Notificación de Prueba 1",
                    "Si ves este mensaje, las notificaciones por TAG están funcionando 1."
                );

                return Ok(new { message = "Notificación de prueba enviada" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpPost("enviar-recordatorio-dia")]
        public async Task<IActionResult> EnviarRecordatorioDia([FromBody] RecordatorioDiaRequest request)
        {
            await _ICitas.EnviarRecordatorioPorFecha(request.Fecha, request.IdEmpresa);

            return Ok(new { message = "Recordatorio Enviado correctamente ✅" });
        }

        // ============================================================
        // PUT api/citas → actualizar cita
        // ============================================================
        [HttpPut]
        public async Task<IActionResult> Put([FromForm] CitaDto value)
        {
            var citaExistente = await _ICitas.GetCitaById(value.IdCita);
            if (citaExistente == null)
                return NotFound(new { message = "La cita no existe" });

            // Datos básicos
            citaExistente.IdEmpleado = value.IdEmpleado;
            citaExistente.NombreCliente = value.NombreCliente;
            citaExistente.Telefono = value.Telefono;
            citaExistente.Correo = value.Correo;
            citaExistente.Nota = value.Nota;
            citaExistente.Costo = value.Costo;
            citaExistente.Estado = value.Estado;

            // Servicio / producto
            citaExistente.IdProducto = value.IdProducto;


            // Fecha
            citaExistente.Fecha = value.Fecha;

            // Hora + duración + fin
            citaExistente.Hora = value.Hora;
            citaExistente.DuracionMinutos = value.DuracionMinutos;

            citaExistente.HoraFin = value.HoraFin != TimeSpan.Zero
                ? value.HoraFin
                : value.Hora.Add(TimeSpan.FromMinutes(value.DuracionMinutos));

            _ICitas.UpdateCita(citaExistente);

            return Ok(new { message = "Cita actualizada correctamente ✅" });
        }

        // DELETE api/citas/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            _ICitas.DeleteCita(id);
            return Ok(new { message = "Cita eliminada correctamente" });
        }
        private static EstadoCita MapearEstado(string estado)
        {
            return estado.Trim().ToLower() switch
            {
                "programada" => EstadoCita.Programada,
                "confirmada" => EstadoCita.Confirmada,
                "en curso" => EstadoCita.EnCurso,
                "completada" => EstadoCita.Completada,
                "cancelada" => EstadoCita.Cancelada,
                _ => throw new Exception("Estado no reconocido")
            };
        }

        // ============================================================
        // 🔥 Cambiar estado de una cita
        // ============================================================
        [HttpPut("CambiarEstado/{idCita}")]
        public async Task<IActionResult> CambiarEstado(
     int idCita,
     [FromBody] CambiarEstadoCitaDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Estado))
                    return BadRequest(new { message = "Estado inválido" });

                EstadoCita estadoEnum;
                try
                {
                    estadoEnum = MapearEstado(dto.Estado);
                }
                catch
                {
                    return BadRequest(new { message = "Estado no reconocido" });
                }

                await _ICitas.CambiarEstadoCita(idCita, estadoEnum);

                return Ok(new { message = "Estado actualizado correctamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Error al cambiar estado",
                    error = ex.Message
                });
            }
        }

    }
}
