using AlahiaPos.Entities.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IMovimientoFinancieroService
    {
        Task<int> CreateAsync(
           MovimientoFinanciero entity
       );

        Task UpdateAsync(
            MovimientoFinanciero entity
        );

        Task DeleteAsync(
            int id
        );

        Task<MovimientoFinanciero?>
            GetByIdAsync(
                int id
            );

        Task<IEnumerable<MovimientoFinanciero>>
            GetAllAsync();

        Task<IEnumerable<MovimientoFinanciero>>
            GetByEmpresaAsync(
                int idEmpresa
            );

        Task<IEnumerable<MovimientoFinanciero>>
            GetByCuentaAsync(
                int idCuentaFinanciera
            );

        Task<IEnumerable<MovimientoFinanciero>>
            GetByFechaAsync(

                int idEmpresa,

                DateTime desde,

                DateTime hasta
            );

        /* =========================================
        🔥 OPERACIONES
        ========================================= */

        Task RegistrarEntradaAsync(

            int idEmpresa,

            int idUsuario,

            int idCuentaDestino,

            decimal monto,

            string motivo,

            string? observacion,

            string? categoria = null,

            int? referenciaId = null,

            string? referenciaTipo = null,

            string? claveIdempotencia = null
        );

        Task RegistrarSalidaAsync(

            int idEmpresa,

            int idUsuario,

            int idCuentaOrigen,

            decimal monto,

            string motivo,

            string? observacion,

            string? categoria = null,

            int? referenciaId = null,

            string? referenciaTipo = null,

            string? claveIdempotencia = null
        );

        Task RegistrarTransferenciaAsync(

            int idEmpresa,

            int idUsuario,

            int idCuentaOrigen,

            int idCuentaDestino,

            decimal monto,

            string motivo,

            string? observacion,

            string? claveIdempotencia = null
        );
    }
}

