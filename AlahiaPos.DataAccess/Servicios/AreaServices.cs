using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class AreaServices : IAreas
    {
        private readonly IRepository<Area> _repository;

        public AreaServices(IRepository<Area> repository)
        {
            _repository = repository;
        }

        public void DeleteArea(int IdArea)
        {
            _repository.Delete(IdArea);
        }

        public async Task<IEnumerable<Area>> GetAllAreas(int IdEmpresa)
        {
            // Filtra por empresa y activas
            return await _repository.GetAllByExpresionAsync(a => a.IdEmpresa == IdEmpresa && a.IsActivo);
        }

        public async Task<Area> GetAreaById(int IdArea)
        {
            return await _repository.GetByIdAsync(IdArea);
        }

        public async Task InsertArea(Area area)
        {
            await _repository.Save(area);
        }

        public void UpdateArea(int Id, Area area)
        {
            _repository.Update(Id, area);
        }
    }
}
