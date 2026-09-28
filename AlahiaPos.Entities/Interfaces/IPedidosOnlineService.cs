using System.Collections.Generic;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IPedidosOnlineService
    {
        Task<PedidoOnlineMenuDto> ObtenerMenuAsync(string slug);
        Task<PedidoOnlineConfirmacionDto> CrearPedidoAsync(string slug, PedidoOnlineCheckoutRequest request);
        Task<PedidoOnlineSeguimientoDto> ObtenerSeguimientoAsync(string slug, int idPedidoOnline, string telefono);
        Task<PedidoOnlinePerfilDto> ObtenerPerfilClienteAsync(string slug, string telefono);
        Task<PedidoOnlinePerfilDto> GuardarPerfilClienteAsync(string slug, PedidoOnlinePerfilRequest request);
        Task<List<PedidoOnlineHistorialItemDto>> ListarPedidosClienteAsync(string slug, string telefono);

        Task<List<PedidoDeliveryListadoDto>> ListarColaDeliveryAsync(int idEmpresa);
        Task<List<PedidoDeliveryListadoDto>> ListarTodosAsync(int idEmpresa);
        Task<List<PedidoDeliveryListadoDto>> ListarMisPedidosAsync(int idEmpresa, int idUsuario);
        Task<PedidoDeliveryListadoDto?> ObtenerPedidoAsync(int idEmpresa, int idPedidoOnline);

        Task<List<DeliveryRepartidorDto>> ListarRepartidoresAsync(int idEmpresa);
        Task<DeliveryRepartidorDto> UpsertRepartidorAsync(int idEmpresa, DeliveryRepartidorUpsertRequest request);
        Task<PedidoDeliveryListadoDto> AsignarAsync(int idEmpresa, DeliveryAsignarRequest request);
        Task<PedidoDeliveryListadoDto> TransicionarDeliveryAsync(int idEmpresa, int idPedidoOnline, DeliveryTransicionRequest request);
        Task<PedidoDeliveryListadoDto> ValidarPagoAsync(int idEmpresa, int idPedidoOnline, int idUsuario);
        Task<(byte[] Contenido, string ContentType)> ObtenerVoucherAsync(int idEmpresa, int idPedidoOnline);
        Task<PedidoDeliveryListadoDto> EnviarACocinaAsync(int idEmpresa, int idPedidoOnline, int idUsuario);
        Task<PedidoOnlineCanalEmpresaDto?> ObtenerCanalEmpresaAsync(int idEmpresa);
    }
}
