using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AlahiaPosApi.Auth
{
    /// <summary>
    /// Quién cobra / guarda la orden en el ERP vivo:
    /// sesión → Bearer → X-IdUsuario → IdUsuario del POS.
    /// No exigir token: el filtro de sesión está apagado en producción.
    /// </summary>
    public static class IdUsuarioCobroResolver
    {
        public static async Task<int> ResolverAsync(
            HttpContext http,
            ISesionTokenResolver tokens,
            int? idUsuarioDto,
            CancellationToken ct = default)
        {
            var sesion = SesionHttp.TryGet(http);
            if (sesion != null && sesion.IdUsuario > 0)
                return sesion.IdUsuario;

            var auth = http.Request.Headers["Authorization"].FirstOrDefault();
            sesion = await tokens.ResolverAsync(auth, ct);
            if (sesion != null && sesion.IdUsuario > 0)
            {
                SesionHttp.Set(http, sesion);
                return sesion.IdUsuario;
            }

            var raw = http.Request.Headers[SucursalConsultaHttp.HeaderUsuario].FirstOrDefault();
            if (int.TryParse(raw, out var idHeader) && idHeader > 0)
                return idHeader;

            return idUsuarioDto is > 0 ? idUsuarioDto.Value : 0;
        }
    }
}
