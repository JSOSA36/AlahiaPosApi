using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AlahiaPos.Entities.Domain;
using AlahiaPosApi.Auth;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace AlahiaPosApi.Pase.Tests
{
    public class SucursalConsultaContratoTests
    {
        [Fact]
        public async Task Listado_sin_Bearer_usa_XIdUsuario()
        {
            var http = new DefaultHttpContext();
            http.Request.Headers[SucursalConsultaHttp.HeaderUsuario] = "44";
            var sucursales = new FakeConsulta();

            var (scope, error) = await SucursalConsultaHttp.ResolverAsync(
                http, new FakeTokens(), sucursales, idEmpresa: 58, idSucursalFiltro: 0);

            Assert.Null(error);
            Assert.Equal(44, sucursales.LastUsuario);
            Assert.Contains(1, scope.IdsPermitidos);
        }

        [Fact]
        public void Cajero_no_ve_otra_sucursal()
        {
            var scope = new SucursalConsultaScope
            {
                IdsPermitidos = new[] { 39 },
                IdPrincipal = 26,
                Sucursales = Array.Empty<SucursalSesionDto>()
            };

            Assert.True(scope.Incluye(39));
            Assert.False(scope.Incluye(26));
            Assert.False(scope.Incluye(null));
        }

        private sealed class FakeTokens : ISesionTokenResolver
        {
            public Task<SesionActual?> ResolverAsync(string? bearerOrToken, CancellationToken cancellationToken = default)
                => Task.FromResult<SesionActual?>(null);

            public Task<bool> AplicarSucursalHeaderAsync(
                SesionActual sesion, int idSucursalHeader, CancellationToken cancellationToken = default)
                => Task.FromResult(true);
        }

        private sealed class FakeConsulta : ISucursalService
        {
            public int LastUsuario;

            public Task<SucursalConsultaScope> ResolverConsultaAsync(
                int idUsuario, int idEmpresa, int? idSucursalFiltro, CancellationToken ct = default)
            {
                LastUsuario = idUsuario;
                return Task.FromResult(new SucursalConsultaScope
                {
                    IdsPermitidos = new[] { 1 },
                    Sucursales = Array.Empty<SucursalSesionDto>(),
                    IdPrincipal = 1
                });
            }

            public Task<Sucursal> AsegurarPrincipalAsync(int idEmpresa, CancellationToken ct = default)
                => throw new NotImplementedException();
            public Task AsegurarAccesoUsuarioAsync(int idUsuario, int idEmpresa, CancellationToken ct = default)
                => throw new NotImplementedException();
            public Task AsignarOperativaAsync(int idUsuario, int idEmpresa, int? idSucursal, bool esAdministrador, CancellationToken ct = default)
                => throw new NotImplementedException();
            public Task<IReadOnlyList<SucursalSesionDto>> ListarPorUsuarioAsync(int idUsuario, int idEmpresa, CancellationToken ct = default)
                => throw new NotImplementedException();
            public Task<bool> TieneAccesoAsync(int idUsuario, int idEmpresa, int idSucursal, CancellationToken ct = default)
                => throw new NotImplementedException();
            public Task<CambiarSucursalResultado> CambiarActivaAsync(int idUsuario, int idEmpresa, int idSucursal, string? dispositivo, string? ip, CancellationToken ct = default)
                => throw new NotImplementedException();
            public Task<int?> ResolverSucursalActivaAsync(int idUsuario, int idEmpresa, int? idSucursalActiva, CancellationToken ct = default)
                => throw new NotImplementedException();
        }
    }
}
