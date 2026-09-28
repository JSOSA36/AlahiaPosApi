using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AlahiaPosApi.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AlahiaPosApi.Auth.Tests
{
    public class SucursalConsultaHttpTests
    {
        [Fact]
        public async Task SinSesion_UsaXIdUsuario()
        {
            var http = new DefaultHttpContext();
            http.Request.Headers["X-IdUsuario"] = "44";
            var tokens = new FakeSesionTokenResolver();
            var sucursales = new FakeConsultaSucursales();

            var (scope, error) = await SucursalConsultaHttp.ResolverAsync(
                http, tokens, sucursales, idEmpresa: 58, idSucursalFiltro: 0);

            Assert.Null(error);
            Assert.Equal(44, sucursales.LastUsuario);
            Assert.Equal(58, sucursales.LastEmpresa);
            Assert.Equal(0, sucursales.LastFiltro);
            Assert.Contains(1, scope.IdsPermitidos);
        }

        [Fact]
        public async Task BearerInvalido_UsaXIdUsuario()
        {
            var http = new DefaultHttpContext();
            http.Request.Headers["Authorization"] = "Bearer ok";
            http.Request.Headers["X-IdUsuario"] = "9";
            var tokens = new FakeSesionTokenResolver();
            var sucursales = new FakeConsultaSucursales();

            var (scope, error) = await SucursalConsultaHttp.ResolverAsync(
                http, tokens, sucursales, idEmpresa: 5, idSucursalFiltro: null);

            Assert.Null(error);
            Assert.Equal(9, sucursales.LastUsuario);
            Assert.NotEmpty(scope.IdsPermitidos);
        }

        [Fact]
        public async Task SinSesionNiUsuario_401()
        {
            var http = new DefaultHttpContext();
            var tokens = new FakeSesionTokenResolver();
            var sucursales = new FakeConsultaSucursales();

            var (_, error) = await SucursalConsultaHttp.ResolverAsync(
                http, tokens, sucursales, idEmpresa: 5, idSucursalFiltro: 0);

            var json = Assert.IsType<JsonResult>(error);
            Assert.Equal(401, json.StatusCode);
        }

        [Fact]
        public async Task SesionDeOtraEmpresa_403()
        {
            var http = new DefaultHttpContext();
            http.Request.Headers["Authorization"] = "Bearer vigente";
            var tokens = new FakeSesionTokenResolver
            {
                Sesion = new SesionActual
                {
                    IdUsuario = 1,
                    IdEmpresa = 5,
                    Estado = true,
                    UserName = "a"
                }
            };
            var sucursales = new FakeConsultaSucursales();

            var (_, error) = await SucursalConsultaHttp.ResolverAsync(
                http, tokens, sucursales, idEmpresa: 58, idSucursalFiltro: 0);

            var json = Assert.IsType<JsonResult>(error);
            Assert.Equal(403, json.StatusCode);
            Assert.Equal(0, sucursales.LastUsuario);
        }

        private sealed class FakeConsultaSucursales : ISucursalService
        {
            public int LastUsuario;
            public int LastEmpresa;
            public int? LastFiltro;

            public Task<SucursalConsultaScope> ResolverConsultaAsync(
                int idUsuario,
                int idEmpresa,
                int? idSucursalFiltro,
                CancellationToken ct = default)
            {
                LastUsuario = idUsuario;
                LastEmpresa = idEmpresa;
                LastFiltro = idSucursalFiltro;
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
