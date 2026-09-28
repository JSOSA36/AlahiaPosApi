using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IPosTerminalService
    {
        Task<PosTerminalClaimResult> ClaimAsync(
            int idEmpresa,
            int idUsuario,
            PosTerminalClaimRequest req,
            bool esEmpresaSistema,
            CancellationToken ct = default);

        Task<List<PosTerminalDto>> ListarAsync(int idEmpresa, CancellationToken ct = default);

        Task RevocarAsync(int idEmpresa, int idPosTerminal, int idUsuarioAdmin, CancellationToken ct = default);

        Task<int> ContarActivosAsync(int idEmpresa, CancellationToken ct = default);
    }
}
