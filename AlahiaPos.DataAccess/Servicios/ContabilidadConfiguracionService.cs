using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    public class ContabilidadConfiguracionService : IContabilidadConfiguracionService
    {
        private readonly AlahiaPosContext _context;
        private readonly IContabilidadGatekeeper _gatekeeper;
        private readonly IModulo _modulos;
        private readonly IContabilidadCuentaMapeoService _mapeo;

        public ContabilidadConfiguracionService(
            AlahiaPosContext context,
            IContabilidadGatekeeper gatekeeper,
            IModulo modulos,
            IContabilidadCuentaMapeoService mapeo)
        {
            _context = context;
            _gatekeeper = gatekeeper;
            _modulos = modulos;
            _mapeo = mapeo;
        }

        public async Task<ContabilidadConfiguracionDto> GetConfiguracionAsync(int idEmpresa)
        {
            if (!await _gatekeeper.ModuloContratadoAsync(idEmpresa))
                throw new UnauthorizedAccessException(
                    "La empresa no tiene contratado el módulo de Contabilidad.");

            var config = await _context.ContabilidadConfiguracion
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdEmpresa == idEmpresa);

            if (config == null)
                throw new KeyNotFoundException(
                    "La configuración contable no está inicializada para esta empresa.");

            return MapToDto(config, moduloContratado: true);
        }

        public async Task<ContabilidadConfiguracionDto> ActualizarAsync(
            ActualizarContabilidadConfiguracionRequest request)
        {
            if (!await _gatekeeper.ModuloContratadoAsync(request.IdEmpresa))
                throw new InvalidOperationException(
                    "La empresa no tiene contratado el módulo de Contabilidad.");

            var config = await EnsureConfiguracionAsync(request.IdEmpresa);

            config.IntegracionAutomatica = request.IntegracionAutomatica;
            config.GenerarCOGSAutomatico = request.GenerarCOGSAutomatico;
            config.SepararAsientoCOGS = request.SepararAsientoCOGS;
            config.FechaActualizacion = DateTime.Now;

            _context.ContabilidadConfiguracion.Update(config);
            await _context.SaveChangesAsync();

            if (config.IntegracionAutomatica)
                await _mapeo.EnsureMapeoDefaultAsync(request.IdEmpresa);

            return MapToDto(config, moduloContratado: true);
        }

        public async Task InicializarSiEsModuloContabilidadAsync(int idEmpresa, int moduloId, bool activo)
        {
            if (!activo)
                return;

            var modulo = await _modulos.GetModuloById(moduloId);
            if (modulo == null ||
                modulo.Codigo != ContabilidadModuloConstantes.CodigoModuloContabilidad)
                return;

            await EnsureConfiguracionAsync(idEmpresa);
        }

        public async Task<ContabilidadConfiguracion> EnsureConfiguracionAsync(int idEmpresa)
        {
            var config = await _context.ContabilidadConfiguracion
                .FirstOrDefaultAsync(c => c.IdEmpresa == idEmpresa);

            if (config != null)
                return config;

            config = new ContabilidadConfiguracion
            {
                IdEmpresa = idEmpresa,
                IntegracionAutomatica = false,
                GenerarCOGSAutomatico = true,
                SepararAsientoCOGS = true,
                FechaInseccion = DateTime.Now
            };

            _context.ContabilidadConfiguracion.Add(config);
            await _context.SaveChangesAsync();

            await _mapeo.EnsureMapeoDefaultAsync(idEmpresa);

            return config;
        }

        private static ContabilidadConfiguracionDto MapToDto(
            ContabilidadConfiguracion config,
            bool moduloContratado)
        {
            return new ContabilidadConfiguracionDto
            {
                IdContabilidadConfiguracion = config.IdContabilidadConfiguracion,
                IdEmpresa = config.IdEmpresa,
                ModuloContratado = moduloContratado,
                IntegracionAutomatica = config.IntegracionAutomatica,
                GenerarCOGSAutomatico = config.GenerarCOGSAutomatico,
                SepararAsientoCOGS = config.SepararAsientoCOGS
            };
        }
    }
}
