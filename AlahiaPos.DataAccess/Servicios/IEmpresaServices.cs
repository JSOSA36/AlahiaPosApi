using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class IEmpresaServices : IEmpresas
    {

        IRepository<Empresas> repository;

        public IEmpresaServices(IRepository<Empresas> repository)
        {
            this.repository = repository;
        }

        public async Task<Empresas> GetEmpresaById(int Id)
        {
           return await  repository.GetByIdAsync(Id);
        }

        public void UpdateEmpresas(int Id,Empresas empresas)
        {
            repository.Update(Id, empresas);
        }
        public async Task InsertEmpresas(Empresas empresas)
        {
            await repository.Save(empresas);
            
        }

        public async Task<Empresas> GetEmpresaByGUID(Guid Id)
        {
           return await repository.GetByExpresionAsync(c => c.GuidPublico == Id);
        }
    }
}
