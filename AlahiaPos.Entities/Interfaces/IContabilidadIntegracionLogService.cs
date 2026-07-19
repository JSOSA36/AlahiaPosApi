using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IContabilidadIntegracionLogService
    {
        Task RegistrarAsync(
            int idEmpresa,
            string estado,
            string? mensaje = null,
            int? idEventoOutbox = null,
            string? origenModulo = null,
            int? origenReferenciaId = null,
            string? tipoOperacion = null,
            int? idAsientoContable = null);
    }
}
