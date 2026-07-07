using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface ICajaMovimientoService
    {
        Task<int> CreateAsync(CajaMovimiento entity);

        Task UpdateAsync(CajaMovimiento entity);

        Task DeleteAsync(int id);

        Task<CajaMovimiento?> GetByIdAsync(int id);

        /* =====================================
        🔥 CONSULTAS
        ===================================== */

        Task<IEnumerable<CajaMovimiento>> GetAllAsync();

        Task<IEnumerable<CajaMovimiento>> GetByCajaAsync(
            int idCajaApertura
        );

        Task<IEnumerable<CajaMovimiento>> GetByFechaAsync(
            DateTime desde,
            DateTime hasta
        );

        /* =====================================
        🔥 MOVIMIENTOS
        ===================================== */

        Task RegistrarEntradaAsync(
            CajaMovimiento entity
        );

        Task RegistrarSalidaAsync(
            CajaMovimiento entity
        );

        /* =====================================
        🔥 TOTALES
        ===================================== */

        Task<decimal> GetTotalEntradasAsync(
            int idCajaApertura
        );

        Task<decimal> GetTotalSalidasAsync(
            int idCajaApertura
        );
    }
}
    

