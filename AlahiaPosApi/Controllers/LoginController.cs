using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly ILoginService _loginService;
        private readonly IUsuarios _usuariosService;
        private readonly IEmpresas _empresasService;
        private readonly IIngresos _ingresosService;
        private readonly IPlanesCloud _lanesCloud;
        private readonly IPoliticasServicioService _politicasServicio;
        private readonly ISuscripcionCobroService _suscripcionCobro;
        private readonly INotificacionCentro _notificaciones;
        private readonly AlahiaPosContext _ctx;

        public LoginController(
            ILoginService loginService,
            IUsuarios usuariosService,
            IEmpresas empresasService,
             IIngresos ingresosService,
             IPlanesCloud lanesCloud,
             IPoliticasServicioService politicasServicio,
             ISuscripcionCobroService suscripcionCobro,
             INotificacionCentro notificaciones,
             AlahiaPosContext ctx
        )
        {
            _loginService = loginService;
            _usuariosService = usuariosService;
            _empresasService = empresasService;
            _ingresosService= ingresosService;
            _lanesCloud = lanesCloud;
            _politicasServicio = politicasServicio;
            _suscripcionCobro = suscripcionCobro;
            _notificaciones = notificaciones;
            _ctx = ctx;
        }

        // =====================================================
        // 🔐 LOGIN OFICIAL
        // =====================================================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (dto == null)
                return BadRequest("Datos inválidos");

            if (string.IsNullOrWhiteSpace(dto.UserName) ||
                string.IsNullOrWhiteSpace(dto.Password) ||
                string.IsNullOrWhiteSpace(dto.DeviceId))
            {
                return BadRequest("Usuario, contraseña y dispositivo son obligatorios");
            }

            try
            {
                // 🔍 Usuario
                var usuarioDb =
                    await _usuariosService
                    .ObtenerPorUserName(dto.UserName);
                // 🔒 VALIDAR SESIÓN ACTIVA
                //if (!string.IsNullOrEmpty(usuarioDb.Token) &&
                // !string.IsNullOrEmpty(usuarioDb.Dispositivo))
                //{
                //    return Ok(new
                //    {
                //        errorSesion = true,
                //        mensaje = "Este usuario ya tiene una sesión activa en otro dispositivo."
                //    });
                //}
                if (usuarioDb == null)
                    return Unauthorized("Usuario no encontrado");

                // 🔐 Validar credenciales (401, nunca 500)
                var loginResponse = await _loginService.Login(usuarioDb, dto.Password);
                if (loginResponse == null)
                    return Unauthorized("Usuario o contraseña inválidos");

                // 🔍 Empresa
                var empresa = await _empresasService.GetEmpresaById(usuarioDb.IdEmpresa);

                if (empresa == null)
                    return Unauthorized("Empresa no encontrada");

                // El ciclo de suscripción NO puede bloquear el login (504 en IIS).
                // Si SQL está lento o con lock, se usa el estado ya guardado.
                try
                {
                    await _empresasService.ActualizarEstadoEmpresa(empresa.IdEmpresa)
                        .WaitAsync(TimeSpan.FromSeconds(4));
                    empresa = await _empresasService.GetEmpresaById(usuarioDb.IdEmpresa)
                        ?? empresa;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Login: suscripción omitida ({ex.Message})");
                }

                // 🔔 Obtener alerta con la empresa actualizada
                var alertaPago = _empresasService.ObtenerAlertaPago(empresa);

                // Cobro dinámico por empresa (sin catálogo de planes)
                var nombrePlanEmpresa = string.IsNullOrWhiteSpace(empresa.NombreComercial)
                    ? $"Plan Empresa {empresa.IdEmpresa}"
                    : $"Plan {empresa.NombreComercial.Trim()}";

                // Demo / prueba: MontoServicio = 0
                if (empresa.MontoServicio <= 0 && empresa.FechaTerminacion.Date < DateTime.Now.Date)
                {
                    return StatusCode(403, "El período de prueba ha finalizado.");
                }

                var esDemoVigente = empresa.MontoServicio <= 0
                    && empresa.FechaTerminacion.Date >= DateTime.Now.Date;

                if (esDemoVigente)
                {
                    if (empresa.EstadoServicio != SuscripcionEstados.Activa || !empresa.PagadoServicio)
                    {
                        empresa.EstadoServicio = SuscripcionEstados.Activa;
                        empresa.PagadoServicio = true;
                        empresa.PoliticasAceptadas = true;
                        _empresasService.UpdateEmpresas(empresa.IdEmpresa, empresa);
                    }
                }

                // 🔴 Suscripción bloqueada (suspendida / pago en validación / cancelada)
                // Se permite sesión mínima para Reportar Pago, pero el front no entra al ERP.
                var bloqueado = !esDemoVigente && _suscripcionCobro.EstaBloqueada(empresa);
                if (bloqueado)
                {
                    usuarioDb.Token = Guid.NewGuid().ToString();
                    usuarioDb.Dispositivo = dto.DeviceId;
                    usuarioDb.UltimoAcceso = DateTime.Now;
                    await _usuariosService.Actualizar(usuarioDb);

                    var factura = await _suscripcionCobro.CalcularFacturaAsync(empresa.IdEmpresa);
                    var pagoPendiente = await _ctx.PagosEmpresa.AsNoTracking()
                        .AnyAsync(p => p.IdEmpresa == empresa.IdEmpresa && p.Estado == "PENDIENTE");

                    // Si hay voucher pendiente, sincronizar estado y no pedir otro reporte
                    var estadoEfectivo = empresa.EstadoServicio ?? SuscripcionEstados.Suspendida;
                    if (pagoPendiente
                        && estadoEfectivo != SuscripcionEstados.Cancelada
                        && estadoEfectivo != SuscripcionEstados.PagoReportado)
                    {
                        empresa.EstadoServicio = SuscripcionEstados.PagoReportado;
                        _empresasService.UpdateEmpresas(empresa.IdEmpresa, empresa);
                        estadoEfectivo = SuscripcionEstados.PagoReportado;
                    }
                    else if (pagoPendiente)
                    {
                        estadoEfectivo = SuscripcionEstados.PagoReportado;
                    }

                    var pagoEnValidacion = pagoPendiente
                        || estadoEfectivo == SuscripcionEstados.PagoReportado;

                    var mensajeBloqueo = pagoEnValidacion
                        ? "Su pago está en validación. El acceso se restaurará cuando MacroBits lo apruebe."
                        : (alertaPago?.Mensaje ?? "Su servicio se encuentra suspendido por falta de pago.");

                    var puedeReportar = estadoEfectivo != SuscripcionEstados.Cancelada
                        && !pagoEnValidacion;

                    return Ok(new
                    {
                        bloqueado = true,
                        mensaje = mensajeBloqueo,
                        estadoServicio = estadoEfectivo,
                        puedeReportarPago = puedeReportar,
                        pagoEnValidacion,
                        token = usuarioDb.Token,
                        usuario = new
                        {
                            idUsuario = usuarioDb.IdUsuario,
                            userName = usuarioDb.UserName,
                            idEmpresa = usuarioDb.IdEmpresa
                        },
                        empresa = new
                        {
                            idEmpresa = empresa.IdEmpresa,
                            nombreComercial = empresa.NombreComercial,
                            idPlan = empresa.IdPlan,
                            nombrePlan = factura.NombrePlan ?? nombrePlanEmpresa,
                            nivelSoporte = NivelesSoporte.Normalizar(empresa.NivelSoporte),
                            estadoServicio = estadoEfectivo,
                            precioPlan = factura.Total,
                            montoPlan = factura.MontoPlan,
                            montoCargos = factura.MontoCargos,
                            montoServicio = factura.MontoServicio,
                            cargoAdicional = factura.CargoAdicional,
                            desgloseFactura = factura.Lineas,
                            correElectronico = empresa.CorreElectronico
                        },
                        alertaPlan = new
                        {
                            tipo = alertaPago?.Tipo ?? "critico",
                            mensaje = mensajeBloqueo
                        }
                    });
                }

                // Límite mensual de ingresos RD$ (0 = ilimitado).
                // No puede tumbar el login si FacturaHeaders está lenta o con lock.
                if (empresa.LimiteFacturacion > 0)
                {
                    try
                    {
                        var fechaInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                        var fechaFin = fechaInicio.AddMonths(1);
                        var ingresosMes = await _ctx.FacturaHeaders.AsNoTracking()
                            .Where(h =>
                                h.IdEmpresa == empresa.IdEmpresa
                                && h.IdTipoDocumentos == 1
                                && !h.EstaCancelada
                                && h.FechaInseccion >= fechaInicio
                                && h.FechaInseccion < fechaFin)
                            .SumAsync(h => (decimal?)h.Total)
                            .WaitAsync(TimeSpan.FromSeconds(4)) ?? 0m;

                        if (ingresosMes >= empresa.LimiteFacturacion)
                        {
                            return Ok(new
                            {
                                requiereUpgrade = true,
                                mensaje = $"Ha llegado al límite de ingresos de su plan contratado (RD$ {empresa.LimiteFacturacion:N0}). Debe ponerse en contacto con nosotros para ampliar su servicio.",
                                empresa = new
                                {
                                    idEmpresa = empresa.IdEmpresa,
                                    idPlan = empresa.IdPlan,
                                    limiteFacturacion = empresa.LimiteFacturacion,
                                    ingresosMes
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Login: límite facturación omitido ({ex.Message})");
                    }
                }

                // ✅ AHORA SÍ → CREAR SESIÓN
                usuarioDb.Token = Guid.NewGuid().ToString();
                usuarioDb.Dispositivo = dto.DeviceId;
                usuarioDb.UltimoAcceso = DateTime.Now;

                await _usuariosService.Actualizar(usuarioDb);

                var politicasEstado = new PoliticasEstadoDto { RequiereAceptacion = false };
                try
                {
                    politicasEstado = await _politicasServicio
                        .ObtenerEstadoAsync(usuarioDb.IdEmpresa, usuarioDb.IdUsuario)
                        .WaitAsync(TimeSpan.FromSeconds(4));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Login: políticas omitidas ({ex.Message})");
                }

                var esAdministrador = politicasEstado?.EsAdministrador == true
                    || _politicasServicio.EsAdministrador(usuarioDb.Empleado?.Ocupacion);

                int notificacionesNoLeidas = 0;
                try
                {
                    notificacionesNoLeidas = await _notificaciones.ContarNoLeidasAsync(
                        empresa.IdEmpresa, usuarioDb.IdUsuario)
                        .WaitAsync(TimeSpan.FromSeconds(3));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Login: notificaciones omitidas ({ex.Message})");
                }

                // ✅ RESPUESTA FINAL
                return Ok(new
                {
                    usuario = new
                    {
                        idUsuario = usuarioDb.IdUsuario,
                        idEmpleado = usuarioDb.IdEmpleado,
                        userName = usuarioDb.UserName,
                        nombre =
                            usuarioDb.Empleado != null
                            &&
                            !string.IsNullOrWhiteSpace(
                                usuarioDb.Empleado.Nombre)
                            ?
                            usuarioDb.Empleado.Nombre.Trim()
                            :
                            usuarioDb.UserName,
                        idEmpresa = usuarioDb.IdEmpresa,
                        dispositivo = dto.DeviceId,
                        puedeEliminarOrden = loginResponse.PuedeEliminarOrden,
                        puedeEliminarItemCarrito = usuarioDb.PuedeEliminarItemCarrito,
                        puedeDisminuirCantidadCarrito = usuarioDb.PuedeDisminuirCantidadCarrito,
                        puedeEditarPrecioCarrito = usuarioDb.PuedeEditarPrecioCarrito,
                        esAdministrador,
                        idPerfil = usuarioDb.IdPerfil,
                        nombrePerfil = usuarioDb.Perfil?.Nombre
                    },
                    empresa = new
                    {
                        idEmpresa = empresa.IdEmpresa,
                        nombreComercial = empresa.NombreComercial,
                        rnc = empresa.RNC,
                        direccion = empresa.Direccion,
                        telefono = empresa.Telefono,
                        correElectronico = empresa.CorreElectronico,
                        logo = empresa.Logo,
                        apiPrint = empresa.ApiPrint,
                        idPlan = empresa.IdPlan,
                        nombrePlan = nombrePlanEmpresa,
                        nivelSoporte = NivelesSoporte.Normalizar(empresa.NivelSoporte),
                        fechaTerminacion = empresa.FechaTerminacion,
                        estadoServicio = empresa.EstadoServicio,
                        pagadoServicio = empresa.PagadoServicio,
                        montoServicio = empresa.MontoServicio,
                        cargoAdicional = empresa.CargoAdicional,
                        limiteFacturacion = empresa.LimiteFacturacion,
                        PoliticasAceptadas = !(politicasEstado?.RequiereAceptacion ?? false)
                    },
                    politicas = politicasEstado,
                    modulos = loginResponse.Modulos,
                    token = usuarioDb.Token,
                    alertaPlan = alertaPago == null || string.IsNullOrWhiteSpace(alertaPago.Mensaje)
                        ? null
                        : new
                        {
                            tipo = alertaPago.Tipo,
                            mensaje = alertaPago.Mensaje,
                            diaCobro = alertaPago.DiaCobro,
                            diasRestantes = alertaPago.DiasRestantes
                        },
                    notificacionesNoLeidas
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] int idUsuario)
        {
            var usuario = await _usuariosService.ObtenerPorId(idUsuario);

            if (usuario == null)
                return NotFound();

            usuario.Token = null;
            usuario.Dispositivo = null;

            await _usuariosService.Actualizar(usuario);

            return Ok("Sesión cerrada correctamente");
        }
        // =====================================================
        // 🔑 SOLICITAR RECUPERACIÓN DE CONTRASEÑA
        // =====================================================
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(
            [FromBody] ForgotPasswordDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Correo))
                return BadRequest("Correo requerido");

            try
            {
                var token = await _loginService
                    .GenerarTokenRecuperacion(dto.Correo);

                // 📧 Aquí luego puedes enviar correo
                return Ok(new
                {
                    mensaje = "Se ha enviado un enlace de recuperación",
                    token // ⚠️ solo para pruebas, quitar en prod
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // =====================================================
        // 🔎 VALIDAR TOKEN DE RECUPERACIÓN
        // =====================================================
        [HttpGet("validate-reset-token/{token}")]
        public async Task<IActionResult> ValidateResetToken(string token)
        {
            var usuario = await _loginService
                .ValidarTokenRecuperacion(token);

            if (usuario == null)
                return BadRequest("Token inválido o expirado");

            return Ok(new
            {
                valido = true,
                usuario = new
                {
                    usuario.IdUsuario,
                    usuario.UserName
                }
            });
        }

        // =====================================================
        // 🔁 RESET PASSWORD CON TOKEN
        // =====================================================
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(
            [FromBody] ResetPasswordDto dto)
        {
            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.Token) ||
                string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                return BadRequest("Datos incompletos");
            }

            var result = await _loginService
                .ResetPasswordConToken(dto.Token, dto.NewPassword);

            if (!result)
                return BadRequest("Token inválido o expirado");

            return Ok("Contraseña actualizada correctamente");
        }
    }
}
