using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPosApi.Auth
{
    public sealed class SesionTokenResolver : ISesionTokenResolver
    {
        private readonly AlahiaPosContext _db;
        private readonly ISucursalService _sucursales;

        public SesionTokenResolver(AlahiaPosContext db, ISucursalService sucursales)
        {
            _db = db;
            _sucursales = sucursales;
        }

        public async Task<SesionActual?> ResolverAsync(string? bearerOrToken, CancellationToken cancellationToken = default)
        {
            var token = SesionHttp.ExtraerToken(bearerOrToken);
            if (string.IsNullOrWhiteSpace(token) || token == "ok")
                return null;

            var usuario = await _db.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Token == token, cancellationToken);

            if (usuario == null || !usuario.Estado || usuario.IdEmpresa <= 0)
                return null;

            var esSistema = await _db.Empresas
                .AsNoTracking()
                .Where(e => e.IdEmpresa == usuario.IdEmpresa)
                .Select(e => e.EsEmpresaSistema)
                .FirstOrDefaultAsync(cancellationToken);

            var idSucursal = 0;
            try
            {
                var resuelta = await _sucursales.ResolverSucursalActivaAsync(
                    usuario.IdUsuario,
                    usuario.IdEmpresa,
                    usuario.IdSucursalActiva,
                    cancellationToken);
                idSucursal = resuelta ?? 0;
            }
            catch
            {
                idSucursal = usuario.IdSucursalActiva ?? 0;
            }

            return new SesionActual
            {
                IdUsuario = usuario.IdUsuario,
                IdEmpresa = usuario.IdEmpresa,
                IdSucursal = idSucursal,
                IdPerfil = usuario.IdPerfil,
                Estado = usuario.Estado,
                EsEmpresaSistema = esSistema,
                UserName = usuario.UserName ?? ""
            };
        }

        public async Task<bool> AplicarSucursalHeaderAsync(
            SesionActual sesion,
            int idSucursalHeader,
            CancellationToken cancellationToken = default)
        {
            if (idSucursalHeader <= 0)
                return true;

            if (idSucursalHeader == sesion.IdSucursal)
                return true;

            var ok = await _sucursales.TieneAccesoAsync(
                sesion.IdUsuario,
                sesion.IdEmpresa,
                idSucursalHeader,
                cancellationToken);

            if (!ok)
                return false;

            sesion.IdSucursal = idSucursalHeader;
            return true;
        }
    }
}
