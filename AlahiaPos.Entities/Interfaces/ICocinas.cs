using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface ICocinas
    {

        Task<IEnumerable<Cocinas>> GetAllCocinas();


        Task<Cocinas> GetCocinasById(int id);
        Task InsertCocinas(Cocinas Cocinas);

        void UpdateCocinas(Cocinas Cocinas);

        void DeleteCocinas(int id);
    }
}
