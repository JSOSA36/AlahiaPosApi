using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
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

        public LoginController(
            ILoginService loginService,
            IUsuarios usuariosService,
            IEmpresas empresasService,
             IIngresos ingresosService,
             IPlanesCloud lanesCloud


        )
        {
            _loginService = loginService;
            _usuariosService = usuariosService;
            _empresasService = empresasService;
            _ingresosService= ingresosService;
            _lanesCloud = lanesCloud;


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

                // 🔐 Validar credenciales
                var loginResponse = await _loginService.Login(usuarioDb, dto.Password);

                // 🔍 Empresa
                var empresa = await _empresasService.GetEmpresaById(usuarioDb.IdEmpresa);

                if (empresa == null)
                    return Unauthorized("Empresa no encontrada");

                // 🔥 Actualizar estado automático
                await _empresasService.ActualizarEstadoEmpresa(empresa.IdEmpresa);

                // 🔄 Refrescar empresa después del update
                empresa = await _empresasService.GetEmpresaById(usuarioDb.IdEmpresa);

                // 🔔 Obtener alerta con la empresa actualizada
                var alertaPago = _empresasService.ObtenerAlertaPago(empresa);

                // 🔴 Validar si puede operar
                //if (!_empresasService.PuedeOperar(empresa))
                //{
                //    return StatusCode(403, new
                //    {
                //        bloqueado = true,
                //        mensaje = "Tu servicio está suspendido. Debes renovar tu plan para continuar.",
                //        estadoServicio = empresa.EstadoServicio,
                //        empresa = new
                //        {
                //            idEmpresa = empresa.IdEmpresa,
                //            nombreComercial = empresa.NombreComercial
                //        }
                //    });
                //}

                // 🔍 Plan
                var plan = await _lanesCloud.GetPlanById((int)empresa.IdPlan);

                if (plan == null)
                    return StatusCode(403, "La empresa no tiene un plan válido asignado.");

                // 🔥 Validar demo
                if (plan.PrecioUSD == 0 && empresa.FechaTerminacion.Date < DateTime.Now.Date)
                {
                    return StatusCode(403, "El período de prueba ha finalizado.");
                }

                // 🔥 Facturación del mes
                var fechaInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var fechaFin = fechaInicio.AddMonths(1).AddSeconds(-1);

                var listadoIngresos = await _ingresosService
                    .GetIngresosByFecha(empresa.IdEmpresa, fechaInicio, fechaFin);

                decimal totalFacturado = listadoIngresos?.Any() == true
                    ? listadoIngresos.Sum(x => x.Monto)
                    : 0;

                // 🔴 Validar límite del plan
                if (plan.LimiteFacturacion > 0 && totalFacturado >= plan.LimiteFacturacion)
                {
                    return Ok(new
                    {
                        requiereUpgrade = true,
                        mensaje = "Has alcanzado el límite de facturación de tu plan.",
                        empresa = new
                        {
                            idEmpresa = empresa.IdEmpresa,
                            idPlan = empresa.IdPlan
                        }
                    });
                }

                // ✅ AHORA SÍ → CREAR SESIÓN
                usuarioDb.Token = Guid.NewGuid().ToString();
                usuarioDb.Dispositivo = dto.DeviceId;
                usuarioDb.UltimoAcceso = DateTime.Now;

                await _usuariosService.Actualizar(usuarioDb);

                // ✅ RESPUESTA FINAL
                return Ok(new
                {
                    usuario = new
                    {
                        idUsuario = usuarioDb.IdUsuario,
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
                        puedeEditarPrecioCarrito = usuarioDb.PuedeEditarPrecioCarrito
                    },
                    empresa = new
                    {
                        idEmpresa = empresa.IdEmpresa,
                        nombreComercial = empresa.NombreComercial,
                        apiPrint = empresa.ApiPrint,
                        idPlan = empresa.IdPlan,
                        nombrePlan = plan.Nombre,
                        fechaTerminacion = empresa.FechaTerminacion,
                        estadoServicio = empresa.EstadoServicio,
                        pagadoServicio = empresa.PagadoServicio,
                        PoliticasAceptadas=empresa.PoliticasAceptadas
                    },
                    modulos = loginResponse.Modulos,
                    token = usuarioDb.Token,
                    alertaPlan = new
                    {
                        tipo = alertaPago.Tipo,
                        mensaje = alertaPago.Mensaje,
                    }
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
