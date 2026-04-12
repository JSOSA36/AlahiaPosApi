using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class AreaNegocioServices : IAreaNegocio
    {
        private readonly IRepository<AreaNegocio> _repository;

        public AreaNegocioServices(IRepository<AreaNegocio> repository)
        {
            _repository = repository;
        }

        public async Task<List<AreaNegocio>> GetAll(int idEmpresa)
        {
            var data = await _repository
                .GetAllByExpresionAsync(a => a.IdEmpresa == idEmpresa && a.Activo);

            return data.ToList();
        }

        public async Task<AreaNegocio> GetById(int idAreaNegocio)
        {
            return await _repository.GetByIdAsync(idAreaNegocio);
        }

        public async Task<AreaNegocio> Add(AreaNegocio entity)
        {
            await _repository.Save(entity);
            return entity;
        }

        public async Task<AreaNegocio> Update(AreaNegocio entity)
        {
            _repository.Update(entity.IdAreaNegocio, entity);
            return entity;
        }

        public async Task<bool> Delete(int idAreaNegocio)
        {
            _repository.Delete(idAreaNegocio);
            return await Task.FromResult(true);
        }
    }
}