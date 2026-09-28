using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IAlmacenExistencia
    {
        Task<AlmacenExistencia?> GetExistencia(
            int idAlmacen,
            int idProducto,
            int idEmpresa);

        Task<decimal> GetTotalPorProducto(
            int idProducto,
            int idEmpresa,
            int? idSucursal = null);

        Task<List<AlmacenExistenciaDto>> GetDetallePorProducto(
            int idProducto,
            int idEmpresa,
            int? idSucursal = null);

        Task AjustarExistencia(
            int idAlmacen,
            int idProducto,
            int idEmpresa,
            decimal delta);

        Task SincronizarCantidadProducto(int idProducto);
    }
}
