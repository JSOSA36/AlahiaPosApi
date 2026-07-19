using System.Collections.Generic;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface ITicketsService
    {
        Task<TicketDetalleDto> CrearAsync(CrearTicketDto dto);
        Task<TicketDetalleDto> CrearDesdeLoginAsync(CrearTicketDesdeLoginDto dto);
        Task<List<TicketListItemDto>> ListarPorEmpresaAsync(int idEmpresa);
        Task<List<TicketListItemDto>> ListarAdminAsync(TicketFiltroAdminDto filtro);
        Task<TicketDetalleDto> ObtenerAsync(int idTicket, int? idEmpresaCliente);
        Task<TicketDetalleDto> AgregarMensajeAsync(AgregarTicketMensajeDto dto);
        Task<TicketDetalleDto> CambiarEstadoAsync(CambiarTicketEstadoDto dto);
        Task<TicketMetricasDto> ObtenerMetricasAsync(int? idEmpresa);
        Task<List<TicketNotificacionDto>> ListarNotificacionesAsync(int idEmpresa, int? idUsuario, bool soloNoLeidas = false);
        Task MarcarNotificacionLeidaAsync(int idNotificacion, int idEmpresa);
        Task MarcarTodasLeidasAsync(int idEmpresa, int? idUsuario);
    }
}
