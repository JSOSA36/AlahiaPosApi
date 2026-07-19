using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using PrinterLibrary;

namespace AlahiaPos.DataAccess.Servicios
{
    public class PoliticasServicioService : IPoliticasServicioService
    {
        private readonly AlahiaPosContext _ctx;

        public PoliticasServicioService(AlahiaPosContext ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// El rol operativo del empleado (Ocupacion): Administrador, Cajero, Peluquera, etc.
        /// </summary>
        public bool EsAdministrador(string? ocupacionRol)
        {
            if (string.IsNullOrWhiteSpace(ocupacionRol))
                return false;

            return string.Equals(ocupacionRol.Trim(), "Administrador", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Admin de políticas: Ocupacion, perfil Administrador, o empresa sistema (MacroBits).
        /// </summary>
        private bool EsAdministradorUsuario(Usuarios? usuario)
        {
            if (usuario == null) return false;

            if (EsAdministrador(usuario.Empleado?.Ocupacion))
                return true;

            if (!string.IsNullOrWhiteSpace(usuario.Perfil?.Nombre) &&
                string.Equals(usuario.Perfil.Nombre.Trim(), "Administrador", StringComparison.OrdinalIgnoreCase))
                return true;

            if (usuario.Empresa?.EsEmpresaSistema == true)
                return true;

            return false;
        }

        public async Task<PoliticasEstadoDto> ObtenerEstadoAsync(int idEmpresa, int idUsuario)
        {
            var usuario = await _ctx.Usuarios
                .AsNoTracking()
                .Include(u => u.Empleado)
                .Include(u => u.Perfil)
                .Include(u => u.Empresa)
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario && u.IdEmpresa == idEmpresa);

            var esAdmin = usuario != null && EsAdministradorUsuario(usuario);

            var versionActiva = await _ctx.PoliticasVersion
                .AsNoTracking()
                .Where(v => v.Estado == PoliticasEstados.Publicada)
                .OrderByDescending(v => v.FechaPublicacion)
                .FirstOrDefaultAsync();

            if (versionActiva == null)
            {
                return new PoliticasEstadoDto
                {
                    RequiereAceptacion = false,
                    EsAdministrador = esAdmin,
                    VersionActiva = null
                };
            }

            var aceptada = await _ctx.PoliticasAceptacion
                .AsNoTracking()
                .AnyAsync(a => a.IdEmpresa == idEmpresa && a.IdVersion == versionActiva.IdVersion);

            return new PoliticasEstadoDto
            {
                RequiereAceptacion = !aceptada,
                EsAdministrador = esAdmin,
                VersionActiva = MapVersion(versionActiva)
            };
        }

        public async Task<AceptarPoliticasResultado> AceptarAsync(AceptarPoliticasRequest request)
        {
            if (request == null)
                return Fail("Solicitud inválida.");

            var usuario = await _ctx.Usuarios
                .Include(u => u.Empleado)
                .Include(u => u.Perfil)
                .Include(u => u.Empresa)
                .FirstOrDefaultAsync(u =>
                    u.IdUsuario == request.IdUsuario &&
                    u.IdEmpresa == request.IdEmpresa);

            if (usuario == null)
                return Fail("Usuario no encontrado en la empresa.");

            if (!EsAdministradorUsuario(usuario))
                return Fail("Solo el administrador de la empresa puede aceptar las políticas.");

            var version = await _ctx.PoliticasVersion
                .FirstOrDefaultAsync(v => v.IdVersion == request.IdVersion);

            if (version == null)
                return Fail("Versión no encontrada.");

            if (version.Estado != PoliticasEstados.Publicada)
                return Fail("Solo se puede aceptar la versión publicada vigente.");

            var yaAceptada = await _ctx.PoliticasAceptacion
                .AnyAsync(a => a.IdEmpresa == request.IdEmpresa && a.IdVersion == request.IdVersion);

            if (yaAceptada)
                return Fail("Esta empresa ya aceptó esta versión de las políticas.");

            var empresa = await _ctx.Empresas
                .FirstOrDefaultAsync(e => e.IdEmpresa == request.IdEmpresa);

            if (empresa == null)
                return Fail("Empresa no encontrada.");

            var aceptacion = new PoliticasAceptacion
            {
                IdVersion = version.IdVersion,
                IdEmpresa = request.IdEmpresa,
                IdUsuario = request.IdUsuario,
                FechaAceptacion = DateTime.Now,
                DireccionIp = Truncate(request.DireccionIp, 64),
                Navegador = Truncate(request.Navegador, 500),
                SistemaOperativo = Truncate(request.SistemaOperativo, 200),
                CorreoEnviado = false
            };

            _ctx.PoliticasAceptacion.Add(aceptacion);

            empresa.PoliticasAceptadas = true;
            empresa.Politicas = version.NumeroVersion;

            await _ctx.SaveChangesAsync();

            var correoEnviado = false;
            try
            {
                correoEnviado = EnviarCorreoAceptacion(empresa, usuario, version, aceptacion);
                if (correoEnviado)
                {
                    aceptacion.CorreoEnviado = true;
                    aceptacion.FechaCorreoEnviado = DateTime.Now;
                    await _ctx.SaveChangesAsync();
                }
            }
            catch
            {
                // La aceptación ya quedó registrada; el correo es best-effort.
            }

            return new AceptarPoliticasResultado
            {
                Exitoso = true,
                Mensaje = "Políticas aceptadas correctamente.",
                IdAceptacion = aceptacion.IdAceptacion,
                CorreoEnviado = correoEnviado
            };
        }

        public async Task<List<PoliticasVersionDto>> ListarVersionesAsync()
        {
            var list = await _ctx.PoliticasVersion
                .AsNoTracking()
                .OrderByDescending(v => v.FechaCreacion)
                .ToListAsync();

            return list.Select(MapVersion).ToList();
        }

        public async Task<PoliticasVersionDto?> ObtenerVersionAsync(int idVersion)
        {
            var v = await _ctx.PoliticasVersion
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdVersion == idVersion);

            return v == null ? null : MapVersion(v);
        }

        public async Task<PoliticasVersionDto> CrearBorradorAsync(CrearPoliticasVersionRequest request)
        {
            if (request == null)
                throw new InvalidOperationException("Solicitud inválida.");

            if (string.IsNullOrWhiteSpace(request.NumeroVersion))
                throw new InvalidOperationException("El número de versión es obligatorio.");

            if (string.IsNullOrWhiteSpace(request.Titulo))
                throw new InvalidOperationException("El título es obligatorio.");

            if (string.IsNullOrWhiteSpace(request.Contenido))
                throw new InvalidOperationException("El contenido es obligatorio.");

            var existe = await _ctx.PoliticasVersion
                .AnyAsync(v => v.NumeroVersion == request.NumeroVersion.Trim());

            if (existe)
                throw new InvalidOperationException($"Ya existe la versión {request.NumeroVersion}.");

            var entity = new PoliticasVersion
            {
                NumeroVersion = request.NumeroVersion.Trim(),
                Titulo = request.Titulo.Trim(),
                Contenido = request.Contenido,
                Estado = PoliticasEstados.Borrador,
                FechaCreacion = DateTime.Now,
                IdUsuarioCreacion = request.IdUsuario,
                Notas = Truncate(request.Notas, 500)
            };

            _ctx.PoliticasVersion.Add(entity);
            await _ctx.SaveChangesAsync();

            return MapVersion(entity);
        }

        public async Task<PoliticasVersionDto> PublicarAsync(PublicarPoliticasRequest request)
        {
            if (request == null)
                throw new InvalidOperationException("Solicitud inválida.");

            var version = await _ctx.PoliticasVersion
                .FirstOrDefaultAsync(v => v.IdVersion == request.IdVersion);

            if (version == null)
                throw new InvalidOperationException("Versión no encontrada.");

            if (version.Estado == PoliticasEstados.Publicada)
                throw new InvalidOperationException("Esta versión ya está publicada.");

            if (version.Estado == PoliticasEstados.Archivada)
                throw new InvalidOperationException("No se puede publicar una versión archivada. Cree una nueva versión.");

            if (version.Estado != PoliticasEstados.Borrador)
                throw new InvalidOperationException("Solo se pueden publicar borradores.");

            var publicadas = await _ctx.PoliticasVersion
                .Where(v => v.Estado == PoliticasEstados.Publicada)
                .ToListAsync();

            foreach (var p in publicadas)
            {
                p.Estado = PoliticasEstados.Archivada;
            }

            version.Estado = PoliticasEstados.Publicada;
            version.FechaPublicacion = DateTime.Now;
            version.IdUsuarioPublicacion = request.IdUsuario;

            await _ctx.SaveChangesAsync();

            return MapVersion(version);
        }

        public async Task<List<PoliticasAceptacionDto>> ListarAceptacionesAsync(int? idVersion = null, int? idEmpresa = null)
        {
            var q = from a in _ctx.PoliticasAceptacion.AsNoTracking()
                    join v in _ctx.PoliticasVersion.AsNoTracking() on a.IdVersion equals v.IdVersion
                    join e in _ctx.Empresas.AsNoTracking() on a.IdEmpresa equals e.IdEmpresa
                    join u in _ctx.Usuarios.AsNoTracking() on a.IdUsuario equals u.IdUsuario
                    join emp in _ctx.EmpleadosP.AsNoTracking() on u.IdEmpleado equals emp.IdEmpleados into empJoin
                    from emp in empJoin.DefaultIfEmpty()
                    select new { a, v, e, u, emp };

            if (idVersion.HasValue)
                q = q.Where(x => x.a.IdVersion == idVersion.Value);

            if (idEmpresa.HasValue)
                q = q.Where(x => x.a.IdEmpresa == idEmpresa.Value);

            var rows = await q
                .OrderByDescending(x => x.a.FechaAceptacion)
                .ToListAsync();

            return rows.Select(x => new PoliticasAceptacionDto
            {
                IdAceptacion = x.a.IdAceptacion,
                IdVersion = x.a.IdVersion,
                NumeroVersion = x.v.NumeroVersion,
                TituloVersion = x.v.Titulo,
                IdEmpresa = x.a.IdEmpresa,
                NombreEmpresa = x.e.NombreComercial,
                IdUsuario = x.a.IdUsuario,
                NombreUsuario = !string.IsNullOrWhiteSpace(x.emp?.Nombre)
                    ? x.emp!.Nombre.Trim()
                    : x.u.UserName,
                FechaAceptacion = x.a.FechaAceptacion,
                DireccionIp = x.a.DireccionIp,
                Navegador = x.a.Navegador,
                SistemaOperativo = x.a.SistemaOperativo,
                CorreoEnviado = x.a.CorreoEnviado,
                FechaCorreoEnviado = x.a.FechaCorreoEnviado
            }).ToList();
        }

        private bool EnviarCorreoAceptacion(
            Empresas empresa,
            Usuarios usuario,
            PoliticasVersion version,
            PoliticasAceptacion aceptacion)
        {
            var destinatarios = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(empresa.CorreElectronico))
                destinatarios.Add(empresa.CorreElectronico.Trim());

            if (!string.IsNullOrWhiteSpace(usuario.Correo))
                destinatarios.Add(usuario.Correo.Trim());

            if (destinatarios.Count == 0)
                return false;

            var contenidoHtml = FormatearContenidoHtml(version.Contenido);

            var body = $@"
                <div style=""font-family:Segoe UI,Arial,sans-serif;max-width:720px;margin:0 auto;color:#111827;"">
                <h2 style=""margin:0 0 8px;font-size:20px;"">{WebUtility.HtmlEncode(version.Titulo)}</h2>
                <p style=""margin:0 0 4px;color:#6b7280;font-size:13px;""><strong>Versión:</strong> {WebUtility.HtmlEncode(version.NumeroVersion)}</p>
                <p style=""margin:0 0 4px;color:#6b7280;font-size:13px;""><strong>Empresa:</strong> {WebUtility.HtmlEncode(empresa.NombreComercial ?? "")}</p>
                <p style=""margin:0 0 4px;color:#6b7280;font-size:13px;""><strong>Fecha de aceptación:</strong> {aceptacion.FechaAceptacion:dd/MM/yyyy HH:mm}</p>
                <p style=""margin:0 0 16px;color:#6b7280;font-size:13px;""><strong>Usuario:</strong> {WebUtility.HtmlEncode(usuario.UserName ?? "")}</p>
                <div style=""border-top:1px solid #e5e7eb;padding-top:16px;font-size:14px;line-height:1.6;color:#374151;"">
                {contenidoHtml}
                </div>
                <p style=""margin-top:20px;color:#6b7280;font-size:12px;border-top:1px solid #e5e7eb;padding-top:12px;"">
                Este correo confirma la aceptación de las Políticas del Servicio de MacroBits SRL / Alahia ERP.
                Conserve este mensaje para su registro.
                </p>
                </div>";

            foreach (var to in destinatarios)
            {
                Utility.Send(
                    "smtp.gmail.com",
                    587,
                    true,
                    "ing.joelarielsosa@gmail.com",
                    "wrcsdhewqdgrtula",
                    "MacroBits Software",
                    to,
                    $"Políticas del Servicio aceptadas - v{version.NumeroVersion}",
                    body
                );
            }

            return true;
        }

        private static string FormatearContenidoHtml(string contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return "";

            var sb = new System.Text.StringBuilder();
            var lineas = contenido.Replace("\r\n", "\n").Split('\n');
            var enLista = false;
            var parrafo = new System.Text.StringBuilder();

            void FlushParrafo()
            {
                if (parrafo.Length == 0) return;
                sb.Append("<p style=\"margin:0 0 12px;\">")
                  .Append(WebUtility.HtmlEncode(parrafo.ToString().Trim()))
                  .Append("</p>");
                parrafo.Clear();
            }

            void CloseLista()
            {
                if (!enLista) return;
                sb.Append("</ul>");
                enLista = false;
            }

            foreach (var raw in lineas)
            {
                var linea = raw.Trim();
                if (string.IsNullOrEmpty(linea) || System.Text.RegularExpressions.Regex.IsMatch(linea, @"^[-–—=]{3,}\s*$"))
                {
                    CloseLista();
                    FlushParrafo();
                    continue;
                }

                if (linea.StartsWith("•") || linea.StartsWith("- ") || linea.StartsWith("* "))
                {
                    FlushParrafo();
                    if (!enLista)
                    {
                        sb.Append("<ul style=\"margin:0 0 14px;padding-left:20px;\">");
                        enLista = true;
                    }
                    var item = System.Text.RegularExpressions.Regex.Replace(linea, @"^[•\-*]\s*", "").Trim();
                    sb.Append("<li style=\"margin-bottom:4px;\">")
                      .Append(WebUtility.HtmlEncode(item))
                      .Append("</li>");
                    continue;
                }

                if (System.Text.RegularExpressions.Regex.IsMatch(linea, @"^\d+\.\s+\S"))
                {
                    CloseLista();
                    FlushParrafo();
                    sb.Append("<h3 style=\"margin:18px 0 8px;font-size:15px;font-weight:700;color:#0f172a;\">")
                      .Append(WebUtility.HtmlEncode(linea))
                      .Append("</h3>");
                    continue;
                }

                CloseLista();
                if (parrafo.Length > 0) parrafo.Append(' ');
                parrafo.Append(linea);
            }

            CloseLista();
            FlushParrafo();
            return sb.ToString();
        }

        private static PoliticasVersionDto MapVersion(PoliticasVersion v) => new()
        {
            IdVersion = v.IdVersion,
            NumeroVersion = v.NumeroVersion,
            Titulo = v.Titulo,
            Contenido = v.Contenido,
            Estado = v.Estado,
            FechaCreacion = v.FechaCreacion,
            IdUsuarioCreacion = v.IdUsuarioCreacion,
            FechaPublicacion = v.FechaPublicacion,
            IdUsuarioPublicacion = v.IdUsuarioPublicacion,
            Notas = v.Notas
        };

        private static AceptarPoliticasResultado Fail(string mensaje) => new()
        {
            Exitoso = false,
            Mensaje = mensaje
        };

        private static string? Truncate(string? value, int max)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            return value.Length <= max ? value : value.Substring(0, max);
        }
    }
}
