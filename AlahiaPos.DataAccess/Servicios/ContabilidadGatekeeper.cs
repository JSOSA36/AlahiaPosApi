using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    public class ContabilidadGatekeeper : IContabilidadGatekeeper
    {
        private readonly IEmpresaModulos _empresaModulos;
        private readonly IModulo _modulos;
        private readonly AlahiaPosContext _context;

        public ContabilidadGatekeeper(
            IEmpresaModulos empresaModulos,
            IModulo modulos,
            AlahiaPosContext context)
        {
            _empresaModulos = empresaModulos;
            _modulos = modulos;
            _context = context;
        }

        public async Task<bool> ModuloContratadoAsync(int idEmpresa)
        {
            var modulo = await _modulos.GetModuloByCodigo(ContabilidadModuloConstantes.CodigoModuloContabilidad);
            if (modulo == null) return false;

            return await _empresaModulos.EmpresaTieneModulo(idEmpresa, modulo.Id);
        }

        public async Task<bool> IntegracionAutomaticaActivaAsync(int idEmpresa)
        {
            if (!await ModuloContratadoAsync(idEmpresa))
                return false;

            var config = await _context.ContabilidadConfiguracion
                .FirstOrDefaultAsync(c => c.IdEmpresa == idEmpresa);

            return config?.IntegracionAutomatica == true;
        }

        public async Task<ContabilidadGatekeeperStatus> ObtenerEstadoAsync(int idEmpresa)
        {
            var moduloContratado = await ModuloContratadoAsync(idEmpresa);
            var integracionActiva = moduloContratado &&
                (await _context.ContabilidadConfiguracion
                    .FirstOrDefaultAsync(c => c.IdEmpresa == idEmpresa))?.IntegracionAutomatica == true;

            return new ContabilidadGatekeeperStatus
            {
                ModuloContratado = moduloContratado,
                IntegracionAutomaticaActiva = integracionActiva
            };
        }
    }
}
