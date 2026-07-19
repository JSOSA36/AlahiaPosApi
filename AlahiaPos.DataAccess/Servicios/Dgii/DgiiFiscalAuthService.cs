using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios.Dgii
{
    /// <summary>
    /// Autorización para config/reproceso DGII vía token de sesión (Usuarios.Token).
    /// </summary>
    public class DgiiFiscalAuthService : IDgiiFiscalAuthService
    {
        public const string CodigoModuloConfig = "CONFIGURACION_DGII";

        private readonly AlahiaPosContext _context;
        private readonly IPerfilRoles _perfilRoles;

        public DgiiFiscalAuthService(AlahiaPosContext context, IPerfilRoles perfilRoles)
        {
            _context = context;
            _perfilRoles = perfilRoles;
        }

        public Task EnsureCanManageConfigAsync(string? bearerToken, int idEmpresa, int idUsuarioClaimed)
            => EnsureAsync(bearerToken, idEmpresa, idUsuarioClaimed, requireConfigPerm: true);

        public Task EnsureCanReprocessAsync(string? bearerToken, int idEmpresa, int idUsuarioClaimed)
            => EnsureAsync(bearerToken, idEmpresa, idUsuarioClaimed, requireConfigPerm: true);

        private async Task EnsureAsync(string? bearerToken, int idEmpresa, int idUsuarioClaimed, bool requireConfigPerm)
        {
            var token = ExtractToken(bearerToken);
            if (string.IsNullOrWhiteSpace(token))
                throw new UnauthorizedAccessException("Token de sesión requerido.");

            if (idEmpresa <= 0 || idUsuarioClaimed <= 0)
                throw new UnauthorizedAccessException("IdEmpresa e IdUsuario son obligatorios.");

            var usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuarioClaimed && u.Token == token);

            if (usuario == null)
                throw new UnauthorizedAccessException("Sesión inválida.");

            if (usuario.IdEmpresa != idEmpresa)
                throw new UnauthorizedAccessException("El usuario no administra esta empresa.");

            if (!usuario.Estado)
                throw new UnauthorizedAccessException("Usuario inactivo.");

            var perfil = await _context.Perfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdPerfil == usuario.IdPerfil);

            var esAdmin = perfil != null
                && (perfil.Nombre?.Contains("Admin", StringComparison.OrdinalIgnoreCase) == true
                    || string.Equals(perfil.Nombre, "Administrador", StringComparison.OrdinalIgnoreCase));

            if (esAdmin)
                return;

            if (!requireConfigPerm)
                return;

            var modulos = await _perfilRoles.ObtenerModulos(usuario.IdPerfil, idEmpresa);
            var puede = modulos.Any(m =>
                string.Equals(m.Codigo, CodigoModuloConfig, StringComparison.OrdinalIgnoreCase)
                || string.Equals(m.Codigo, "DGII_FISCAL", StringComparison.OrdinalIgnoreCase));

            if (!puede)
                throw new UnauthorizedAccessException("Sin permiso CONFIGURACION_DGII.");
        }

        private static string? ExtractToken(string? bearer)
        {
            if (string.IsNullOrWhiteSpace(bearer)) return null;
            var t = bearer.Trim();
            if (t.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return t["Bearer ".Length..].Trim();
            return t;
        }
    }
}
