using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface INotificacionCentro
    {
        Task<NotificacionDto> PublicarAsync(NotificacionEvento evento, CancellationToken ct = default);
        Task<List<NotificacionDto>> ListarAsync(int idEmpresa, int? idUsuario, bool soloNoLeidas = false, int top = 50, bool incluirArchivadas = false);
        Task<int> ContarNoLeidasAsync(int idEmpresa, int? idUsuario);
        Task MarcarLeidaAsync(int idNotificacion, int idEmpresa, int? idUsuario);
        Task MarcarNoLeidaAsync(int idNotificacion, int idEmpresa, int? idUsuario);
        Task MarcarTodasAsync(int idEmpresa, int? idUsuario);
        Task ArchivarAsync(int idNotificacion, int idEmpresa, int? idUsuario);
    }

    public interface INotificacionCanal
    {
        string Canal { get; }
        Task EnviarAsync(NotificacionEvento evento, NotificacionDto creada, CancellationToken ct = default);
    }

    public interface INotificacionRealtime
    {
        Task EmitirNuevaAsync(NotificacionDto notificacion, CancellationToken ct = default);
    }
}
