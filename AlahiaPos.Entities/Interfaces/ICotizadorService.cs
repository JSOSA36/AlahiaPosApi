using System.Collections.Generic;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto.Cotizador;

namespace AlahiaPos.Entities.Interfaces
{
    public interface ICotizadorRecomendador
    {
        Task<IReadOnlyList<RecomendacionModuloDto>> RecomendarAsync(
            CotizadorCalcularRequest request,
            IReadOnlyList<RecomendacionModuloDto> recomendacionesReglas,
            CotizadorPropuestaDto propuestaParcial);
    }

    public interface ICotizadorService
    {
        Task<CotizadorCatalogoDto> ObtenerCatalogoAsync();
        Task<CotizadorPropuestaDto> CalcularAsync(CotizadorCalcularRequest request);
        Task<CotizadorGuardarResponse> GuardarAsync(CotizadorGuardarRequest request);
        Task EnviarCorreoAsync(CotizadorEnviarCorreoRequest request);
        Task SolicitarAsync(CotizadorSolicitarRequest request);
        Task<CotizadorVistaPublicaDto?> ObtenerVistaPublicaAsync(string folio);
    }
}
