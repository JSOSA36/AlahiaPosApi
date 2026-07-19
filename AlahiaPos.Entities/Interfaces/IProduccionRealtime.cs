using System.Threading;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IProduccionRealtime
    {
        Task EmitirTrabajoUpsertAsync(ProduccionTrabajoDto trabajo, CancellationToken ct = default);
        Task EmitirTrabajoEstadoAsync(ProduccionTrabajoDto trabajo, CancellationToken ct = default);
    }
}
