using AlahiaPos.Entities.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface ICuentaFinancieraService
    {
        Task<int> CreateAsync(
            CuentaFinanciera entity
        );

        Task UpdateAsync(
            CuentaFinanciera entity
        );

        Task DeleteAsync(
            int id
        );

        Task<CuentaFinanciera?>
            GetByIdAsync(
                int id
            );

        Task<IEnumerable<CuentaFinanciera>>
            GetAllAsync();

        Task<IEnumerable<CuentaFinanciera>>
            GetByEmpresaAsync(
                int idEmpresa
            );

        Task<decimal>
            GetBalanceAsync(
                int idCuentaFinanciera
            );

        Task<IEnumerable<TesoreriaSaldoResumenDto>>
            GetResumenSaldosAsync(
                int idEmpresa
            );

        Task<int>
            SincronizarSaldosAsync(
                int idEmpresa
            );
    }
}

