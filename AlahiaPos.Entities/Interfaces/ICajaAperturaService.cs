using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface ICajaAperturaService
    {
        Task<int> CreateAsync(CajaApertura entity);

        Task UpdateAsync(CajaApertura entity);

        Task DeleteAsync(int id);

        Task<CajaApertura?> GetByIdAsync(int id);

        /* =====================================
        🔥 CONSULTAS
        ===================================== */

        Task<IEnumerable<CajaApertura>> GetAllAsync(int IdEmpresa);

        Task<CajaApertura?> GetCajaAbiertaAsync(
            int idEmpresa,
            int idUsuario
        );

        Task<IEnumerable<CajaApertura>>
       GetByFechaAsync(
           int idEmpresa,
           DateTime desde,
           DateTime hasta
       );

        /* =====================================
        🔥 VALIDACIONES
        ===================================== */

        Task<bool> ExisteCajaAbiertaAsync(
            int idEmpresa,
            int idUsuario
        );

        /* =====================================
        🔥 APERTURA / CIERRE
        ===================================== */

        Task AbrirCajaAsync(
            CajaApertura entity
        );

        Task CerrarCajaAsync(
            int idCajaApertura
        );
    }
}
