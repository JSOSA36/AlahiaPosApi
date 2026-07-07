using AlahiaPos.Entities.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IMetodoPagoCuentaService
    {
        Task<int> CreateAsync(
           MetodoPagoCuenta entity
       );

        Task UpdateAsync(
            MetodoPagoCuenta entity
        );

        Task DeleteAsync(
            int id
        );

        Task<MetodoPagoCuenta?>
            GetByIdAsync(
                int id
            );

        Task<IEnumerable<MetodoPagoCuenta>>
            GetAllAsync();

        Task<IEnumerable<MetodoPagoCuenta>>
            GetByEmpresaAsync(
                int idEmpresa
            );

        Task<MetodoPagoCuenta?>
            GetByMetodoAsync(

                int idEmpresa,

                string metodoPago
            );
    }
}

