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
                var loginResponse = await _loginService.Login(
                    dto.UserName,
                    dto.Password,
                    dto.DeviceId
                );

                // 🔍 Empresa
                var empresa = await _empresasService.GetEmpresaById(
                    loginResponse.Usuario.IdEmpresa
                );

                if (empresa == null)
                    return Unauthorized("Empresa no encontrada");

                // 🔴 Estado general
                if (!empresa.Estado)
                    return Unauthorized("El servicio se encuentra suspendido por falta de pago.");

                // 🔍 Plan (fallback por si viene null)
               

               
               
                    var plan = await _lanesCloud.GetPlanById((int)empresa.IdPlan);
               

                if (plan == null)
                    return Unauthorized("La empresa no tiene un plan válido asignado.");

                // 🔥 RANGO DEL MES
                var fechaInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var fechaFin = fechaInicio.AddMonths(1).AddSeconds(-1);

                var listadoIngresos = await _ingresosService
                    .GetIngresosByFecha(empresa.IdEmpresa, fechaInicio, fechaFin);

                decimal totalFacturado = listadoIngresos?.Any() == true
                    ? listadoIngresos.Sum(x => x.Monto)
                    : 0;

                // 🟡 DEMO
                if (plan.PrecioUSD == 0)
                {
                    if (empresa.FechaTerminacion.Date < DateTime.Now.Date)
                    {
                        return Unauthorized("El período de prueba ha finalizado.");
                    }
                }
                

                // 🔥 ALERTA 80%
                string alertaPlan = null;

                //if (plan.LimiteFacturacion > 0)
                //{
                //    var porcentajeUso = (totalFacturado / plan.LimiteFacturacion) * 100;

                //    if (porcentajeUso >= 80 && porcentajeUso < 100)
                //    {
                //        alertaPlan = $"Has consumido el {Math.Round(porcentajeUso, 0)}% de tu plan.";
                //    }
                //}

                // 🔴 BLOQUEO LOGIN
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
                // ✅ OK
                return Ok(new
                {
                    usuario = new
                    {
                        idUsuario = loginResponse.Usuario.IdUsuario,
                        userName = loginResponse.Usuario.UserName,
                        idEmpresa = loginResponse.Usuario.IdEmpresa,
                        dispositivo = loginResponse.Usuario.Dispositivo,
                        puedeEliminarOrden = loginResponse.PuedeEliminarOrden,
                    },
                    empresa = new
                    {
                        idEmpresa = empresa.IdEmpresa,
                        nombreComercial = empresa.NombreComercial,
                        apiPrint = empresa.ApiPrint,
                        idPlan = empresa.IdPlan, // 🔥 corregido
                        fechaTerminacion = empresa.FechaTerminacion
                    },
                    modulos = loginResponse.Modulos,
                    token = loginResponse.Token,
                    alertaPlan = alertaPlan
                });
            }
            catch (Exception ex)
            {
                return Unauthorized(ex.Message);
            }
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
