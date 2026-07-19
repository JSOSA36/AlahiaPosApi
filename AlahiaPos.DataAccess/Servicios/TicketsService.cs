using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PrinterLibrary;

namespace AlahiaPos.DataAccess.Servicios
{
    public class TicketsService : ITicketsService
    {
        private const string CorreoAdminFallback = "ing.joelarielsosa@gmail.com";
        private const long MaxArchivoBytes = 25 * 1024 * 1024; // 25 MB

        private static readonly HashSet<string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp",
            ".pdf",
            ".doc", ".docx",
            ".xls", ".xlsx", ".csv",
            ".mp4", ".webm", ".mov", ".avi"
        };

        private readonly AlahiaPosContext _ctx;
        private readonly INotificacionCentro _notificaciones;

        public TicketsService(AlahiaPosContext ctx, INotificacionCentro notificaciones)
        {
            _ctx = ctx;
            _notificaciones = notificaciones;
        }

        public async Task<TicketDetalleDto> CrearAsync(CrearTicketDto dto)
        {
            if (dto.IdEmpresa <= 0) throw new Exception("IdEmpresa inválido.");
            if (dto.IdUsuarioCrea <= 0) throw new Exception("IdUsuario inválido.");
            if (string.IsNullOrWhiteSpace(dto.Asunto)) throw new Exception("El asunto es obligatorio.");
            if (string.IsNullOrWhiteSpace(dto.Descripcion)) throw new Exception("La descripción es obligatoria.");
            if (dto.Archivos == null || !dto.Archivos.Any(f => f != null && f.Length > 0))
                throw new Exception("Debe adjuntar al menos una imagen o archivo como evidencia.");

            var empresa = await _ctx.Empresas.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == dto.IdEmpresa)
                ?? throw new Exception("Empresa no encontrada.");

            if (empresa.EsEmpresaSistema)
                throw new Exception("La empresa sistema no crea tickets de cliente.");

            var prioridad = NormalizarPrioridad(dto.Prioridad);
            var categoria = NormalizarCategoria(dto.Categoria);
            var numero = await GenerarNumeroAsync();
            var ahora = DateTime.Now;
            var (horasMin, horasMax) = await CalcularRangoEstimadoAsync();

            var ticket = new Ticket
            {
                Numero = numero,
                IdEmpresa = dto.IdEmpresa,
                IdUsuarioCrea = dto.IdUsuarioCrea,
                NombreCliente = string.IsNullOrWhiteSpace(dto.NombreCliente)
                    ? empresa.NombreComercial
                    : dto.NombreCliente.Trim(),
                Asunto = dto.Asunto.Trim(),
                Descripcion = dto.Descripcion.Trim(),
                Categoria = categoria,
                Prioridad = prioridad,
                Estado = TicketEstados.Abierto,
                VersionSistema = Trunc(dto.VersionSistema, 40),
                Dispositivo = Trunc(dto.Dispositivo, 80),
                Navegador = Trunc(dto.Navegador, 120),
                SistemaOperativo = Trunc(dto.SistemaOperativo, 120),
                HorasEstimadasMin = horasMin,
                HorasEstimadasMax = horasMax,
                FechaEstimadaResolucion = ahora.AddHours(horasMax),
                FechaCreacion = ahora,
                FechaActualizacion = ahora
            };

            _ctx.Tickets.Add(ticket);
            await _ctx.SaveChangesAsync();

            var mensajeInicial = new TicketMensaje
            {
                IdTicket = ticket.IdTicket,
                IdUsuario = dto.IdUsuarioCrea,
                EsRespuestaAdmin = false,
                Mensaje = ticket.Descripcion,
                FechaCreacion = DateTime.Now
            };
            _ctx.TicketMensajes.Add(mensajeInicial);
            await _ctx.SaveChangesAsync();

            await GuardarAdjuntosAsync(ticket.IdTicket, mensajeInicial.IdMensaje, dto.Archivos);

            var idEmpresaAdmin = await ObtenerIdEmpresaSistemaAsync();
            if (idEmpresaAdmin > 0)
            {
                await _notificaciones.PublicarAsync(new NotificacionEvento
                {
                    Tipo = NotificacionTipos.TicketNuevo,
                    IdEmpresa = idEmpresaAdmin,
                    DestinoTipo = NotificacionDestinos.Empresa,
                    Prioridad = NotificacionPrioridades.Advertencia,
                    Titulo = $"Nuevo ticket {ticket.Numero}",
                    Mensaje = $"{empresa.NombreComercial}: {ticket.Asunto} ({ticket.Prioridad})",
                    Ruta = "/tickets-admin",
                    ReferenciaTipo = "Ticket",
                    ReferenciaId = ticket.IdTicket,
                    CorreoDestino = await ObtenerCorreoAdminAsync(),
                    NombreEmpresa = empresa.NombreComercial
                });
            }

            return await ObtenerAsync(ticket.IdTicket, dto.IdEmpresa);
        }

        public async Task<TicketDetalleDto> CrearDesdeLoginAsync(CrearTicketDesdeLoginDto dto)
        {
            if (dto == null) throw new Exception("Solicitud inválida.");
            if (string.IsNullOrWhiteSpace(dto.UserName)) throw new Exception("El usuario es obligatorio.");
            if (string.IsNullOrWhiteSpace(dto.Password)) throw new Exception("La contraseña es obligatoria.");
            if (string.IsNullOrWhiteSpace(dto.Asunto)) throw new Exception("El asunto es obligatorio.");
            if (string.IsNullOrWhiteSpace(dto.Descripcion)) throw new Exception("La descripción es obligatoria.");

            var userName = dto.UserName.Trim();
            var usuario = await _ctx.Usuarios.AsNoTracking()
                .Include(u => u.Empleado)
                .FirstOrDefaultAsync(u => u.UserName == userName && u.Estado)
                ?? throw new Exception("Usuario o contraseña inválidos");

            var passwordHash = Utility.EncriptarPassword(dto.Password);
            if (!string.Equals(usuario.PasswordHash, passwordHash, StringComparison.Ordinal))
                throw new Exception("Usuario o contraseña inválidos");

            var empresa = await _ctx.Empresas.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == usuario.IdEmpresa)
                ?? throw new Exception("Empresa no encontrada.");

            if (empresa.EsEmpresaSistema)
                throw new Exception("La empresa sistema no crea tickets de cliente.");

            var crear = new CrearTicketDto
            {
                IdEmpresa = usuario.IdEmpresa,
                IdUsuarioCrea = usuario.IdUsuario,
                NombreCliente = string.IsNullOrWhiteSpace(usuario.Empleado?.Nombre)
                    ? empresa.NombreComercial
                    : usuario.Empleado!.Nombre!.Trim(),
                Asunto = dto.Asunto,
                Descripcion = dto.Descripcion,
                Categoria = string.IsNullOrWhiteSpace(dto.Categoria) ? "Acceso al Sistema" : dto.Categoria,
                Prioridad = string.IsNullOrWhiteSpace(dto.Prioridad) ? "ALTA" : dto.Prioridad,
                VersionSistema = dto.VersionSistema,
                Dispositivo = dto.Dispositivo,
                Navegador = dto.Navegador,
                SistemaOperativo = dto.SistemaOperativo,
                Archivos = dto.Archivos
            };

            return await CrearAsync(crear);
        }

        public async Task<List<TicketListItemDto>> ListarPorEmpresaAsync(int idEmpresa)
        {
            var q = BaseListQuery().Where(t => t.IdEmpresa == idEmpresa);
            var list = await MaterializarListaAsync(q.OrderByDescending(t => t.FechaActualizacion));
            return list;
        }

        public async Task<List<TicketListItemDto>> ListarAdminAsync(TicketFiltroAdminDto filtro)
        {
            filtro ??= new TicketFiltroAdminDto();
            var q = BaseListQuery();

            if (filtro.IdEmpresa.HasValue && filtro.IdEmpresa > 0)
                q = q.Where(t => t.IdEmpresa == filtro.IdEmpresa.Value);

            if (!string.IsNullOrWhiteSpace(filtro.Estado))
                q = q.Where(t => t.Estado == filtro.Estado.Trim().ToUpperInvariant());

            if (!string.IsNullOrWhiteSpace(filtro.Prioridad))
                q = q.Where(t => t.Prioridad == filtro.Prioridad.Trim().ToUpperInvariant());

            if (!string.IsNullOrWhiteSpace(filtro.Categoria))
                q = q.Where(t => t.Categoria == filtro.Categoria.Trim());

            if (filtro.FechaDesde.HasValue)
                q = q.Where(t => t.FechaCreacion >= filtro.FechaDesde.Value.Date);

            if (filtro.FechaHasta.HasValue)
            {
                var hasta = filtro.FechaHasta.Value.Date.AddDays(1);
                q = q.Where(t => t.FechaCreacion < hasta);
            }

            if (!string.IsNullOrWhiteSpace(filtro.Q))
            {
                var term = filtro.Q.Trim();
                q = q.Where(t =>
                    t.Numero.Contains(term)
                    || t.Asunto.Contains(term)
                    || (t.NombreCliente != null && t.NombreCliente.Contains(term))
                    || _ctx.Empresas.Any(e => e.IdEmpresa == t.IdEmpresa && e.NombreComercial != null && e.NombreComercial.Contains(term)));
            }

            var orden = (filtro.Orden ?? "recientes").Trim().ToLowerInvariant();
            IQueryable<Ticket> ordenado = orden switch
            {
                "antiguos" => q.OrderBy(t => t.FechaCreacion),
                "prioridad" => q
                    .OrderByDescending(t => t.Prioridad == TicketEstados.PrioridadCritica)
                    .ThenByDescending(t => t.Prioridad == TicketEstados.PrioridadAlta)
                    .ThenByDescending(t => t.Prioridad == TicketEstados.PrioridadMedia)
                    .ThenByDescending(t => t.FechaActualizacion),
                "sin_responder" => q
                    .OrderByDescending(t =>
                        t.Estado != TicketEstados.Cerrado
                        && t.Estado != TicketEstados.Resuelto
                        && (t.FechaUltimaRespuestaAdmin == null
                            || _ctx.TicketMensajes.Any(m =>
                                m.IdTicket == t.IdTicket
                                && !m.EsRespuestaAdmin
                                && (t.FechaUltimaRespuestaAdmin == null || m.FechaCreacion > t.FechaUltimaRespuestaAdmin))))
                    .ThenByDescending(t => t.FechaActualizacion),
                _ => q.OrderByDescending(t => t.FechaActualizacion)
            };

            return await MaterializarListaAsync(ordenado);
        }

        public async Task<TicketDetalleDto> ObtenerAsync(int idTicket, int? idEmpresaCliente)
        {
            var ticket = await _ctx.Tickets.AsNoTracking()
                .FirstOrDefaultAsync(t => t.IdTicket == idTicket)
                ?? throw new Exception("Ticket no encontrado.");

            if (idEmpresaCliente.HasValue && idEmpresaCliente > 0 && ticket.IdEmpresa != idEmpresaCliente.Value)
                throw new Exception("No tiene acceso a este ticket.");

            var empresaNombre = await _ctx.Empresas.AsNoTracking()
                .Where(e => e.IdEmpresa == ticket.IdEmpresa)
                .Select(e => e.NombreComercial)
                .FirstOrDefaultAsync() ?? "";

            var usuarioCrea = await NombreUsuarioAsync(ticket.IdUsuarioCrea);
            var mensajes = await _ctx.TicketMensajes.AsNoTracking()
                .Where(m => m.IdTicket == idTicket)
                .OrderBy(m => m.FechaCreacion)
                .ToListAsync();

            var adjuntos = await _ctx.TicketAdjuntos.AsNoTracking()
                .Where(a => a.IdTicket == idTicket)
                .OrderBy(a => a.FechaCreacion)
                .ToListAsync();

            var msgDtos = new List<TicketMensajeDto>();
            foreach (var m in mensajes)
            {
                msgDtos.Add(new TicketMensajeDto
                {
                    IdMensaje = m.IdMensaje,
                    IdTicket = m.IdTicket,
                    IdUsuario = m.IdUsuario,
                    NombreUsuario = await NombreUsuarioAsync(m.IdUsuario),
                    EsRespuestaAdmin = m.EsRespuestaAdmin,
                    Mensaje = m.Mensaje,
                    FechaCreacion = m.FechaCreacion,
                    Adjuntos = adjuntos
                        .Where(a => a.IdMensaje == m.IdMensaje)
                        .Select(MapAdjunto)
                        .ToList()
                });
            }

            var cant = mensajes.Count;
            return new TicketDetalleDto
            {
                IdTicket = ticket.IdTicket,
                Numero = ticket.Numero,
                IdEmpresa = ticket.IdEmpresa,
                NombreEmpresa = empresaNombre,
                NombreCliente = ticket.NombreCliente,
                Asunto = ticket.Asunto,
                Descripcion = ticket.Descripcion,
                Categoria = ticket.Categoria,
                Prioridad = ticket.Prioridad,
                Estado = ticket.Estado,
                FechaCreacion = ticket.FechaCreacion,
                FechaActualizacion = ticket.FechaActualizacion,
                IdUsuarioCrea = ticket.IdUsuarioCrea,
                UsuarioCrea = usuarioCrea,
                VersionSistema = ticket.VersionSistema,
                Dispositivo = ticket.Dispositivo,
                Navegador = ticket.Navegador,
                SistemaOperativo = ticket.SistemaOperativo,
                FechaResolucion = ticket.FechaResolucion,
                FechaCierre = ticket.FechaCierre,
                FechaUltimaRespuestaAdmin = ticket.FechaUltimaRespuestaAdmin,
                HorasEstimadasMin = ticket.HorasEstimadasMin,
                HorasEstimadasMax = ticket.HorasEstimadasMax,
                FechaEstimadaResolucion = ticket.FechaEstimadaResolucion,
                CantidadMensajes = cant,
                SinResponder = EsSinResponder(ticket, mensajes),
                Mensajes = msgDtos,
                Adjuntos = adjuntos.Where(a => a.IdMensaje == null).Select(MapAdjunto).ToList()
            };
        }

        public async Task<TicketDetalleDto> AgregarMensajeAsync(AgregarTicketMensajeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Mensaje) && (dto.Archivos == null || dto.Archivos.Count == 0))
                throw new Exception("Escriba un mensaje o adjunte un archivo.");

            var ticket = await _ctx.Tickets.AsTracking()
                .FirstOrDefaultAsync(t => t.IdTicket == dto.IdTicket)
                ?? throw new Exception("Ticket no encontrado.");

            var usuario = await _ctx.Usuarios.AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdUsuario == dto.IdUsuario)
                ?? throw new Exception("Usuario no encontrado.");

            var empresaUsuario = await _ctx.Empresas.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == usuario.IdEmpresa)
                ?? throw new Exception("Empresa del usuario no encontrada.");

            var esAdmin = empresaUsuario.EsEmpresaSistema;
            if (!esAdmin && ticket.IdEmpresa != usuario.IdEmpresa)
                throw new Exception("No tiene acceso a este ticket.");

            if (ticket.Estado == TicketEstados.Cerrado && !esAdmin)
                throw new Exception("El ticket está cerrado. Contacte a soporte para reabrirlo.");

            var estadoAnterior = ticket.Estado;

            // Automatización de estados
            if (esAdmin)
            {
                if (ticket.Estado is TicketEstados.Abierto or TicketEstados.Reabierto or TicketEstados.EnProceso)
                    ticket.Estado = TicketEstados.PendienteCliente;
                ticket.FechaUltimaRespuestaAdmin = DateTime.Now;
            }
            else
            {
                if (ticket.Estado is TicketEstados.Resuelto or TicketEstados.Cerrado)
                    ticket.Estado = TicketEstados.Reabierto;
                else if (ticket.Estado is TicketEstados.PendienteCliente or TicketEstados.Abierto)
                    ticket.Estado = TicketEstados.EnProceso;
            }

            ticket.FechaActualizacion = DateTime.Now;
            if (ticket.Estado == TicketEstados.Reabierto)
            {
                ticket.FechaResolucion = null;
                ticket.FechaCierre = null;
            }

            var mensaje = new TicketMensaje
            {
                IdTicket = ticket.IdTicket,
                IdUsuario = dto.IdUsuario,
                EsRespuestaAdmin = esAdmin,
                Mensaje = (dto.Mensaje ?? "").Trim(),
                FechaCreacion = DateTime.Now
            };
            _ctx.TicketMensajes.Add(mensaje);
            await _ctx.SaveChangesAsync();

            await GuardarAdjuntosAsync(ticket.IdTicket, mensaje.IdMensaje, dto.Archivos);

            if (esAdmin)
            {
                var emp = await _ctx.Empresas.AsNoTracking()
                    .FirstOrDefaultAsync(e => e.IdEmpresa == ticket.IdEmpresa);

                await _notificaciones.PublicarAsync(new NotificacionEvento
                {
                    Tipo = NotificacionTipos.TicketNuevoMensaje,
                    IdEmpresa = ticket.IdEmpresa,
                    DestinoTipo = NotificacionDestinos.Empresa,
                    Prioridad = NotificacionPrioridades.Info,
                    Titulo = $"Respuesta en {ticket.Numero}",
                    Mensaje = Trunc(mensaje.Mensaje, 180) ?? "Nueva respuesta de soporte",
                    Ruta = "/tickets",
                    ReferenciaTipo = "Ticket",
                    ReferenciaId = ticket.IdTicket,
                    CorreoDestino = emp?.CorreElectronico,
                    NombreEmpresa = emp?.NombreComercial
                });

                if (estadoAnterior != ticket.Estado)
                    await NotificarCambioEstadoAsync(ticket, estadoAnterior, ticket.Estado);
            }
            else
            {
                var idEmpresaAdmin = await ObtenerIdEmpresaSistemaAsync();
                if (idEmpresaAdmin > 0)
                {
                    await _notificaciones.PublicarAsync(new NotificacionEvento
                    {
                        Tipo = NotificacionTipos.TicketNuevoMensaje,
                        IdEmpresa = idEmpresaAdmin,
                        DestinoTipo = NotificacionDestinos.Empresa,
                        Prioridad = NotificacionPrioridades.Advertencia,
                        Titulo = $"Cliente respondió {ticket.Numero}",
                        Mensaje = Trunc(mensaje.Mensaje, 180) ?? "Nueva respuesta del cliente",
                        Ruta = "/tickets-admin",
                        ReferenciaTipo = "Ticket",
                        ReferenciaId = ticket.IdTicket,
                        CorreoDestino = await ObtenerCorreoAdminAsync()
                    });
                }
            }

            return await ObtenerAsync(ticket.IdTicket, esAdmin ? null : ticket.IdEmpresa);
        }

        public async Task<TicketDetalleDto> CambiarEstadoAsync(CambiarTicketEstadoDto dto)
        {
            var ticket = await _ctx.Tickets.AsTracking()
                .FirstOrDefaultAsync(t => t.IdTicket == dto.IdTicket)
                ?? throw new Exception("Ticket no encontrado.");

            var usuario = await _ctx.Usuarios.AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdUsuario == dto.IdUsuario)
                ?? throw new Exception("Usuario no encontrado.");

            var empresaUsuario = await _ctx.Empresas.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == usuario.IdEmpresa);
            if (empresaUsuario == null || !empresaUsuario.EsEmpresaSistema)
                throw new Exception("Solo MacroBits puede cambiar el estado del ticket.");

            var nuevo = NormalizarEstado(dto.Estado);
            var anterior = ticket.Estado;
            if (anterior == nuevo) return await ObtenerAsync(ticket.IdTicket, null);

            ticket.Estado = nuevo;
            ticket.FechaActualizacion = DateTime.Now;

            if (nuevo == TicketEstados.Resuelto)
                ticket.FechaResolucion = DateTime.Now;
            if (nuevo == TicketEstados.Cerrado)
            {
                ticket.FechaCierre = DateTime.Now;
                ticket.FechaResolucion ??= DateTime.Now;
            }
            if (nuevo == TicketEstados.Reabierto)
            {
                ticket.FechaResolucion = null;
                ticket.FechaCierre = null;
            }

            if (!string.IsNullOrWhiteSpace(dto.Nota))
            {
                _ctx.TicketMensajes.Add(new TicketMensaje
                {
                    IdTicket = ticket.IdTicket,
                    IdUsuario = dto.IdUsuario,
                    EsRespuestaAdmin = true,
                    Mensaje = $"[Cambio de estado: {anterior} → {nuevo}] {dto.Nota.Trim()}",
                    FechaCreacion = DateTime.Now
                });
            }

            await _ctx.SaveChangesAsync();
            await NotificarCambioEstadoAsync(ticket, anterior, nuevo);

            return await ObtenerAsync(ticket.IdTicket, null);
        }

        public async Task<TicketMetricasDto> ObtenerMetricasAsync(int? idEmpresa)
        {
            var q = _ctx.Tickets.AsNoTracking().AsQueryable();
            if (idEmpresa.HasValue && idEmpresa > 0)
                q = q.Where(t => t.IdEmpresa == idEmpresa.Value);

            var tickets = await q.ToListAsync();
            var metricas = new TicketMetricasDto
            {
                Total = tickets.Count,
                Abiertos = tickets.Count(t => t.Estado == TicketEstados.Abierto),
                EnProceso = tickets.Count(t => t.Estado == TicketEstados.EnProceso),
                PendientesCliente = tickets.Count(t => t.Estado == TicketEstados.PendienteCliente),
                Resueltos = tickets.Count(t => t.Estado == TicketEstados.Resuelto),
                Cerrados = tickets.Count(t => t.Estado == TicketEstados.Cerrado),
                Reabiertos = tickets.Count(t => t.Estado == TicketEstados.Reabierto)
            };

            var conRespuesta = tickets
                .Where(t => t.FechaUltimaRespuestaAdmin.HasValue)
                .Select(t => (t.FechaUltimaRespuestaAdmin!.Value - t.FechaCreacion).TotalHours)
                .Where(h => h >= 0)
                .ToList();
            if (conRespuesta.Count > 0)
                metricas.TiempoPromedioRespuestaHoras = Math.Round(conRespuesta.Average(), 1);

            var resueltos = tickets
                .Where(t => t.FechaResolucion.HasValue)
                .Select(t => (t.FechaResolucion!.Value - t.FechaCreacion).TotalHours)
                .Where(h => h >= 0)
                .ToList();
            if (resueltos.Count > 0)
                metricas.TiempoPromedioResolucionHoras = Math.Round(resueltos.Average(), 1);

            return metricas;
        }

        public async Task<List<TicketNotificacionDto>> ListarNotificacionesAsync(int idEmpresa, int? idUsuario, bool soloNoLeidas = false)
        {
            var q = _ctx.TicketNotificaciones.AsNoTracking()
                .Where(n => n.IdEmpresa == idEmpresa);

            if (idUsuario.HasValue && idUsuario > 0)
                q = q.Where(n => n.IdUsuarioDestino == null || n.IdUsuarioDestino == idUsuario);

            if (soloNoLeidas)
                q = q.Where(n => !n.Leida);

            var rows = await q.OrderByDescending(n => n.FechaCreacion).Take(50).ToListAsync();
            var ticketIds = rows.Select(r => r.IdTicket).Distinct().ToList();
            var numeros = await _ctx.Tickets.AsNoTracking()
                .Where(t => ticketIds.Contains(t.IdTicket))
                .ToDictionaryAsync(t => t.IdTicket, t => t.Numero);

            return rows.Select(n => new TicketNotificacionDto
            {
                IdNotificacion = n.IdNotificacion,
                IdEmpresa = n.IdEmpresa,
                IdTicket = n.IdTicket,
                NumeroTicket = numeros.GetValueOrDefault(n.IdTicket),
                Tipo = n.Tipo,
                Titulo = n.Titulo,
                Mensaje = n.Mensaje,
                Leida = n.Leida,
                FechaCreacion = n.FechaCreacion
            }).ToList();
        }

        public async Task MarcarNotificacionLeidaAsync(int idNotificacion, int idEmpresa)
        {
            var n = await _ctx.TicketNotificaciones.AsTracking()
                .FirstOrDefaultAsync(x => x.IdNotificacion == idNotificacion && x.IdEmpresa == idEmpresa);
            if (n == null) return;
            n.Leida = true;
            await _ctx.SaveChangesAsync();
        }

        public async Task MarcarTodasLeidasAsync(int idEmpresa, int? idUsuario)
        {
            var q = _ctx.TicketNotificaciones.AsTracking()
                .Where(n => n.IdEmpresa == idEmpresa && !n.Leida);
            if (idUsuario.HasValue && idUsuario > 0)
                q = q.Where(n => n.IdUsuarioDestino == null || n.IdUsuarioDestino == idUsuario);

            var list = await q.ToListAsync();
            foreach (var n in list) n.Leida = true;
            await _ctx.SaveChangesAsync();
        }

        // ----------------- helpers -----------------

        private IQueryable<Ticket> BaseListQuery() => _ctx.Tickets.AsNoTracking();

        private async Task<List<TicketListItemDto>> MaterializarListaAsync(IQueryable<Ticket> q)
        {
            var tickets = await q.Take(500).ToListAsync();
            if (tickets.Count == 0) return new List<TicketListItemDto>();

            var empIds = tickets.Select(t => t.IdEmpresa).Distinct().ToList();
            var userIds = tickets.Select(t => t.IdUsuarioCrea).Distinct().ToList();
            var ticketIds = tickets.Select(t => t.IdTicket).ToList();

            var empresas = await _ctx.Empresas.AsNoTracking()
                .Where(e => empIds.Contains(e.IdEmpresa))
                .ToDictionaryAsync(e => e.IdEmpresa, e => e.NombreComercial ?? "");

            var usuarios = await (
                from u in _ctx.Usuarios.AsNoTracking()
                join e in _ctx.EmpleadosP.AsNoTracking() on u.IdEmpleado equals e.IdEmpleados into ej
                from e in ej.DefaultIfEmpty()
                where userIds.Contains(u.IdUsuario)
                select new { u.IdUsuario, Nombre = e != null ? e.Nombre : u.UserName }
            ).ToDictionaryAsync(x => x.IdUsuario, x => x.Nombre ?? "");

            var msgCounts = await _ctx.TicketMensajes.AsNoTracking()
                .Where(m => ticketIds.Contains(m.IdTicket))
                .GroupBy(m => m.IdTicket)
                .Select(g => new { IdTicket = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.IdTicket, x => x.Count);

            var lastClientMsg = await _ctx.TicketMensajes.AsNoTracking()
                .Where(m => ticketIds.Contains(m.IdTicket) && !m.EsRespuestaAdmin)
                .GroupBy(m => m.IdTicket)
                .Select(g => new { IdTicket = g.Key, Fecha = g.Max(x => x.FechaCreacion) })
                .ToDictionaryAsync(x => x.IdTicket, x => x.Fecha);

            return tickets.Select(t =>
            {
                lastClientMsg.TryGetValue(t.IdTicket, out var lastCli);
                var sinResp = t.Estado != TicketEstados.Cerrado
                    && t.Estado != TicketEstados.Resuelto
                    && (t.FechaUltimaRespuestaAdmin == null
                        || (lastCli != default && lastCli > t.FechaUltimaRespuestaAdmin));

                return new TicketListItemDto
                {
                    IdTicket = t.IdTicket,
                    Numero = t.Numero,
                    IdEmpresa = t.IdEmpresa,
                    NombreEmpresa = empresas.GetValueOrDefault(t.IdEmpresa, ""),
                    NombreCliente = t.NombreCliente,
                    Asunto = t.Asunto,
                    Categoria = t.Categoria,
                    Prioridad = t.Prioridad,
                    Estado = t.Estado,
                    FechaCreacion = t.FechaCreacion,
                    FechaActualizacion = t.FechaActualizacion,
                    UsuarioCrea = usuarios.GetValueOrDefault(t.IdUsuarioCrea, ""),
                    CantidadMensajes = msgCounts.GetValueOrDefault(t.IdTicket, 0),
                    SinResponder = sinResp,
                    FechaUltimaRespuestaAdmin = t.FechaUltimaRespuestaAdmin,
                    HorasEstimadasMin = t.HorasEstimadasMin,
                    HorasEstimadasMax = t.HorasEstimadasMax,
                    FechaEstimadaResolucion = t.FechaEstimadaResolucion
                };
            }).ToList();
        }

        /// <summary>
        /// Rango de atención según cola activa. Piso 2h, techo 24h.
        /// Cola vacía ≈ 2–6 h; más tickets en cola acercan el tope a 24 h.
        /// </summary>
        private async Task<(int min, int max)> CalcularRangoEstimadoAsync()
        {
            const int piso = 2;
            const int techo = 24;

            var cola = await _ctx.Tickets.AsNoTracking().CountAsync(t =>
                t.Estado == TicketEstados.Abierto
                || t.Estado == TicketEstados.EnProceso
                || t.Estado == TicketEstados.Reabierto);

            var min = Math.Min(techo - 2, piso + (cola * 2));
            var max = Math.Min(techo, Math.Max(min + 4, 6 + (cola * 4)));
            if (max < min) max = Math.Min(techo, min + 2);
            if (min < piso) min = piso;
            if (max > techo) max = techo;

            return (min, max);
        }

        private static bool EsSinResponder(Ticket ticket, List<TicketMensaje> mensajes)
        {
            if (ticket.Estado is TicketEstados.Cerrado or TicketEstados.Resuelto) return false;
            var lastCli = mensajes.Where(m => !m.EsRespuestaAdmin).Select(m => (DateTime?)m.FechaCreacion).DefaultIfEmpty(null).Max();
            return ticket.FechaUltimaRespuestaAdmin == null
                || (lastCli.HasValue && lastCli > ticket.FechaUltimaRespuestaAdmin);
        }

        private async Task<string> GenerarNumeroAsync()
        {
            var anio = DateTime.Now.Year;
            var seq = await _ctx.TicketSecuencia.AsTracking()
                .FirstOrDefaultAsync(s => s.Anio == anio);
            if (seq == null)
            {
                seq = new TicketSecuencia { Anio = anio, Ultimo = 0 };
                _ctx.TicketSecuencia.Add(seq);
            }
            seq.Ultimo += 1;
            await _ctx.SaveChangesAsync();
            return $"TKT-{anio}-{seq.Ultimo:D5}";
        }

        private async Task GuardarAdjuntosAsync(int idTicket, int? idMensaje, List<IFormFile>? archivos)
        {
            if (archivos == null || archivos.Count == 0) return;

            foreach (var file in archivos)
            {
                if (file == null || file.Length == 0) continue;
                if (file.Length > MaxArchivoBytes)
                    throw new Exception($"El archivo {file.FileName} supera 25 MB.");

                var ext = Path.GetExtension(file.FileName);
                if (!ExtensionesPermitidas.Contains(ext))
                    throw new Exception($"Tipo de archivo no permitido: {ext}");

                string url;
                using (var ms = new MemoryStream())
                {
                    await file.CopyToAsync(ms);
                    url = Utility.UploadFileFtp(ms.ToArray(), Guid.NewGuid() + ext);
                }

                _ctx.TicketAdjuntos.Add(new TicketAdjunto
                {
                    IdTicket = idTicket,
                    IdMensaje = idMensaje,
                    NombreArchivo = Trunc(file.FileName, 260) ?? "archivo",
                    Url = url,
                    ContentType = Trunc(file.ContentType, 120),
                    TamanoBytes = file.Length,
                    FechaCreacion = DateTime.Now
                });
            }

            await _ctx.SaveChangesAsync();
        }

        private async Task NotificarCambioEstadoAsync(Ticket ticket, string anterior, string nuevo)
        {
            var emp = await _ctx.Empresas.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == ticket.IdEmpresa);

            await _notificaciones.PublicarAsync(new NotificacionEvento
            {
                Tipo = NotificacionTipos.TicketEstadoCambiado,
                IdEmpresa = ticket.IdEmpresa,
                DestinoTipo = NotificacionDestinos.Empresa,
                Prioridad = nuevo is TicketEstados.Resuelto or TicketEstados.Cerrado
                    ? NotificacionPrioridades.Exito
                    : NotificacionPrioridades.Advertencia,
                Titulo = $"Ticket {ticket.Numero}: {nuevo.Replace('_', ' ')}",
                Mensaje = $"Estado actualizado de {anterior} a {nuevo}. Asunto: {ticket.Asunto}",
                Ruta = "/tickets",
                ReferenciaTipo = "Ticket",
                ReferenciaId = ticket.IdTicket,
                CorreoDestino = emp?.CorreElectronico,
                NombreEmpresa = emp?.NombreComercial
            });
        }

        private async Task<int> ObtenerIdEmpresaSistemaAsync()
        {
            return await _ctx.Empresas.AsNoTracking()
                .Where(e => e.EsEmpresaSistema && e.Estado)
                .Select(e => e.IdEmpresa)
                .FirstOrDefaultAsync();
        }

        private async Task<string> ObtenerCorreoAdminAsync()
        {
            var correo = await _ctx.Empresas.AsNoTracking()
                .Where(e => e.EsEmpresaSistema && e.Estado)
                .Select(e => e.CorreElectronico)
                .FirstOrDefaultAsync();
            return string.IsNullOrWhiteSpace(correo) ? CorreoAdminFallback : correo.Trim();
        }

        private async Task<string> NombreUsuarioAsync(int idUsuario)
        {
            var nombre = await (
                from u in _ctx.Usuarios.AsNoTracking()
                join e in _ctx.EmpleadosP.AsNoTracking() on u.IdEmpleado equals e.IdEmpleados into ej
                from e in ej.DefaultIfEmpty()
                where u.IdUsuario == idUsuario
                select e != null ? e.Nombre : u.UserName
            ).FirstOrDefaultAsync();
            return nombre ?? $"Usuario #{idUsuario}";
        }

        private static TicketAdjuntoDto MapAdjunto(TicketAdjunto a) => new()
        {
            IdAdjunto = a.IdAdjunto,
            IdTicket = a.IdTicket,
            IdMensaje = a.IdMensaje,
            NombreArchivo = a.NombreArchivo,
            Url = a.Url,
            ContentType = a.ContentType,
            TamanoBytes = a.TamanoBytes,
            FechaCreacion = a.FechaCreacion
        };

        private static string NormalizarPrioridad(string? p)
        {
            var v = (p ?? TicketEstados.PrioridadMedia).Trim().ToUpperInvariant();
            return v switch
            {
                "BAJA" or "MEDIA" or "ALTA" or "CRITICA" => v,
                "CRÍTICA" => TicketEstados.PrioridadCritica,
                _ => TicketEstados.PrioridadMedia
            };
        }

        private static string NormalizarEstado(string? e)
        {
            var v = (e ?? "").Trim().ToUpperInvariant().Replace(' ', '_');
            return v switch
            {
                "ABIERTO" => TicketEstados.Abierto,
                "EN_PROCESO" or "ENPROCESO" => TicketEstados.EnProceso,
                "PENDIENTE_CLIENTE" or "PENDIENTEDELCLIENTE" => TicketEstados.PendienteCliente,
                "RESUELTO" => TicketEstados.Resuelto,
                "CERRADO" => TicketEstados.Cerrado,
                "REABIERTO" => TicketEstados.Reabierto,
                _ => throw new Exception("Estado inválido.")
            };
        }

        private static string NormalizarCategoria(string? c)
        {
            var v = (c ?? "Otro").Trim();
            var permitidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Facturación Electrónica", "Facturacion Electronica",
                "Facturación", "Facturacion",
                "Inventario", "Compras", "Ventas", "Contabilidad", "Banco", "Reportes",
                "Configuración", "Configuracion",
                "Error del Sistema", "Solicitud de Mejora",
                "Acceso al Sistema", "Acceso al sistema",
                "Otro"
            };
            if (!permitidas.Contains(v)) return "Otro";
            return v.ToLowerInvariant() switch
            {
                "facturacion electronica" or "facturación electrónica" => "Facturación Electrónica",
                "facturacion" or "facturación" => "Facturación",
                "configuracion" or "configuración" => "Configuración",
                "acceso al sistema" => "Acceso al Sistema",
                _ => permitidas.First(x => x.Equals(v, StringComparison.OrdinalIgnoreCase))
            };
        }

        private static string? Trunc(string? s, int max)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim();
            return s.Length <= max ? s : s[..max];
        }
    }
}
