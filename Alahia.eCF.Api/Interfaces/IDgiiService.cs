namespace Alahia.eCF.Api.Interfaces
{
    public interface IDgiiService
    {
        Task<string> ObtenerSemilla();

        Task<string> ObtenerToken(string semillaFirmada);

        Task<string> EnviarEcf(byte[] xmlFirmado, string fileName, string token);

        Task<string> ConsultarEstado(string trackId, string token);
    }
}
