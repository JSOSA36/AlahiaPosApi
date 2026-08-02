using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IGastos
    {
        Task<IEnumerable<Gastos>> GetAllGastos(int IdEmpresa);
        Task<decimal> TotalGastosDelMes(int IdEmpresa);
        Task<Gastos> GetGastosById(int id);
        Task InsertGastos(Gastos Gastos);
        Task CerrarGastosPendientes(
    int idEmpresa,
    int idUsuario,
    int idCajaCierre
);
        void UpdateGastos(Gastos Gastos);

        Task AnularGastoAsync(
            int idGasto,
            int idEmpresa,
            string motivoAnulacion,
            string? usuarioAnulo);

        /// <summary>
        /// Flujo completo de registro de gasto (tesorería + fila + contabilidad).
        /// Usado por el módulo Gastos y por Conciliación Bancaria.
        /// </summary>
        Task<RegistrarGastoResult> RegistrarGastoCompletoAsync(RegistrarGastoRequest request);
    }
}
