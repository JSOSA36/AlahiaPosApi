using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Auth
{
    public static class SucursalConsultaHttp
    {
        public const string HeaderUsuario = "X-IdUsuario";

        public static async Task<(SucursalConsultaScope Scope, IActionResult? Error)> ResolverAsync(
            HttpContext http,
            ISesionTokenResolver tokens,
            ISucursalService sucursales,
            int idEmpresa,
            int? idSucursalFiltro,
            CancellationToken ct = default)
        {
            var sesion = SesionHttp.TryGet(http);
            if (sesion == null)
            {
                var auth = http.Request.Headers["Authorization"].FirstOrDefault();
                sesion = await tokens.ResolverAsync(auth, ct);
            }

            int idUsuario;
            if (sesion != null)
            {
                if (sesion.IdEmpresa != idEmpresa)
                {
                    return (SucursalConsultaScope.Vacio, new JsonResult(new { message = SesionHttp.ForbiddenOtraEmpresa })
                    {
                        StatusCode = 403
                    });
                }

                idUsuario = sesion.IdUsuario;
            }
            else if (!TryUsuarioHeader(http, out idUsuario))
            {
                // ERP vivo no exige Bearer. Sin usuario no hay alcance de sucursal.
                return (SucursalConsultaScope.Vacio, new JsonResult(new { message = SesionHttp.UnauthorizedToken })
                {
                    StatusCode = 401
                });
            }

            try
            {
                var scope = await sucursales.ResolverConsultaAsync(
                    idUsuario, idEmpresa, idSucursalFiltro, ct);
                return (scope, null);
            }
            catch (UnauthorizedAccessException)
            {
                return (SucursalConsultaScope.Vacio, new JsonResult(new { message = SesionHttp.ForbiddenOtraSucursal })
                {
                    StatusCode = 403
                });
            }
        }

        private static bool TryUsuarioHeader(HttpContext http, out int idUsuario)
        {
            idUsuario = 0;
            var raw = http.Request.Headers[HeaderUsuario].FirstOrDefault();
            return int.TryParse(raw, out idUsuario) && idUsuario > 0;
        }
    }
}
