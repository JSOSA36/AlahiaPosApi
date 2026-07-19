using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class PagoEmpresaService : IPagoEmpresaService
    {
        private const string CorreoAdminFallback = "ing.joelarielsosa@gmail.com";

        private readonly AlahiaPosContext _ctx;
        private readonly ISuscripcionCobroService _suscripcion;
        private readonly INotificacionCentro _notificaciones;

        public PagoEmpresaService(
            AlahiaPosContext ctx,
            ISuscripcionCobroService suscripcion,
            INotificacionCentro notificaciones)
        {
            _ctx = ctx;
            _suscripcion = suscripcion;
            _notificaciones = notificaciones;
        }

        public async Task CrearPagoAsync(CrearPagoDto dto)
        {
            var empresa = await _ctx.Empresas.AsTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == dto.IdEmpresa)
                ?? throw new Exception("Empresa no encontrada.");

            var yaPendiente = await _ctx.PagosEmpresa.AnyAsync(p =>
                p.IdEmpresa == dto.IdEmpresa && p.Estado == "PENDIENTE");
            if (yaPendiente)
                throw new Exception("Ya tiene un pago en validación. Espere la aprobación de MacroBits.");

            var ciclo = await _suscripcion.ObtenerOCrearCicloActualAsync(empresa);
            var estadoPrevio = (empresa.EstadoServicio ?? string.Empty).Trim().ToUpperInvariant();
            var estabaSuspendida = estadoPrevio is "SUSPENDIDA" or "BLOQUEADO";

            var pago = new PagoEmpresa
            {
                IdEmpresa = dto.IdEmpresa,
                Monto = dto.Monto,
                ArchivoUrl = dto.ArchivoUrl,
                FechaSubida = DateTime.Now,
                FechaPago = dto.FechaPago ?? DateTime.Now,
                Banco = dto.Banco,
                Referencia = dto.Referencia,
                IdCiclo = ciclo?.IdCiclo,
                IdUsuarioReporta = dto.IdUsuarioReporta,
                Estado = "PENDIENTE",
                // Marcador interno (no visible al cliente). Se limpia al aprobar/rechazar.
                Observacion = estabaSuspendida ? "[HUBO_SUSPENSION]" : null
            };

            _ctx.PagosEmpresa.Add(pago);
            await _ctx.SaveChangesAsync();

            await _suscripcion.OnPagoReportadoAsync(dto.IdEmpresa, pago.Id, ciclo?.IdCiclo);

            await NotificarAdminPagoReportadoAsync(empresa, pago);
        }

        public async Task<List<PagoEmpresaDto>> ObtenerPagosAsync()
        {
            var rows = await (
                from p in _ctx.PagosEmpresa.AsNoTracking()
                join e in _ctx.Empresas.AsNoTracking() on p.IdEmpresa equals e.IdEmpresa
                orderby p.FechaSubida descending
                select new PagoEmpresaDto
                {
                    Id = p.Id,
                    IdEmpresa = (int)p.IdEmpresa!,
                    NombreEmpresa = e.NombreComercial,
                    Monto = p.Monto ?? 0,
                    FechaSubida = p.FechaSubida ?? DateTime.MinValue,
                    ArchivoUrl = p.ArchivoUrl ?? "",
                    Estado = p.Estado ?? "",
                    Observacion = p.Observacion,
                    FechaValidacion = p.FechaValidacion,
                    UsuarioValida = p.UsuarioValida ?? "",
                    FechaPago = p.FechaPago,
                    Banco = p.Banco,
                    Referencia = p.Referencia,
                    IdCiclo = p.IdCiclo
                }
            ).ToListAsync();

            foreach (var r in rows)
                r.Observacion = LimpiarMarcadorInterno(r.Observacion);

            return rows;
        }

        public async Task<List<PagoEmpresaDto>> ObtenerPagosPorEmpresaAsync(int idEmpresa)
        {
            var rows = await (
                from p in _ctx.PagosEmpresa.AsNoTracking()
                join e in _ctx.Empresas.AsNoTracking() on p.IdEmpresa equals e.IdEmpresa
                where p.IdEmpresa == idEmpresa
                orderby p.FechaSubida descending
                select new PagoEmpresaDto
                {
                    Id = p.Id,
                    IdEmpresa = (int)p.IdEmpresa!,
                    NombreEmpresa = e.NombreComercial,
                    Monto = p.Monto ?? 0,
                    FechaSubida = p.FechaSubida ?? DateTime.MinValue,
                    ArchivoUrl = p.ArchivoUrl ?? "",
                    Estado = p.Estado ?? "",
                    Observacion = p.Observacion,
                    FechaValidacion = p.FechaValidacion,
                    UsuarioValida = p.UsuarioValida ?? "",
                    FechaPago = p.FechaPago,
                    Banco = p.Banco,
                    Referencia = p.Referencia,
                    IdCiclo = p.IdCiclo
                }
            ).ToListAsync();

            foreach (var r in rows)
                r.Observacion = LimpiarMarcadorInterno(r.Observacion);

            return rows;
        }

        public async Task ValidarPagoAsync(ValidarPagoDto dto)
        {
            var pago = await _ctx.PagosEmpresa.AsTracking()
                .FirstOrDefaultAsync(p => p.Id == dto.IdPago)
                ?? throw new Exception("El pago no existe.");

            if (pago.Estado != "PENDIENTE")
                throw new Exception("Este pago ya fue procesado.");

            var empresa = await _ctx.Empresas.AsTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == pago.IdEmpresa)
                ?? throw new Exception("Empresa no encontrada.");

            var estadoNuevo = (dto.Estado ?? string.Empty).Trim().ToUpperInvariant();
            var huboSuspension = await DeterminarHuboSuspensionAsync(empresa.IdEmpresa, pago);

            if (estadoNuevo == "APROBADO")
            {
                pago.Estado = "APROBADO";
                pago.FechaValidacion = DateTime.Now;
                pago.UsuarioValida = dto.UsuarioValida;
                pago.Observacion = dto.Observacion;
                _ctx.PagosEmpresa.Update(pago);
                await _ctx.SaveChangesAsync();

                await _suscripcion.OnPagoAprobadoAsync(
                    empresa.IdEmpresa, pago.Id, pago.IdCiclo, dto.UsuarioValida);

                var mensajeServicio = huboSuspension
                    ? "Su servicio ha sido reactivado. Ya puede ingresar normalmente al sistema."
                    : "Hemos recibido y confirmado su pago. Gracias por mantener su suscripción al día.";

                await _notificaciones.PublicarAsync(new NotificacionEvento
                {
                    Tipo = NotificacionTipos.PagoAprobado,
                    IdEmpresa = empresa.IdEmpresa,
                    DestinoTipo = NotificacionDestinos.Empresa,
                    Prioridad = NotificacionPrioridades.Exito,
                    Titulo = "Pago aprobado — Alahia ERP",
                    Mensaje = $"Su pago de USD {(pago.Monto ?? 0):0.00} fue aprobado. {mensajeServicio}",
                    Ruta = "/pago-suscripcion",
                    ReferenciaTipo = "PagoEmpresa",
                    ReferenciaId = pago.Id,
                    CorreoDestino = empresa.CorreElectronico,
                    NombreEmpresa = empresa.NombreComercial
                });
            }
            else if (estadoNuevo == "RECHAZADO")
            {
                pago.Estado = "RECHAZADO";
                pago.Observacion = dto.Observacion;
                pago.FechaValidacion = DateTime.Now;
                pago.UsuarioValida = dto.UsuarioValida;
                _ctx.PagosEmpresa.Update(pago);
                await _ctx.SaveChangesAsync();

                await _suscripcion.OnPagoRechazadoAsync(
                    empresa.IdEmpresa, pago.Id, dto.Observacion);

                await _notificaciones.PublicarAsync(new NotificacionEvento
                {
                    Tipo = NotificacionTipos.PagoRechazado,
                    IdEmpresa = empresa.IdEmpresa,
                    DestinoTipo = NotificacionDestinos.Empresa,
                    Prioridad = NotificacionPrioridades.Error,
                    Titulo = "Pago rechazado — Alahia ERP",
                    Mensaje = $"Su pago de USD {(pago.Monto ?? 0):0.00} fue rechazado. Motivo: {dto.Observacion ?? "Comprobante no válido"}. Puede reportar un nuevo pago.",
                    Ruta = "/pago-suscripcion",
                    ReferenciaTipo = "PagoEmpresa",
                    ReferenciaId = pago.Id,
                    CorreoDestino = empresa.CorreElectronico,
                    NombreEmpresa = empresa.NombreComercial
                });
            }
            else
            {
                throw new Exception("Estado inválido.");
            }
        }

        private async Task<bool> DeterminarHuboSuspensionAsync(int idEmpresa, PagoEmpresa pago)
        {
            if (!string.IsNullOrEmpty(pago.Observacion)
                && pago.Observacion.Contains("[HUBO_SUSPENSION]", StringComparison.OrdinalIgnoreCase))
                return true;

            if (pago.IdCiclo.HasValue)
            {
                var porCiclo = await _ctx.SuscripcionEvento.AnyAsync(e =>
                    e.IdEmpresa == idEmpresa
                    && e.IdCiclo == pago.IdCiclo
                    && e.Tipo == "SUSPENSION");
                if (porCiclo) return true;
            }

            return await _ctx.SuscripcionEvento.AnyAsync(e =>
                e.IdEmpresa == idEmpresa
                && e.Tipo == "SUSPENSION"
                && e.Fecha >= DateTime.Now.AddDays(-40));
        }

        private async Task NotificarAdminPagoReportadoAsync(Empresas empresa, PagoEmpresa pago)
        {
            var admin = await _ctx.Empresas.AsNoTracking()
                .Where(e => e.EsEmpresaSistema && e.Estado)
                .Select(e => new { e.IdEmpresa, e.CorreElectronico })
                .FirstOrDefaultAsync();

            var idEmpresaAdmin = admin?.IdEmpresa ?? 0;
            if (idEmpresaAdmin <= 0) return;

            var correoAdmin = string.IsNullOrWhiteSpace(admin?.CorreElectronico)
                ? CorreoAdminFallback
                : admin!.CorreElectronico!.Trim();

            await _notificaciones.PublicarAsync(new NotificacionEvento
            {
                Tipo = NotificacionTipos.PagoPendiente,
                IdEmpresa = idEmpresaAdmin,
                DestinoTipo = NotificacionDestinos.Empresa,
                Prioridad = NotificacionPrioridades.Advertencia,
                Titulo = $"Pago pendiente de aprobar — {empresa.NombreComercial}",
                Mensaje = $"{empresa.NombreComercial} reportó un pago de USD {(pago.Monto ?? 0):0.00} (ref: {pago.Referencia ?? "—"}). Revise Cobros y Suscripciones.",
                Ruta = "/cobros-admin",
                ReferenciaTipo = "PagoEmpresa",
                ReferenciaId = pago.Id,
                CorreoDestino = correoAdmin,
                NombreEmpresa = empresa.NombreComercial
            });
        }

        private static string? LimpiarMarcadorInterno(string? observacion)
        {
            if (string.IsNullOrWhiteSpace(observacion)) return observacion;
            var limpio = observacion.Replace("[HUBO_SUSPENSION]", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            return string.IsNullOrWhiteSpace(limpio) ? null : limpio;
        }
    }
}
