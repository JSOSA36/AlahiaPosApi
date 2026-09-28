using System.Linq;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AlahiaPosApi.Auth
{
    /// <summary>
    /// Si el endpoint tiene [RequiereTerminalPos], exige huella y asiento libre/activo.
    /// MacroBits (empresa sistema) no consume cupo.
    /// </summary>
    public sealed class RequiereTerminalPosFilter : IAsyncActionFilter
    {
        public const string HeaderName = "X-Pos-Device-Id";

        private readonly IPosTerminalService _terminales;

        public RequiereTerminalPosFilter(IPosTerminalService terminales)
        {
            _terminales = terminales;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var endpoint = context.HttpContext.GetEndpoint();
            if (endpoint?.Metadata.GetMetadata<RequiereTerminalPosAttribute>() == null)
            {
                await next();
                return;
            }

            var sesion = SesionHttp.TryGet(context.HttpContext);
            if (sesion == null)
            {
                context.Result = new JsonResult(new { message = SesionHttp.UnauthorizedToken })
                {
                    StatusCode = 401
                };
                return;
            }

            var deviceId = context.HttpContext.Request.Headers[HeaderName].FirstOrDefault()
                ?? context.HttpContext.Request.Headers["x-pos-device-id"].FirstOrDefault()
                ?? "";

            var result = await _terminales.ClaimAsync(
                sesion.IdEmpresa,
                sesion.IdUsuario,
                new PosTerminalClaimRequest { DeviceId = deviceId },
                sesion.EsEmpresaSistema,
                context.HttpContext.RequestAborted);

            if (!result.Permitido)
            {
                context.Result = new JsonResult(new
                {
                    message = result.Mensaje,
                    codigo = result.Codigo,
                    limite = result.Limite,
                    usadas = result.Usadas
                })
                {
                    StatusCode = 403
                };
                return;
            }

            await next();
        }
    }
}
