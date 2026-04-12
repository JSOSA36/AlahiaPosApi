using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IEmpresas
    {
        Task<Empresas> GetEmpresaById(int Id);
        Task<Empresas> GetEmpresaByGUID(Guid Id);
        void UpdateEmpresas(int Id,Empresas empresas);
        Task InsertEmpresas(Empresas empresas);
    }
}
