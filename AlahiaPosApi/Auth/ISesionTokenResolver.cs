using System.Threading;
using System.Threading.Tasks;

namespace AlahiaPosApi.Auth
{
    public interface ISesionTokenResolver
    {
        Task<SesionActual?> ResolverAsync(string? bearerOrToken, CancellationToken cancellationToken = default);

        Task<bool> AplicarSucursalHeaderAsync(
            SesionActual sesion,
            int idSucursalHeader,
            CancellationToken cancellationToken = default);
    }
}
