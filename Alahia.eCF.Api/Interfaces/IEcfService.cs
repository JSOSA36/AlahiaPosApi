namespace Alahia.eCF.Api.Interfaces
{
    public interface IEcfService
    {
        Task<string> ProcesarEcf(EcfRequestDto request);
    }
}
