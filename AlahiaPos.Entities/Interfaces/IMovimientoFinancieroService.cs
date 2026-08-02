using AlahiaPos.Entities.Dto;
using System;
using System.Collections.Generic;
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

        Task<IEnumerable<MovimientoFinancieroListadoDto>>
            ConsultarAsync(MovimientoFinancieroFiltroDto filtro);

        Task<EstadoCuentaDto>
            GetEstadoCuentaAsync(
                int idCuentaFinanciera,
                DateTime? desde = null,
                DateTime? hasta = null);

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

        /// <summary>
        /// Transferencia Caja→Banco (o inversa) originada en reclasificación de pago.
        /// Permite saldo negativo en origen cuando el banco es la fuente de verdad.
        /// </summary>
        Task<int> RegistrarTransferenciaReclasificacionAsync(
            int idEmpresa,
            int idUsuario,
            int idCuentaOrigen,
            int idCuentaDestino,
            decimal monto,
            string motivo,
            string? observacion,
            string claveIdempotencia,
            int? referenciaId,
            DateTime fechaMovimiento);

        Task<int> RegistrarAjusteAsync(RegistrarAjusteDto dto);

        Task AnularMovimientoAsync(AnularMovimientoDto dto);
    }
}
