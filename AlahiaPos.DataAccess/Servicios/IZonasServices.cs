using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class IZonasServices : IZonas
    {

        IRepository<Zonas> repository;
        public IZonasServices(IRepository<Zonas> repository)
        {
            this.repository = repository;
        }

        public void DeleteZonas(int IdMesa)
        {
            repository.Delete(IdMesa);
        }

        public async Task<IEnumerable<Zonas>> GetAllZonas()
        {
           //repository.GetAllAsync("Mesas");
            return await repository.GetAllByExpresionAsync(c => c.IdEmpresa == GlobalParamter.IdEmpresa,"Mesas");
        }

        public async Task<Zonas> GetAllZonasById(int IdZonas)
        {
            return await repository.GetByIdAsync(IdZonas);
        }

        public Task InsertZonas(Zonas Zonas)
        {
            return  repository.Save(Zonas);
        }

        public void UpdateZonas(int Id, Zonas Zonas)
        {
            repository.Update(Id, Zonas);
        }
    }
}
