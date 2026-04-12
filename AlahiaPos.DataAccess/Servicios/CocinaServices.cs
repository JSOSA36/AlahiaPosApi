using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class CocinaServices : ICocinas
    {

        IRepository<Cocinas> _Repository;

        public CocinaServices(IRepository<Cocinas> repository)
        {
            _Repository = repository;
        }
        public void DeleteCocinas(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Cocinas>> GetAllCocinas()
        {
            return await _Repository.GetAllByExpresionAsync(c=>c.IdEmpresa==GlobalParamter.IdEmpresa);
        }

        public Task<Cocinas> GetCocinasById(int id)
        {
            throw new NotImplementedException();
        }

        public Task InsertCocinas(Cocinas Cocinas)
        {
            throw new NotImplementedException();
        }

        public void UpdateCocinas(Cocinas Cocinas)
        {
            throw new NotImplementedException();
        }
    }
}
