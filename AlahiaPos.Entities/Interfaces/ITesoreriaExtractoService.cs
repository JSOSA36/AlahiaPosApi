using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface ITesoreriaExtractoService
    {
        Task<TesoreriaExtractoImport> ImportarCsvAsync(ImportarExtractoDto dto);
        Task<TesoreriaExtractoImport> ImportarAsync(ImportarExtractoDto dto);

        // Flujo PREVIEW editable -> Confirmar -> matching existente.
        Task<ExtractoPreviewDto> PreviewAsync(ImportarExtractoDto dto);
        Task<ExtractoPreviewDto> GetPreviewAsync(int idTesoreriaExtractoImport, int idEmpresa);
        Task<ExtractoPreviewDto> ActualizarLineasPreviewAsync(
            int idTesoreriaExtractoImport,
            int idEmpresa,
            ActualizarLineasPreviewDto dto);
        Task<TesoreriaExtractoImport> ConfirmarPreviewAsync(
            int idTesoreriaExtractoImport,
            int idEmpresa,
            int idUsuario,
            int toleranciaDiasMatch = 3);
        Task DescartarPreviewAsync(int idTesoreriaExtractoImport, int idEmpresa, int idUsuario);
        Task<IEnumerable<ExtractoLineaMatchDto>> SugerirMatchesAsync(int idTesoreriaExtractoImport, int idEmpresa);
        Task ConfirmarMatchAsync(ConfirmarExtractoMatchDto dto);
        Task DescartarLineaAsync(int idTesoreriaExtractoLinea, int idEmpresa, int idUsuario);
        Task<int> CrearMovimientoDesdeLineaAsync(CrearMovimientoDesdeExtractoDto dto);
        Task<TesoreriaExtractoImport?> GetImportByIdAsync(int idTesoreriaExtractoImport, int idEmpresa);
        Task<IEnumerable<TesoreriaExtractoLinea>> GetLineasAsync(int idTesoreriaExtractoImport, int idEmpresa);
        Task<ResolverExtractoLineaResultadoDto> ResolverLineaAsync(ResolverExtractoLineaDto dto);
        Task<ExtractoResumenDto> GetResumenAsync(int idTesoreriaExtractoImport, int idEmpresa, decimal tolerancia = 0.01m);
        Task<ExtractoResumenDto> CerrarAsync(int idTesoreriaExtractoImport, int idEmpresa, int idUsuario, decimal tolerancia = 0.01m);
    }
}
