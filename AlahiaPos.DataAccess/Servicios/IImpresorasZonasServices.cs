using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class IImpresorasZonasServices : IImpresorasZonas
    {


        IRepository<ImpresorasZonas> Repository;

        public IImpresorasZonasServices(IRepository<ImpresorasZonas> repository)
        {
            Repository = repository;
        }

        public void DeleteImpresorasZonas(int IdImpresorasZonas)
        {
             Repository.Delete(IdImpresorasZonas);
        }

        public async Task<ImpresorasZonas> GetAllImpresorasZonasById(int IdImpresorasZonas)
        {
            return await Repository.GetByIdAsync(IdImpresorasZonas);
        }

        public async Task<IEnumerable<ImpresorasZonas>> GetImpresorasZonas()
        {
            return await Repository.GetAllAsync();
        }

        public async Task InsertImpresorasZonas(ImpresorasZonas ImpresorasZonas)
        {
           await Repository.Save(ImpresorasZonas);
        }

        public void UpdateImpresorasZonas(int Id, ImpresorasZonas ImpresorasZonas)
        {
            Repository.Update(Id, ImpresorasZonas);
        }
    }
}
