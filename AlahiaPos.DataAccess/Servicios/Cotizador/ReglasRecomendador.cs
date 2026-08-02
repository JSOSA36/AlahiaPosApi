using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto.Cotizador;
using AlahiaPos.Entities.Interfaces;

namespace AlahiaPos.DataAccess.Servicios.Cotizador
{
    /// <summary>
    /// Recomendador v1 basado en reglas/dependencias.
    /// Un futuro IaRecomendador puede priorizar/explicar, pero NUNCA fijar precios.
    /// </summary>
    public class ReglasRecomendador : ICotizadorRecomendador
    {
        public Task<IReadOnlyList<RecomendacionModuloDto>> RecomendarAsync(
            CotizadorCalcularRequest request,
            IReadOnlyList<RecomendacionModuloDto> recomendacionesReglas,
            CotizadorPropuestaDto propuestaParcial)
        {
            // Hoy: las dependencias ya son la fuente. La IA no inventa módulos ni precios.
            IReadOnlyList<RecomendacionModuloDto> result = recomendacionesReglas
                .GroupBy(r => r.Codigo + "|" + r.Tipo)
                .Select(g => g.First())
                .ToList();

            return Task.FromResult(result);
        }
    }
}
