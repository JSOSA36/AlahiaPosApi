using AlahiaPosApi.Auth;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace AlahiaPosApi.Pase.Tests
{
    /// <summary>
    /// Regresión 2026-09-09: guardar orden devolvía 401 "Token de sesión requerido"
    /// porque el vivo no manda Bearer obligatorio.
    /// </summary>
    public class IdUsuarioCobroResolverTests
    {
        [Fact]
        public async Task SinBearer_usa_XIdUsuario()
        {
            var http = new DefaultHttpContext();
            http.Request.Headers[SucursalConsultaHttp.HeaderUsuario] = "27";
            var tokens = new FakeTokens();

            var id = await IdUsuarioCobroResolver.ResolverAsync(http, tokens, idUsuarioDto: 99);

            Assert.Equal(27, id);
        }

        [Fact]
        public async Task SinBearer_ni_header_usa_IdUsuario_del_POS()
        {
            var http = new DefaultHttpContext();
            var tokens = new FakeTokens();

            var id = await IdUsuarioCobroResolver.ResolverAsync(http, tokens, idUsuarioDto: 44);

            Assert.Equal(44, id);
        }

        [Fact]
        public async Task Bearer_valido_gana_sobre_header_y_dto()
        {
            var http = new DefaultHttpContext();
            http.Request.Headers["Authorization"] = "Bearer vigente";
            http.Request.Headers[SucursalConsultaHttp.HeaderUsuario] = "9";
            var tokens = new FakeTokens
            {
                Sesion = new SesionActual { IdUsuario = 12, IdEmpresa = 59, Estado = true, UserName = "a" }
            };

            var id = await IdUsuarioCobroResolver.ResolverAsync(http, tokens, idUsuarioDto: 99);

            Assert.Equal(12, id);
        }

        [Fact]
        public async Task Bearer_invalido_cae_a_XIdUsuario_no_a_cero()
        {
            var http = new DefaultHttpContext();
            http.Request.Headers["Authorization"] = "Bearer ok";
            http.Request.Headers[SucursalConsultaHttp.HeaderUsuario] = "26";
            var tokens = new FakeTokens();

            var id = await IdUsuarioCobroResolver.ResolverAsync(http, tokens, idUsuarioDto: 1);

            Assert.Equal(26, id);
        }

        [Fact]
        public async Task Sin_usuario_devuelve_cero_no_lanza()
        {
            var http = new DefaultHttpContext();
            var id = await IdUsuarioCobroResolver.ResolverAsync(http, new FakeTokens(), null);
            Assert.Equal(0, id);
        }

        [Fact]
        public void Guardar_orden_y_cobro_usan_el_resolver_no_401_de_token()
        {
            var src = PaseRepo.ReadApiFile(Path.Combine("Controllers", "FacturaHeaderController.cs"));
            Assert.Contains("IdUsuarioCobroResolver.ResolverAsync", src);
            Assert.DoesNotContain("if (!TryIdUsuarioSesion(out var idUsuarioCobro))", src);
            Assert.DoesNotContain("if (!TryIdUsuarioSesion(out var idUsuarioSesion))\n                    return Unauthorized", src.Replace("\r\n", "\n"));
        }

        private sealed class FakeTokens : ISesionTokenResolver
        {
            public SesionActual? Sesion { get; set; }

            public Task<SesionActual?> ResolverAsync(string? bearerOrToken, CancellationToken cancellationToken = default)
            {
                var token = SesionHttp.ExtraerToken(bearerOrToken);
                if (string.IsNullOrWhiteSpace(token) || token == "ok" || token == "invalid")
                    return Task.FromResult<SesionActual?>(null);
                return Task.FromResult(Sesion);
            }

            public Task<bool> AplicarSucursalHeaderAsync(
                SesionActual sesion,
                int idSucursalHeader,
                CancellationToken cancellationToken = default)
                => Task.FromResult(true);
        }
    }
}
