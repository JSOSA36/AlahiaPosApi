using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IArsAseguradoraService
    {
        Task<IEnumerable<ArsAseguradora>> GetAll(int idEmpresa, bool soloActivos = true);
        Task<ArsAseguradora?> GetById(int idArs, int idEmpresa);
        Task<ArsAseguradora> Insert(ArsAseguradora ars);
        Task Update(int idArs, ArsAseguradora ars, int idEmpresa);
        Task SetActivo(int idArs, int idEmpresa, bool activo);
        Task<bool> EmpresaUsaArsAsync(int idEmpresa);

        Task<decimal> AplicarCoberturaEnVentaAsync(
            FacturaHeaders header,
            IEnumerable<PagoDTO> pagos,
            bool esAbonoInicialCredito);

        Task RegistrarPagoArsAsync(ArsPagoRequest request);
        Task<ArsPagoLoteResultadoDto> RegistrarPagoLoteArsAsync(ArsPagoLoteRequest request);

        Task<IEnumerable<ArsCuentaPorCobrarDto>> GetCuentasPorCobrarArsAsync(ArsResumenFiltroRequest filtro);
        Task<IEnumerable<ArsDocumentoCxCDto>> GetDocumentosArsAsync(ArsResumenFiltroRequest filtro);
        Task<IEnumerable<ArsPagoHistorialDto>> GetPagosArsAsync(int idEmpresa, int idFacturaHeader, int idArs);
        Task<IEnumerable<ArsVentasResumenDto>> GetVentasPorArsAsync(ArsResumenFiltroRequest filtro);
        Task<IEnumerable<ArsAntiguedadBucketDto>> GetAntiguedadArsAsync(ArsResumenFiltroRequest filtro);
        Task<IEnumerable<ArsDesgloseCajaDto>> GetDesgloseCajaAbiertaAsync(int idEmpresa, int idUsuario);
    }
}
