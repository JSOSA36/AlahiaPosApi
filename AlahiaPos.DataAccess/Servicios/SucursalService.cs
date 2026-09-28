using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    public class SucursalService : ISucursalService
    {
        private readonly AlahiaPosContext _db;

        public SucursalService(AlahiaPosContext db)
        {
            _db = db;
        }

        public async Task<Sucursal> AsegurarPrincipalAsync(int idEmpresa, CancellationToken ct = default)
        {
            if (idEmpresa <= 0)
                throw new ArgumentOutOfRangeException(nameof(idEmpresa));

            var principal = await _db.Sucursales
                .AsTracking()
                .FirstOrDefaultAsync(s => s.IdEmpresa == idEmpresa && s.EsPrincipal, ct);

            if (principal == null)
            {
                var empresa = await _db.Empresas.AsNoTracking()
                    .FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa, ct);

                principal = new Sucursal
                {
                    IdEmpresa = idEmpresa,
                    Codigo = "PRINC",
                    Nombre = "Sucursal Principal",
                    EsPrincipal = true,
                    Activa = true,
                    Direccion = empresa?.Direccion,
                    Telefono = empresa?.Telefono,
                    Municipio = empresa?.Municipio,
                    Provincia = empresa?.Provincia,
                    Latitude = empresa?.Latitude,
                    Longitude = empresa?.Longitude,
                    ApiPrint = empresa?.ApiPrint,
                    FechaCreacion = DateTime.Now
                };
                _db.Sucursales.Add(principal);
                await _db.SaveChangesAsync(ct);
            }

            var almacen = await _db.Almacenes
                .AsTracking()
                .FirstOrDefaultAsync(a => a.IdEmpresa == idEmpresa && a.IdSucursal == principal.IdSucursal, ct)
                ?? await _db.Almacenes.AsTracking()
                    .FirstOrDefaultAsync(a => a.IdEmpresa == idEmpresa, ct);

            if (almacen == null)
            {
                almacen = new Almacen
                {
                    Nombre = "Principal",
                    Descripcion = "Almacén principal",
                    IdEmpresa = idEmpresa,
                    EsPrincipal = true,
                    Activo = true,
                    FechaCreacion = DateTime.Now,
                    IdSucursal = principal.IdSucursal
                };
                _db.Almacenes.Add(almacen);
                await _db.SaveChangesAsync(ct);
            }
            else if (almacen.IdSucursal == null || almacen.IdSucursal <= 0)
            {
                almacen.IdSucursal = principal.IdSucursal;
                await _db.SaveChangesAsync(ct);
            }

            if (principal.IdAlmacenPrincipal == null || principal.IdAlmacenPrincipal <= 0)
            {
                principal.IdAlmacenPrincipal = almacen.IdAlmacen;
                await _db.SaveChangesAsync(ct);
            }

            return principal;
        }

        public async Task AsegurarAccesoUsuarioAsync(int idUsuario, int idEmpresa, CancellationToken ct = default)
        {
            if (idUsuario <= 0 || idEmpresa <= 0) return;

            // El administrador no pertenece a una sucursal; ve todas.
            if (await EsAdministradorEmpresaAsync(idUsuario, idEmpresa, ct))
                return;

            var principal = await AsegurarPrincipalAsync(idEmpresa, ct);

            var yaAsignado = await _db.UsuarioSucursales.AnyAsync(
                x => x.IdUsuario == idUsuario && x.Activo, ct);

            if (!yaAsignado)
            {
                _db.UsuarioSucursales.Add(new UsuarioSucursal
                {
                    IdUsuario = idUsuario,
                    IdSucursal = principal.IdSucursal,
                    EsDefault = true,
                    Activo = true,
                    FechaCreacion = DateTime.Now
                });
                await _db.SaveChangesAsync(ct);
            }

            var usuario = await _db.Usuarios.AsTracking()
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario, ct);
            if (usuario != null && (usuario.IdSucursalActiva == null || usuario.IdSucursalActiva <= 0))
            {
                var asignada = await _db.UsuarioSucursales
                    .Where(x => x.IdUsuario == idUsuario && x.Activo)
                    .OrderByDescending(x => x.EsDefault)
                    .Select(x => x.IdSucursal)
                    .FirstOrDefaultAsync(ct);
                usuario.IdSucursalActiva = asignada > 0 ? asignada : principal.IdSucursal;
                await _db.SaveChangesAsync(ct);
            }
        }

        public async Task AsignarOperativaAsync(
            int idUsuario,
            int idEmpresa,
            int? idSucursal,
            bool esAdministrador,
            CancellationToken ct = default)
        {
            if (idUsuario <= 0 || idEmpresa <= 0)
                throw new ArgumentOutOfRangeException(nameof(idUsuario));

            var usuario = await _db.Usuarios
                .AsTracking()
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario && u.IdEmpresa == idEmpresa, ct)
                ?? throw new InvalidOperationException("Usuario no encontrado.");

            var actuales = await _db.UsuarioSucursales
                .AsTracking()
                .Where(x => x.IdUsuario == idUsuario)
                .ToListAsync(ct);

            if (esAdministrador)
            {
                foreach (var a in actuales)
                {
                    a.Activo = false;
                    a.EsDefault = false;
                }
                await _db.SaveChangesAsync(ct);
                return;
            }

            if (idSucursal is not > 0)
                throw new ArgumentException("Debe indicar la sucursal del usuario.");

            var sucursalOk = await _db.Sucursales.AsNoTracking()
                .AnyAsync(s =>
                    s.IdSucursal == idSucursal
                    && s.IdEmpresa == idEmpresa
                    && s.Activa, ct);
            if (!sucursalOk)
                throw new ArgumentException("La sucursal no pertenece a esta empresa.");

            foreach (var a in actuales)
            {
                var esEsta = a.IdSucursal == idSucursal;
                a.Activo = esEsta;
                a.EsDefault = esEsta;
            }

            if (!actuales.Any(a => a.IdSucursal == idSucursal))
            {
                _db.UsuarioSucursales.Add(new UsuarioSucursal
                {
                    IdUsuario = idUsuario,
                    IdSucursal = idSucursal.Value,
                    EsDefault = true,
                    Activo = true,
                    FechaCreacion = DateTime.Now
                });
            }

            usuario.IdSucursalActiva = idSucursal;
            await _db.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<SucursalSesionDto>> ListarPorUsuarioAsync(
            int idUsuario,
            int idEmpresa,
            CancellationToken ct = default)
        {
            if (await EsAdministradorEmpresaAsync(idUsuario, idEmpresa, ct))
            {
                try
                {
                    await AsegurarPrincipalAsync(idEmpresa, ct);
                }
                catch
                {
                    // La lista no depende de crear la principal.
                }
                return await ListarTodasLasSucursalesAsync(idUsuario, idEmpresa, ct);
            }

            var lista = await ListarAsignadasAsync(idUsuario, idEmpresa, ct);
            if (lista.Count == 0)
            {
                await AsegurarAccesoUsuarioAsync(idUsuario, idEmpresa, ct);
                lista = await ListarAsignadasAsync(idUsuario, idEmpresa, ct);
            }

            var usuario = await _db.Usuarios.AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario && u.IdEmpresa == idEmpresa, ct);
            return RestringirASucursalOperativa(lista, usuario?.IdSucursalActiva);
        }

        public async Task<bool> TieneAccesoAsync(
            int idUsuario,
            int idEmpresa,
            int idSucursal,
            CancellationToken ct = default)
        {
            if (idUsuario <= 0 || idEmpresa <= 0 || idSucursal <= 0)
                return false;

            var sucursalOk = await _db.Sucursales.AsNoTracking()
                .AnyAsync(s =>
                    s.IdSucursal == idSucursal
                    && s.IdEmpresa == idEmpresa
                    && s.Activa, ct);
            if (!sucursalOk)
                return false;

            if (await EsAdministradorEmpresaAsync(idUsuario, idEmpresa, ct))
                return true;

            var usuario = await _db.Usuarios.AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario && u.IdEmpresa == idEmpresa, ct);
            var asignadas = await ListarAsignadasAsync(idUsuario, idEmpresa, ct);
            var operativa = RestringirASucursalOperativa(asignadas, usuario?.IdSucursalActiva);
            return operativa.Any(s => s.IdSucursal == idSucursal);
        }

        public async Task<int?> ResolverSucursalActivaAsync(
            int idUsuario,
            int idEmpresa,
            int? idSucursalActiva,
            CancellationToken ct = default)
        {
            var lista = await ListarPorUsuarioAsync(idUsuario, idEmpresa, ct);
            if (lista.Count == 0)
                return idSucursalActiva is > 0 ? idSucursalActiva : null;

            var esAdmin = await EsAdministradorEmpresaAsync(idUsuario, idEmpresa, ct);
            if (!esAdmin && lista.Count == 1)
                return lista[0].IdSucursal;

            if (idSucursalActiva > 0
                && lista.Any(x => x.IdSucursal == idSucursalActiva.Value))
            {
                return idSucursalActiva;
            }

            var def = lista.FirstOrDefault(x => x.EsDefault) ?? lista.FirstOrDefault();
            return def?.IdSucursal;
        }

        public async Task<CambiarSucursalResultado> CambiarActivaAsync(
            int idUsuario,
            int idEmpresa,
            int idSucursal,
            string? dispositivo,
            string? ip,
            CancellationToken ct = default)
        {
            if (!await EsAdministradorEmpresaAsync(idUsuario, idEmpresa, ct))
                throw new UnauthorizedAccessException("Solo el administrador puede cambiar de sucursal.");

            if (!await TieneAccesoAsync(idUsuario, idEmpresa, idSucursal, ct))
                throw new UnauthorizedAccessException("El usuario no tiene acceso a esa sucursal.");

            var usuario = await _db.Usuarios.AsTracking()
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario, ct)
                ?? throw new InvalidOperationException("Usuario no encontrado.");

            var origen = usuario.IdSucursalActiva;
            usuario.IdSucursalActiva = idSucursal;

            _db.SucursalCambioLogs.Add(new SucursalCambioLog
            {
                IdUsuario = idUsuario,
                IdEmpresa = idEmpresa,
                IdSucursalOrigen = origen,
                IdSucursalDestino = idSucursal,
                Fecha = DateTime.Now,
                Dispositivo = dispositivo,
                Ip = ip
            });

            await _db.SaveChangesAsync(ct);

            var lista = await ListarPorUsuarioAsync(idUsuario, idEmpresa, ct);
            var actual = lista.First(x => x.IdSucursal == idSucursal);

            return new CambiarSucursalResultado
            {
                IdSucursal = actual.IdSucursal,
                Nombre = actual.Nombre,
                ApiPrint = actual.ApiPrint,
                IdAlmacenPrincipal = actual.IdAlmacenPrincipal,
                Sucursales = lista
            };
        }

        private async Task<IReadOnlyList<SucursalSesionDto>> ListarTodasLasSucursalesAsync(
            int idUsuario,
            int idEmpresa,
            CancellationToken ct)
        {
            var defaults = await _db.UsuarioSucursales.AsNoTracking()
                .Where(x => x.IdUsuario == idUsuario && x.Activo)
                .ToListAsync(ct);

            var sucursales = await _db.Sucursales.AsNoTracking()
                .Where(s => s.IdEmpresa == idEmpresa && s.Activa)
                .OrderByDescending(s => s.EsPrincipal)
                .ThenBy(s => s.Nombre)
                .ToListAsync(ct);

            var defaultIds = defaults
                .Where(d => d.EsDefault)
                .Select(d => d.IdSucursal)
                .ToHashSet();

            return sucursales.Select(s => new SucursalSesionDto
            {
                IdSucursal = s.IdSucursal,
                Codigo = s.Codigo,
                Nombre = s.Nombre,
                EsPrincipal = s.EsPrincipal,
                EsDefault = defaultIds.Contains(s.IdSucursal),
                Activa = s.Activa,
                Direccion = s.Direccion,
                Telefono = s.Telefono,
                Municipio = s.Municipio,
                Provincia = s.Provincia,
                ApiPrint = s.ApiPrint,
                IdAlmacenPrincipal = s.IdAlmacenPrincipal
            }).ToList();
        }

        private async Task<IReadOnlyList<SucursalSesionDto>> ListarAsignadasAsync(
            int idUsuario,
            int idEmpresa,
            CancellationToken ct)
        {
            return await _db.UsuarioSucursales
                .AsNoTracking()
                .Where(x => x.IdUsuario == idUsuario && x.Activo)
                .Join(_db.Sucursales.AsNoTracking(),
                    us => us.IdSucursal,
                    s => s.IdSucursal,
                    (us, s) => new { us, s })
                .Where(x => x.s.IdEmpresa == idEmpresa && x.s.Activa)
                .OrderByDescending(x => x.s.EsPrincipal)
                .ThenBy(x => x.s.Nombre)
                .Select(x => new SucursalSesionDto
                {
                    IdSucursal = x.s.IdSucursal,
                    Codigo = x.s.Codigo,
                    Nombre = x.s.Nombre,
                    EsPrincipal = x.s.EsPrincipal,
                    EsDefault = x.us.EsDefault,
                    Activa = x.s.Activa,
                    Direccion = x.s.Direccion,
                    Telefono = x.s.Telefono,
                    Municipio = x.s.Municipio,
                    Provincia = x.s.Provincia,
                    ApiPrint = x.s.ApiPrint,
                    IdAlmacenPrincipal = x.s.IdAlmacenPrincipal
                })
                .ToListAsync(ct);
        }

        private async Task<bool> EsAdministradorEmpresaAsync(
            int idUsuario,
            int idEmpresa,
            CancellationToken ct)
        {
            var usuario = await _db.Usuarios.AsNoTracking()
                .Include(u => u.Empleado)
                .Include(u => u.Perfil)
                .Include(u => u.Empresa)
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario && u.IdEmpresa == idEmpresa, ct);

            if (usuario == null)
                return false;

            if (usuario.Empresa?.EsEmpresaSistema == true)
                return true;

            var ocupacion = usuario.Empleado?.Ocupacion?.Trim();
            if (!string.IsNullOrWhiteSpace(ocupacion)
                && string.Equals(ocupacion, "Administrador", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var perfil = usuario.Perfil?.Nombre?.Trim() ?? "";
            if (string.Equals(perfil, "Administrador", StringComparison.OrdinalIgnoreCase)
                || perfil.Contains("ADMIN", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public async Task<SucursalConsultaScope> ResolverConsultaAsync(
            int idUsuario,
            int idEmpresa,
            int? idSucursalFiltro,
            CancellationToken ct = default)
        {
            if (idUsuario <= 0 || idEmpresa <= 0)
                return SucursalConsultaScope.Vacio;

            var lista = (await ListarPorUsuarioAsync(idUsuario, idEmpresa, ct))
                .Where(s => s.Activa)
                .ToList();
            if (lista.Count == 0)
                return SucursalConsultaScope.Vacio;

            var principalEmpresa = await AsegurarPrincipalAsync(idEmpresa, ct);
            var idPrincipalEmpresa = principalEmpresa.IdSucursal;
            var ids = lista.Select(s => s.IdSucursal).Distinct().ToList();
            var esAdmin = await EsAdministradorEmpresaAsync(idUsuario, idEmpresa, ct);

            if (idSucursalFiltro is > 0)
            {
                if (!ids.Contains(idSucursalFiltro.Value))
                    throw new UnauthorizedAccessException("El usuario no tiene acceso a esa sucursal.");

                var una = lista.First(s => s.IdSucursal == idSucursalFiltro.Value);
                return new SucursalConsultaScope
                {
                    IdsPermitidos = new[] { una.IdSucursal },
                    Sucursales = new[] { una },
                    IdPrincipal = idPrincipalEmpresa
                };
            }

            if (!esAdmin)
            {
                var una = lista[0];
                return new SucursalConsultaScope
                {
                    IdsPermitidos = new[] { una.IdSucursal },
                    Sucursales = new[] { una },
                    IdPrincipal = idPrincipalEmpresa
                };
            }

            return new SucursalConsultaScope
            {
                IdsPermitidos = ids,
                Sucursales = lista,
                IdPrincipal = idPrincipalEmpresa
            };
        }

        private static IReadOnlyList<SucursalSesionDto> RestringirASucursalOperativa(
            IReadOnlyList<SucursalSesionDto> lista,
            int? idSucursalActiva)
        {
            if (lista == null || lista.Count <= 1)
                return lista ?? Array.Empty<SucursalSesionDto>();

            if (idSucursalActiva is > 0)
            {
                var deActiva = lista.Where(s => s.IdSucursal == idSucursalActiva.Value).ToList();
                if (deActiva.Count > 0)
                    return deActiva;
            }

            var def = lista.FirstOrDefault(s => s.EsDefault) ?? lista[0];
            return new[] { def };
        }
    }
}
