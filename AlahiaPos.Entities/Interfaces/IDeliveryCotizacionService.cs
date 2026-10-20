using System.Threading;
using System.Threading.Tasks;
using AlahiaPos.Entities.Delivery;
using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IDeliveryCotizacionService
    {
        Task<DeliveryCotizacionDto> CotizarAsync(
            int idEmpresa,
            int? idSucursal,
            bool esDelivery,
            decimal? latitud,
            decimal? longitud,
            CancellationToken cancellationToken);

        Task<DeliverySnapshot> ResolverParaPedidoAsync(
            int idEmpresa,
            int? idSucursal,
            bool esDelivery,
            decimal? latitud,
            decimal? longitud,
            CancellationToken cancellationToken);

        Task<DeliveryConfigPantallaDto> ObtenerPantallaAsync(int idEmpresa, int idSucursalSesion);

        Task<DeliveryConfigPantallaDto> GuardarAsync(
            int idEmpresa,
            int idSucursalSesion,
            int idUsuario,
            DeliveryConfigGuardarRequest request);
    }
}
