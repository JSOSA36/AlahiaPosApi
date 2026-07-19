using System.Threading.Tasks;
using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IAntiguedadSaldosService
    {
        Task<AntiguedadSaldosReporteDto> ObtenerAntiguedadCxCAsync(AntiguedadSaldosFiltroRequest filtro);
        Task<AntiguedadSaldosReporteDto> ObtenerAntiguedadCxPAsync(AntiguedadSaldosFiltroRequest filtro);
    }
}
