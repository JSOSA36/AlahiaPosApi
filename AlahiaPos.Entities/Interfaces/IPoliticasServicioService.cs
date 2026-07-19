using System.Collections.Generic;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IPoliticasServicioService
    {
        Task<PoliticasEstadoDto> ObtenerEstadoAsync(int idEmpresa, int idUsuario);
        Task<AceptarPoliticasResultado> AceptarAsync(AceptarPoliticasRequest request);
        Task<List<PoliticasVersionDto>> ListarVersionesAsync();
        Task<PoliticasVersionDto?> ObtenerVersionAsync(int idVersion);
        Task<PoliticasVersionDto> CrearBorradorAsync(CrearPoliticasVersionRequest request);
        Task<PoliticasVersionDto> PublicarAsync(PublicarPoliticasRequest request);
        Task<List<PoliticasAceptacionDto>> ListarAceptacionesAsync(int? idVersion = null, int? idEmpresa = null);
        bool EsAdministrador(string? ocupacionRol);
    }
}
