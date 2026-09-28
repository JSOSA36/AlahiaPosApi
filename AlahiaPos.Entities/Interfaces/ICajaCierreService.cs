using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface ICajaCierreService
    {
        Task<int> CreateAsync(CajaCierre entity);
        Task<CajaListadoDto>
        ImprimirCierre(

        int idCajaCierre
         );
        Task UpdateAsync(CajaCierre entity);
        Task<IEnumerable<CajaListadoDto>>
 GetByFechaAsync(
     int idEmpresa,
     DateTime desde,
     DateTime hasta
 );
        Task DeleteAsync(int id);

        Task<CajaCierre?> GetByIdAsync(int id);

        /* =====================================
        🔥 CONSULTAS
        ===================================== */

        Task<IEnumerable<CajaListadoDto>>
  GetAllAsync();


        Task<CajaCierre?> GetUltimoCierreAsync(
            int idEmpresa,
            int idUsuario = 0
        );

        /* =====================================
        🔥 CIERRE
        ===================================== */

        Task<CajaCierre>
        ProcesarCierreAsync(
            CajaCierre model
        );
    }
}
