using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PrinterLibrary;

namespace AlahiaPos.DataAccess.Servicios.Notificaciones
{
    public class NotificacionCentroService : INotificacionCentro
    {
        private readonly AlahiaPosContext _ctx;
        private readonly IEnumerable<INotificacionCanal> _canales;
        private readonly INotificacionRealtime _realtime;
        private readonly ILogger<NotificacionCentroService> _logger;

        private static readonly Dictionary<string, NotificacionPolitica> Politicas = new(StringComparer.OrdinalIgnoreCase)
        {
            [NotificacionTipos.TicketNuevo] = new() { Email = true },
            [NotificacionTipos.TicketNuevoMensaje] = new() { Email = true },
            [NotificacionTipos.TicketEstadoCambiado] = new() { Email = true },
            [NotificacionTipos.PagoPendiente] = new() { Email = true },
            [NotificacionTipos.PagoAprobado] = new() { Email = true },
            [NotificacionTipos.PagoRechazado] = new() { Email = true },
            [NotificacionTipos.ServicioSuspendido] = new() { Email = true },
            [NotificacionTipos.CambioPlan] = new() { Email = true },
            [NotificacionTipos.EcfAceptado] = new() { Email = false },
            [NotificacionTipos.EcfRechazado] = new() { Email = true },
            [NotificacionTipos.InventarioBajo] = new() { Email = false },
            [NotificacionTipos.CxcVencida] = new() { Email = false },
            [NotificacionTipos.CompraPendiente] = new() { Email = false },
            [NotificacionTipos.AvisoAdministrativo] = new() { Email = true },
            [NotificacionTipos.PedidoDeliveryAsignado] = new() { Email = false },
            [NotificacionTipos.CierreCaja] = new() { Email = true },
        };

        public NotificacionCentroService(
            AlahiaPosContext ctx,
            IEnumerable<INotificacionCanal> canales,
            INotificacionRealtime realtime,
            ILogger<NotificacionCentroService> logger)
        {
            _ctx = ctx;
            _canales = canales;
            _realtime = realtime;
            _logger = logger;
        }

        public async Task<NotificacionDto> PublicarAsync(NotificacionEvento evento, CancellationToken ct = default)
        {
            if (evento.IdEmpresa <= 0) throw new Exception("IdEmpresa inválido para notificación.");
            if (string.IsNullOrWhiteSpace(evento.Tipo)) throw new Exception("Tipo de notificación requerido.");
            if (string.IsNullOrWhiteSpace(evento.Titulo)) throw new Exception("Título requerido.");

            var destino = ResolverDestino(evento);
            var prioridad = ResolverPrioridad(evento.Prioridad);

            var entity = new Notificacion
            {
                IdEmpresa = evento.IdEmpresa,
                DestinoTipo = destino,
                IdUsuarioDestino = destino == NotificacionDestinos.Usuario ? evento.IdUsuarioDestino : null,
                IdRolDestino = destino == NotificacionDestinos.Rol ? evento.IdRolDestino : null,
                RolCodigo = destino == NotificacionDestinos.Rol ? Trunc(evento.RolCodigo, 60) : null,
                Tipo = evento.Tipo.Trim().ToUpperInvariant(),
                Prioridad = prioridad,
                Titulo = Trunc(evento.Titulo, 200)!,
                Mensaje = Trunc(evento.Mensaje ?? "", 500)!,
                Ruta = Trunc(evento.Ruta, 200),
                ReferenciaTipo = Trunc(evento.ReferenciaTipo, 60),
                ReferenciaId = evento.ReferenciaId,
                MetadataJson = evento.MetadataJson,
                Leida = false,
                Archivada = false,
                FechaCreacion = DateTime.Now
            };

            _ctx.Notificaciones.Add(entity);
            await _ctx.SaveChangesAsync(ct);

            var dto = Map(entity);

            try
            {
                await _realtime.EmitirNuevaAsync(dto, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SignalR emit falló para notificación {Id}", dto.IdNotificacion);
            }

            Politicas.TryGetValue(entity.Tipo, out var politica);
            politica ??= new NotificacionPolitica { Email = true };

            foreach (var canal in _canales)
            {
                var debe = canal.Canal switch
                {
                    NotificacionCanales.InApp => false,
                    NotificacionCanales.Email => politica.Email,
                    NotificacionCanales.Push => politica.Push,
                    NotificacionCanales.WhatsApp => politica.WhatsApp,
                    _ => false
                };
                if (!debe) continue;

                try
                {
                    await canal.EnviarAsync(evento, dto, ct);
                    _ctx.NotificacionCanalLog.Add(new NotificacionCanalLog
                    {
                        IdNotificacion = entity.IdNotificacion,
                        Canal = canal.Canal,
                        FechaEnvio = DateTime.Now,
                        Exito = true
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Canal {Canal} falló para notificación {Id}", canal.Canal, entity.IdNotificacion);
                    _ctx.NotificacionCanalLog.Add(new NotificacionCanalLog
                    {
                        IdNotificacion = entity.IdNotificacion,
                        Canal = canal.Canal,
                        FechaEnvio = DateTime.Now,
                        Exito = false,
                        Detalle = Trunc(ex.Message, 500)
                    });
                }
            }

            try { await _ctx.SaveChangesAsync(ct); }
            catch { /* log no crítico */ }

            return dto;
        }

        public async Task<List<NotificacionDto>> ListarAsync(
            int idEmpresa, int? idUsuario, bool soloNoLeidas = false, int top = 50, bool incluirArchivadas = false)
        {
            var q = QueryVisible(idEmpresa, idUsuario, incluirArchivadas);

            if (soloNoLeidas)
                q = q.Where(n => !n.Leida);

            var rows = await q.OrderByDescending(n => n.FechaCreacion).Take(Math.Clamp(top, 1, 200)).ToListAsync();
            return rows.Select(Map).ToList();
        }

        public async Task<int> ContarNoLeidasAsync(int idEmpresa, int? idUsuario)
        {
            return await QueryVisible(idEmpresa, idUsuario, incluirArchivadas: false)
                .Where(n => !n.Leida)
                .CountAsync();
        }

        public async Task MarcarLeidaAsync(int idNotificacion, int idEmpresa, int? idUsuario)
        {
            var n = await ObtenerEditableAsync(idNotificacion, idEmpresa, idUsuario);
            if (n == null) return;
            if (n.Leida) return;
            n.Leida = true;
            n.FechaLeida = DateTime.Now;
            await _ctx.SaveChangesAsync();
            try { await _realtime.EmitirLeidaAsync(idEmpresa, idNotificacion); }
            catch (Exception ex) { _logger.LogWarning(ex, "SignalR leída falló para {Id}", idNotificacion); }
        }

        public async Task MarcarNoLeidaAsync(int idNotificacion, int idEmpresa, int? idUsuario)
        {
            var n = await ObtenerEditableAsync(idNotificacion, idEmpresa, idUsuario);
            if (n == null) return;
            n.Leida = false;
            n.FechaLeida = null;
            await _ctx.SaveChangesAsync();
        }

        public async Task MarcarTodasAsync(int idEmpresa, int? idUsuario)
        {
            var list = await QueryVisibleTracked(idEmpresa, idUsuario)
                .Where(n => !n.Leida)
                .ToListAsync();
            var ahora = DateTime.Now;
            foreach (var n in list)
            {
                n.Leida = true;
                n.FechaLeida = ahora;
            }
            await _ctx.SaveChangesAsync();
            foreach (var n in list)
            {
                try { await _realtime.EmitirLeidaAsync(idEmpresa, n.IdNotificacion); }
                catch (Exception ex) { _logger.LogWarning(ex, "SignalR leída falló para {Id}", n.IdNotificacion); }
            }
        }

        public async Task ArchivarAsync(int idNotificacion, int idEmpresa, int? idUsuario)
        {
            var n = await ObtenerEditableAsync(idNotificacion, idEmpresa, idUsuario);
            if (n == null) return;
            n.Archivada = true;
            n.FechaArchivada = DateTime.Now;
            var eraNoLeida = !n.Leida;
            n.Leida = true;
            n.FechaLeida ??= DateTime.Now;
            await _ctx.SaveChangesAsync();
            if (eraNoLeida)
            {
                try { await _realtime.EmitirLeidaAsync(idEmpresa, idNotificacion); }
                catch (Exception ex) { _logger.LogWarning(ex, "SignalR leída falló para {Id}", idNotificacion); }
            }
        }

        private IQueryable<Notificacion> QueryVisible(int idEmpresa, int? idUsuario, bool incluirArchivadas)
        {
            var q = _ctx.Notificaciones.AsNoTracking()
                .Where(n => n.IdEmpresa == idEmpresa);

            if (!incluirArchivadas)
                q = q.Where(n => !n.Archivada);

            if (idUsuario.HasValue && idUsuario > 0)
            {
                // EMPRESA (todos) + USUARIO (propio). ROL se filtrará cuando exista mapeo perfil.
                q = q.Where(n =>
                    n.DestinoTipo == NotificacionDestinos.Empresa
                    || (n.DestinoTipo == NotificacionDestinos.Usuario && n.IdUsuarioDestino == idUsuario)
                    || (n.DestinoTipo == NotificacionDestinos.Rol)); // futuro: filtrar por perfil del usuario
            }

            return q;
        }

        private IQueryable<Notificacion> QueryVisibleTracked(int idEmpresa, int? idUsuario)
        {
            var q = _ctx.Notificaciones.AsTracking()
                .Where(n => n.IdEmpresa == idEmpresa && !n.Archivada);

            if (idUsuario.HasValue && idUsuario > 0)
            {
                q = q.Where(n =>
                    n.DestinoTipo == NotificacionDestinos.Empresa
                    || (n.DestinoTipo == NotificacionDestinos.Usuario && n.IdUsuarioDestino == idUsuario)
                    || n.DestinoTipo == NotificacionDestinos.Rol);
            }

            return q;
        }

        private async Task<Notificacion?> ObtenerEditableAsync(int idNotificacion, int idEmpresa, int? idUsuario)
        {
            var n = await _ctx.Notificaciones.AsTracking()
                .FirstOrDefaultAsync(x => x.IdNotificacion == idNotificacion && x.IdEmpresa == idEmpresa);
            if (n == null) return null;

            if (idUsuario.HasValue && idUsuario > 0
                && n.DestinoTipo == NotificacionDestinos.Usuario
                && n.IdUsuarioDestino != idUsuario)
                return null;

            return n;
        }

        private static string ResolverDestino(NotificacionEvento e)
        {
            var raw = (e.DestinoTipo ?? "").Trim().ToUpperInvariant();
            if (raw is "USUARIO" or "EMPRESA" or "ROL") return raw;
            if (e.IdUsuarioDestino.HasValue && e.IdUsuarioDestino > 0) return NotificacionDestinos.Usuario;
            if (e.IdRolDestino.HasValue || !string.IsNullOrWhiteSpace(e.RolCodigo)) return NotificacionDestinos.Rol;
            return NotificacionDestinos.Empresa;
        }

        private static string ResolverPrioridad(string? p)
        {
            var v = (p ?? NotificacionPrioridades.Info).Trim().ToUpperInvariant();
            return v switch
            {
                "INFO" or "ADVERTENCIA" or "ERROR" or "EXITO" => v,
                "WARNING" => NotificacionPrioridades.Advertencia,
                "SUCCESS" => NotificacionPrioridades.Exito,
                _ => NotificacionPrioridades.Info
            };
        }

        private static NotificacionDto Map(Notificacion n) => new()
        {
            IdNotificacion = n.IdNotificacion,
            IdEmpresa = n.IdEmpresa,
            DestinoTipo = n.DestinoTipo,
            IdUsuarioDestino = n.IdUsuarioDestino,
            IdRolDestino = n.IdRolDestino,
            RolCodigo = n.RolCodigo,
            Tipo = n.Tipo,
            Prioridad = n.Prioridad,
            Titulo = n.Titulo,
            Mensaje = n.Mensaje,
            Ruta = n.Ruta,
            ReferenciaTipo = n.ReferenciaTipo,
            ReferenciaId = n.ReferenciaId,
            Leida = n.Leida,
            Archivada = n.Archivada,
            FechaCreacion = n.FechaCreacion
        };

        private static string? Trunc(string? s, int max)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim();
            return s.Length <= max ? s : s[..max];
        }
    }

    public class EmailNotificacionCanal : INotificacionCanal
    {
        private readonly AlahiaPosContext _ctx;
        public string Canal => NotificacionCanales.Email;

        public EmailNotificacionCanal(AlahiaPosContext ctx) => _ctx = ctx;

        public async Task EnviarAsync(NotificacionEvento evento, NotificacionDto creada, CancellationToken ct = default)
        {
            var to = evento.CorreoDestino;
            if (string.IsNullOrWhiteSpace(to))
            {
                to = await _ctx.Empresas.AsNoTracking()
                    .Where(e => e.IdEmpresa == evento.IdEmpresa)
                    .Select(e => e.CorreElectronico)
                    .FirstOrDefaultAsync(ct);
            }
            if (string.IsNullOrWhiteSpace(to)) return;

            var destinatarios = to
                .Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => x.Contains('@'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (destinatarios.Count == 0) return;

            var mensajeHtml = string.Join("<br/>",
                (evento.Mensaje ?? string.Empty)
                    .Replace("\r\n", "\n")
                    .Split('\n')
                    .Select(WebUtility.HtmlEncode));

            var empresaHeader = string.IsNullOrWhiteSpace(evento.NombreEmpresa)
                ? ""
                : $@"<p style=""margin:6px 0 0;color:#e2e8f0;font-size:0.95rem;"">{WebUtility.HtmlEncode(evento.NombreEmpresa)}</p>";

            var body = $@"
              <div style=""font-family:Segoe UI,Arial,sans-serif;max-width:640px;color:#0f172a;"">
                <div style=""background:linear-gradient(135deg,#2F80ED,#174A70);padding:16px 20px;border-radius:12px 12px 0 0;border-bottom:3px solid #F2C94C;"">
                  <h2 style=""margin:0;color:#fff;font-size:1.15rem;"">{WebUtility.HtmlEncode(evento.Titulo)}</h2>
                  {empresaHeader}
                </div>
                <div style=""border:1px solid #e2e8f0;border-top:none;border-radius:0 0 12px 12px;padding:18px 20px;background:#fff;"">
                  <p style=""line-height:1.5;white-space:pre-wrap;font-family:Consolas,monospace;font-size:14px;"">{mensajeHtml}</p>
                  <p style=""margin-top:1.25rem;color:#64748b;"">— Alahia ERP</p>
                </div>
              </div>";

            foreach (var dest in destinatarios)
            {
                Utility.Send(
                    "smtp.gmail.com",
                    587,
                    true,
                    "ing.joelarielsosa@gmail.com",
                    "wrcsdhewqdgrtula",
                    "Alahia ERP",
                    dest,
                    evento.Titulo,
                    body);
            }
        }
    }

    public class PushNotificacionCanalStub : INotificacionCanal
    {
        private readonly ILogger<PushNotificacionCanalStub> _logger;
        public string Canal => NotificacionCanales.Push;
        public PushNotificacionCanalStub(ILogger<PushNotificacionCanalStub> logger) => _logger = logger;

        public Task EnviarAsync(NotificacionEvento evento, NotificacionDto creada, CancellationToken ct = default)
        {
            _logger.LogInformation("[Push stub] {Tipo} empresa {Id}: {Titulo}", evento.Tipo, evento.IdEmpresa, evento.Titulo);
            return Task.CompletedTask;
        }
    }

    public class WhatsAppNotificacionCanalStub : INotificacionCanal
    {
        private readonly ILogger<WhatsAppNotificacionCanalStub> _logger;
        public string Canal => NotificacionCanales.WhatsApp;
        public WhatsAppNotificacionCanalStub(ILogger<WhatsAppNotificacionCanalStub> logger) => _logger = logger;

        public Task EnviarAsync(NotificacionEvento evento, NotificacionDto creada, CancellationToken ct = default)
        {
            _logger.LogInformation("[WhatsApp stub] {Tipo} empresa {Id}: {Titulo}", evento.Tipo, evento.IdEmpresa, evento.Titulo);
            return Task.CompletedTask;
        }
    }
}
