using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AlahiaPosApi.Auth
{
    /// <summary>
    /// Filtro global: Bearer → Usuarios.Token → SesionActual.
    /// Rechaza IdEmpresa / IdSucursal ajenos. Header X-IdSucursal solo si hay acceso.
    /// </summary>
    public sealed class SesionAuthFilter : IAsyncActionFilter
    {
        public const string HeaderSucursal = "X-IdSucursal";

        private readonly ISesionTokenResolver _resolver;

        public SesionAuthFilter(ISesionTokenResolver resolver)
        {
            _resolver = resolver;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var endpoint = context.HttpContext.GetEndpoint();
            var allowAnonymous = endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null;
            var authHeader = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            var sesion = await _resolver.ResolverAsync(authHeader, context.HttpContext.RequestAborted);

            if (sesion != null)
            {
                if (await RechazarHeaderSucursalInvalido(context, sesion))
                    return;
                SesionHttp.Set(context.HttpContext, sesion);
            }

            if (allowAnonymous)
            {
                if (sesion != null)
                {
                    var deniedAnon = RejectIfForbidden(context, endpoint, sesion);
                    if (deniedAnon)
                        return;
                }

                await next();
                return;
            }

            if (sesion == null)
            {
                context.Result = new JsonResult(new { message = SesionHttp.UnauthorizedToken })
                {
                    StatusCode = 401
                };
                return;
            }

            if (RejectIfForbidden(context, endpoint, sesion))
                return;

            await next();
        }

        private async Task<bool> RechazarHeaderSucursalInvalido(
            ActionExecutingContext context,
            SesionActual sesion)
        {
            var raw = context.HttpContext.Request.Headers[HeaderSucursal].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            if (!int.TryParse(raw, out var idSucursal) || idSucursal <= 0)
            {
                context.Result = new JsonResult(new { message = SesionHttp.ForbiddenOtraSucursal })
                {
                    StatusCode = 403
                };
                return true;
            }

            var ok = await _resolver.AplicarSucursalHeaderAsync(
                sesion, idSucursal, context.HttpContext.RequestAborted);
            if (ok)
                return false;

            context.Result = new JsonResult(new { message = SesionHttp.ForbiddenOtraSucursal })
            {
                StatusCode = 403
            };
            return true;
        }

        private static bool RejectIfForbidden(
            ActionExecutingContext context,
            Endpoint? endpoint,
            SesionActual sesion)
        {
            var requiereSistema = endpoint?.Metadata.GetMetadata<RequiereEmpresaSistemaAttribute>() != null;
            if (requiereSistema && !sesion.EsEmpresaSistema)
            {
                context.Result = new JsonResult(new { message = SesionHttp.ForbiddenMacroBits })
                {
                    StatusCode = 403
                };
                return true;
            }

            var permitirObjetivo = endpoint?.Metadata.GetMetadata<PermitirEmpresaObjetivoAttribute>() != null;
            var bind = TenantIdEmpresaGuard.Apply(
                context.ActionArguments,
                sesion.IdEmpresa,
                permitirObjetivo,
                sesion.EsEmpresaSistema);

            if (bind.Forbidden)
            {
                context.Result = new JsonResult(new { message = SesionHttp.ForbiddenOtraEmpresa })
                {
                    StatusCode = 403
                };
                return true;
            }

            var omitirSucursal = endpoint?.Metadata.GetMetadata<PermitirCambioSucursalAttribute>() != null
                || (permitirObjetivo && sesion.EsEmpresaSistema);

            var sucursalBind = TenantSucursalGuard.Apply(
                context.ActionArguments,
                sesion.IdSucursal,
                omitir: omitirSucursal);

            if (sucursalBind.Forbidden)
            {
                context.Result = new JsonResult(new { message = SesionHttp.ForbiddenOtraSucursal })
                {
                    StatusCode = 403
                };
                return true;
            }

            return false;
        }
    }
}
