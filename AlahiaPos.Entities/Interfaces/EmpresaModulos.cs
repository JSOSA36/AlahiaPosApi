using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface EmpresaModulos
    {
        Task<IEnumerable<EmpresaModulo>> GetModulosByEmpresa(int IdEmpresa);
        Task<bool> EmpresaTieneModulo(int IdEmpresa, int IdModulo);
        Task InsertEmpresaModulo(EmpresaModulo empresaModulo);
        void UpdateEmpresaModulo(int Id, EmpresaModulo empresaModulo);
    }
}
