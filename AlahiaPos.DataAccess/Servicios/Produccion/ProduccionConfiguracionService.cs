using System;
using System.Threading.Tasks;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios.Produccion
{
    public class ProduccionConfiguracionService : IProduccionConfiguracionService
    {
        private readonly AlahiaPosContext _ctx;

        public ProduccionConfiguracionService(AlahiaPosContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<ProduccionConfiguracionDto?> ObtenerAsync(int idEmpresa)
        {
            var c = await _ctx.ProduccionConfiguracionEmpresa.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdEmpresa == idEmpresa);
            return c == null ? null : Map(c);
        }

        public async Task<bool> EstaActivoAsync(int idEmpresa)
        {
            return await _ctx.ProduccionConfiguracionEmpresa.AsNoTracking()
                .AnyAsync(x => x.IdEmpresa == idEmpresa && x.Activo);
        }

        public async Task<ProduccionConfiguracionDto> ActualizarAsync(ProduccionConfiguracionDto dto)
        {
            if (dto == null || dto.IdEmpresa <= 0)
                throw new InvalidOperationException("Configuración inválida.");

            var entity = await _ctx.ProduccionConfiguracionEmpresa
                .AsTracking()
                .FirstOrDefaultAsync(x => x.IdEmpresa == dto.IdEmpresa);

            if (entity == null)
            {
                entity = new ProduccionConfiguracionEmpresa { IdEmpresa = dto.IdEmpresa };
                _ctx.ProduccionConfiguracionEmpresa.Add(entity);
            }

            entity.Activo = dto.Activo;
            entity.UsarEstaciones = dto.UsarEstaciones;
            entity.UsarEstadosPorItem = dto.UsarEstadosPorItem;
            entity.SonidoActivo = dto.SonidoActivo;
            entity.TiempoAdvertenciaSegDefault = dto.TiempoAdvertenciaSegDefault;
            entity.TiempoCriticoSegDefault = dto.TiempoCriticoSegDefault;
            entity.PermitirCompletarDesdeEstacion = dto.PermitirCompletarDesdeEstacion;
            entity.IdEstacionPredeterminada = dto.IdEstacionPredeterminada;
            entity.ModoOscuroDefault = dto.ModoOscuroDefault;
            entity.MostrarNombreCliente = dto.MostrarNombreCliente;
            entity.MostrarUsuarioSolicita = dto.MostrarUsuarioSolicita;
            entity.FechaActualizacion = DateTime.Now;

            await _ctx.SaveChangesAsync();
            return Map(entity);
        }

        private static ProduccionConfiguracionDto Map(ProduccionConfiguracionEmpresa c) => new()
        {
            IdEmpresa = c.IdEmpresa,
            Activo = c.Activo,
            UsarEstaciones = c.UsarEstaciones,
            UsarEstadosPorItem = c.UsarEstadosPorItem,
            SonidoActivo = c.SonidoActivo,
            TiempoAdvertenciaSegDefault = c.TiempoAdvertenciaSegDefault,
            TiempoCriticoSegDefault = c.TiempoCriticoSegDefault,
            PermitirCompletarDesdeEstacion = c.PermitirCompletarDesdeEstacion,
            IdEstacionPredeterminada = c.IdEstacionPredeterminada,
            ModoOscuroDefault = c.ModoOscuroDefault,
            MostrarNombreCliente = c.MostrarNombreCliente,
            MostrarUsuarioSolicita = c.MostrarUsuarioSolicita
        };
    }
}
