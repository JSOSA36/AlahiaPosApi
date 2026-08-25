using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IFacturaCompraImagenService
    {
        Task<FacturaCompraImagenResultadoDto> InterpretarAsync(
            FacturaCompraInterpretarRequest request,
            CancellationToken ct = default);
    }
}
