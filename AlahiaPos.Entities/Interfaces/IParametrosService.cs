using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IParametrosService
    {
        Task<List<Parametros>> GetParametrosEmpresa(int idEmpresa);

        Task<List<Parametros>> GetParametrosPOS(int idEmpresa, string codigoPOS);

        Task<Parametros?> GetParametro(int idEmpresa, string clave);

        Task<Parametros?> GetParametroPOS(int idEmpresa, string codigoPOS, string clave);

        Task InsertParametro(Parametros parametro);

        Task UpdateParametro(Parametros parametro);

        Task DeleteParametro(int idParametro);
    }
}
