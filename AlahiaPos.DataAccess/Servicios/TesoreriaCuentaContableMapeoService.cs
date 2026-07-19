using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;

namespace AlahiaPos.DataAccess.Servicios
{
    public class TesoreriaCuentaContableMapeoService : ITesoreriaCuentaContableMapeoService
    {
        private readonly IRepository<TesoreriaCuentaContableMapeo> _repository;

        public TesoreriaCuentaContableMapeoService(IRepository<TesoreriaCuentaContableMapeo> repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<TesoreriaCuentaContableMapeo>> GetByEmpresaAsync(int idEmpresa)
        {
            return await _repository.GetAllByExpresionAsync(
                x => x.IdEmpresa == idEmpresa && x.Activo);
        }

        public async Task<TesoreriaCuentaContableMapeo?> GetByCuentaFinancieraAsync(int idEmpresa, int idCuentaFinanciera)
        {
            return await _repository.GetByExpresionAsync(
                x =>
                    x.IdEmpresa == idEmpresa
                    && x.IdCuentaFinanciera == idCuentaFinanciera
                    && x.Activo
                    && x.TipoMapeo == "PRINCIPAL");
        }

        public async Task<int> UpsertAsync(TesoreriaCuentaContableMapeo mapeo)
        {
            var existente = await _repository.GetByExpresionAsync(
                x =>
                    x.IdEmpresa == mapeo.IdEmpresa
                    && x.IdCuentaFinanciera == mapeo.IdCuentaFinanciera
                    && x.TipoMapeo == mapeo.TipoMapeo);

            if (existente == null)
            {
                mapeo.FechaCreacion = DateTime.UtcNow;
                await _repository.Save(mapeo);
                return mapeo.IdTesoreriaCuentaContableMapeo;
            }

            existente.IdCuentaContable = mapeo.IdCuentaContable;
            existente.Activo = mapeo.Activo;
            _repository.Update(existente.IdTesoreriaCuentaContableMapeo, existente);
            return existente.IdTesoreriaCuentaContableMapeo;
        }

        public async Task DeleteAsync(int id)
        {
            _repository.Delete(id);
            await Task.CompletedTask;
        }
    }
}
