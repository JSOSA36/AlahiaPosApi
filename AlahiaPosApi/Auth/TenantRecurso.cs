using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Auth
{
    /// <summary>
    /// Comprueba IdEmpresa de un registro ya cargado (Get-by-id).
    /// Distinto del guard de binding: aquí no hay IdEmpresa en la ruta.
    /// </summary>
    public static class TenantRecurso
    {
        public static bool EsDeLaSesion(HttpContext? http, int idEmpresaRecurso)
        {
            var s = SesionHttp.TryGet(http);
            // Sin filtro global de sesión: no bloquear lecturas.
            if (s == null) return true;
            return s.IdEmpresa == idEmpresaRecurso;
        }

        public static IActionResult? RechazarSiOtraEmpresa(HttpContext? http, int idEmpresaRecurso)
        {
            if (EsDeLaSesion(http, idEmpresaRecurso))
                return null;
            return new NotFoundResult();
        }
    }
}
