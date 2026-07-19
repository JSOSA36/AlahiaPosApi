using System.Threading;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace AlahiaPosApi.Hubs
{
    public class ProduccionHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var http = Context.GetHttpContext();
            var idEmpresa = http?.Request.Query["idEmpresa"].ToString();

            if (int.TryParse(idEmpresa, out var emp) && emp > 0)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"produccion:empresa:{emp}");

            await base.OnConnectedAsync();
        }
    }

    public class SignalRProduccionRealtime : IProduccionRealtime
    {
        private readonly IHubContext<ProduccionHub> _hub;

        public SignalRProduccionRealtime(IHubContext<ProduccionHub> hub)
        {
            _hub = hub;
        }

        public Task EmitirTrabajoUpsertAsync(ProduccionTrabajoDto trabajo, CancellationToken ct = default)
        {
            return _hub.Clients.Group($"produccion:empresa:{trabajo.IdEmpresa}")
                .SendAsync("produccion:trabajoUpsert", trabajo, ct);
        }

        public Task EmitirTrabajoEstadoAsync(ProduccionTrabajoDto trabajo, CancellationToken ct = default)
        {
            return _hub.Clients.Group($"produccion:empresa:{trabajo.IdEmpresa}")
                .SendAsync("produccion:trabajoEstado", trabajo, ct);
        }
    }
}
