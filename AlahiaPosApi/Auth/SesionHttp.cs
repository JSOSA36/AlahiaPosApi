using Microsoft.AspNetCore.Http;

namespace AlahiaPosApi.Auth
{
    public static class SesionHttp
    {
        public const string ItemsKey = "Alahia.SesionActual";
        public const string ForbiddenOtraEmpresa = "No puede acceder a datos de otra empresa.";
        public const string ForbiddenOtraSucursal = "No puede acceder a datos de otra sucursal.";
        public const string UnauthorizedToken = "Token de sesión requerido o inválido.";
        public const string ForbiddenMacroBits = "Solo MacroBits puede ejecutar esta operación.";

        public static SesionActual? TryGet(HttpContext? http)
        {
            if (http?.Items == null) return null;
            return http.Items.TryGetValue(ItemsKey, out var v) ? v as SesionActual : null;
        }

        public static void Set(HttpContext http, SesionActual sesion)
        {
            http.Items[ItemsKey] = sesion;
        }

        public static string? ExtraerToken(string? authorization)
        {
            if (string.IsNullOrWhiteSpace(authorization)) return null;
            var t = authorization.Trim();
            if (t.StartsWith("Bearer ", System.StringComparison.OrdinalIgnoreCase))
                return t["Bearer ".Length..].Trim();
            return t;
        }
    }
}
