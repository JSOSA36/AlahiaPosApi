using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public interface IAreaNegocio
    {
        Task<List<AreaNegocio>> GetAll(int idEmpresa);
        Task<AreaNegocio> GetById(int idAreaNegocio);
        Task<AreaNegocio> Add(AreaNegocio entity);
        Task<AreaNegocio> Update(AreaNegocio entity);
        Task<bool> Delete(int idAreaNegocio);
       
    }
}
