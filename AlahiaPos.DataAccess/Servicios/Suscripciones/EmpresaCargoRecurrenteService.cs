using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios.Suscripciones
{
    public class EmpresaCargoRecurrenteService : IEmpresaCargoRecurrenteService
    {
        private readonly AlahiaPosContext _ctx;
        private readonly ISuscripcionCobroService _suscripcion;

        public EmpresaCargoRecurrenteService(AlahiaPosContext ctx, ISuscripcionCobroService suscripcion)
        {
            _ctx = ctx;
            _suscripcion = suscripcion;
        }

        public async Task<List<EmpresaCargoRecurrenteDto>> ListarPorEmpresaAsync(int idEmpresa, bool soloActivos = false)
        {
            var q = _ctx.EmpresaCargoRecurrente.AsNoTracking()
                .Where(c => c.IdEmpresa == idEmpresa);

            if (soloActivos)
                q = q.Where(c => c.Activo);

            var rows = await q.OrderByDescending(c => c.Activo).ThenBy(c => c.Nombre).ToListAsync();
            var nombreEmpresa = await _ctx.Empresas.AsNoTracking()
                .Where(e => e.IdEmpresa == idEmpresa)
                .Select(e => e.NombreComercial)
                .FirstOrDefaultAsync();

            return rows.Select(c => MapDto(c, nombreEmpresa)).ToList();
        }

        public Task<SuscripcionCalculoFacturaDto> CalcularFacturaAsync(int idEmpresa, DateTime? fechaReferencia = null)
            => _suscripcion.CalcularFacturaAsync(idEmpresa, fechaReferencia);

        public Task RecalcularCicloAbiertoAsync(int idEmpresa)
            => _suscripcion.RecalcularCicloAbiertoAsync(idEmpresa);

        public async Task<EmpresaCargoRecurrenteDto> CrearAsync(CrearEmpresaCargoRecurrenteDto dto)
        {
            var tipo = (dto.TipoCargo ?? TipoCargoRecurrente.Otro).Trim().ToUpperInvariant();
            string codigo = (dto.Codigo ?? "").Trim();
            string nombre = (dto.Nombre ?? "").Trim();
            decimal monto = dto.MontoMensual ?? 0;
            int? idModulo = dto.IdModulo;

            if (tipo == TipoCargoRecurrente.Modulo)
            {
                if (!idModulo.HasValue)
                    throw new Exception("IdModulo es requerido para cargos tipo MODULO.");

                var modulo = await _ctx.Modulos.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == idModulo.Value)
                    ?? throw new Exception("Módulo no encontrado.");

                if (string.IsNullOrWhiteSpace(codigo)) codigo = modulo.Codigo;
                if (string.IsNullOrWhiteSpace(nombre)) nombre = modulo.Nombre;
                if (!dto.MontoMensual.HasValue) monto = modulo.PrecioUSD;
            }

            if (string.IsNullOrWhiteSpace(nombre))
                throw new Exception("Nombre del cargo es requerido.");
            if (string.IsNullOrWhiteSpace(codigo))
                codigo = tipo;

            var entity = new EmpresaCargoRecurrente
            {
                IdEmpresa = dto.IdEmpresa,
                TipoCargo = tipo,
                IdModulo = idModulo,
                Codigo = codigo,
                Nombre = nombre,
                MontoMensual = monto,
                FechaInicio = dto.FechaInicio ?? DateTime.Now.Date,
                FechaFin = dto.FechaFin,
                Activo = true,
                Observacion = dto.Observacion,
                IdUsuarioCreacion = dto.IdUsuarioCreacion,
                FechaCreacion = DateTime.Now
            };

            _ctx.EmpresaCargoRecurrente.Add(entity);
            await _ctx.SaveChangesAsync();

            await _suscripcion.RegistrarEventoAsync(dto.IdEmpresa, "CARGO_CREADO",
                $"Cargo {tipo} '{nombre}' por USD {monto:0.00}/mes");

            await _suscripcion.RecalcularCicloAbiertoAsync(dto.IdEmpresa);

            return MapDto(entity, null);
        }

        public async Task<EmpresaCargoRecurrenteDto> ActualizarAsync(ActualizarEmpresaCargoRecurrenteDto dto)
        {
            var entity = await _ctx.EmpresaCargoRecurrente.FirstOrDefaultAsync(c => c.Id == dto.Id)
                ?? throw new Exception("Cargo no encontrado.");

            var montoAnterior = entity.MontoMensual;
            entity.MontoMensual = dto.MontoMensual;
            if (dto.FechaInicio.HasValue)
                entity.FechaInicio = dto.FechaInicio.Value;
            if (dto.FechaFin.HasValue)
                entity.FechaFin = dto.FechaFin;
            if (dto.Observacion != null)
                entity.Observacion = dto.Observacion;
            if (!string.IsNullOrWhiteSpace(dto.Nombre))
                entity.Nombre = dto.Nombre.Trim();
            entity.IdUsuarioModificacion = dto.IdUsuarioModificacion;
            entity.FechaModificacion = DateTime.Now;

            await _ctx.SaveChangesAsync();

            if (montoAnterior != dto.MontoMensual)
            {
                await _suscripcion.RegistrarEventoAsync(entity.IdEmpresa, "CARGO_MONTO_CAMBIADO",
                    $"Cargo '{entity.Nombre}': {montoAnterior:0.00} → {dto.MontoMensual:0.00}");
            }
            else
            {
                await _suscripcion.RegistrarEventoAsync(entity.IdEmpresa, "CARGO_ACTUALIZADO",
                    $"Cargo '{entity.Nombre}' actualizado");
            }

            await _suscripcion.RecalcularCicloAbiertoAsync(entity.IdEmpresa);
            return MapDto(entity, null);
        }

        public async Task DesactivarAsync(int id, int? idUsuario = null)
        {
            var entity = await _ctx.EmpresaCargoRecurrente.FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new Exception("Cargo no encontrado.");

            entity.Activo = false;
            entity.FechaFin ??= DateTime.Now.Date;
            entity.IdUsuarioModificacion = idUsuario;
            entity.FechaModificacion = DateTime.Now;
            await _ctx.SaveChangesAsync();

            await _suscripcion.RegistrarEventoAsync(entity.IdEmpresa, "CARGO_DESACTIVADO",
                $"Cargo '{entity.Nombre}' desactivado (no afecta permisos/licencia)");

            await _suscripcion.RecalcularCicloAbiertoAsync(entity.IdEmpresa);
        }

        private static EmpresaCargoRecurrenteDto MapDto(EmpresaCargoRecurrente c, string? nombreEmpresa) => new()
        {
            Id = c.Id,
            IdEmpresa = c.IdEmpresa,
            TipoCargo = c.TipoCargo,
            IdModulo = c.IdModulo,
            Codigo = c.Codigo,
            Nombre = c.Nombre,
            MontoMensual = c.MontoMensual,
            FechaInicio = c.FechaInicio,
            FechaFin = c.FechaFin,
            Activo = c.Activo,
            Observacion = c.Observacion,
            NombreEmpresa = nombreEmpresa,
            FechaCreacion = c.FechaCreacion
        };
    }
}
