using System.Threading;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace AlahiaPosApi.Hubs
{
    public class NotificacionesHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var http = Context.GetHttpContext();
            var idEmpresa = http?.Request.Query["idEmpresa"].ToString();
            var idUsuario = http?.Request.Query["idUsuario"].ToString();

            if (int.TryParse(idEmpresa, out var emp) && emp > 0)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"empresa:{emp}");

            if (int.TryParse(idUsuario, out var usr) && usr > 0)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"usuario:{usr}");

            await base.OnConnectedAsync();
        }
    }

    public class SignalRNotificacionRealtime : INotificacionRealtime
    {
        private readonly IHubContext<NotificacionesHub> _hub;

        public SignalRNotificacionRealtime(IHubContext<NotificacionesHub> hub)
        {
            _hub = hub;
        }

        public async Task EmitirNuevaAsync(NotificacionDto notificacion, CancellationToken ct = default)
        {
            var destino = (notificacion.DestinoTipo ?? "").Trim().ToUpperInvariant();

            // USUARIO: solo al destinatario (evita doble entrega vía grupo empresa)
            if (destino == "USUARIO" && notificacion.IdUsuarioDestino.HasValue)
            {
                await _hub.Clients.Group($"usuario:{notificacion.IdUsuarioDestino.Value}")
                    .SendAsync("notificacion:nueva", notificacion, ct);
                return;
            }

            // EMPRESA / ROL: broadcast a la empresa (filtro de rol en cliente/API)
            await _hub.Clients.Group($"empresa:{notificacion.IdEmpresa}")
                .SendAsync("notificacion:nueva", notificacion, ct);
        }
    }
}
