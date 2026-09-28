using AlahiaPosApi.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace AlahiaPosApi.Auth.Tests
{
    public sealed class FakeSesionTokenResolver : ISesionTokenResolver
    {
        public SesionActual? Sesion { get; set; }

        public Task<SesionActual?> ResolverAsync(string? bearerOrToken, CancellationToken cancellationToken = default)
        {
            var token = SesionHttp.ExtraerToken(bearerOrToken);
            if (string.IsNullOrWhiteSpace(token) || token == "invalid" || token == "ok")
                return Task.FromResult<SesionActual?>(null);
            return Task.FromResult(Sesion);
        }

        public Task<bool> AplicarSucursalHeaderAsync(
            SesionActual sesion,
            int idSucursalHeader,
            CancellationToken cancellationToken = default)
        {
            if (idSucursalHeader <= 0 || idSucursalHeader == sesion.IdSucursal)
                return Task.FromResult(true);
            return Task.FromResult(false);
        }
    }

    public class FacturaBodyDto
    {
        public int IdEmpresa { get; set; }
        public int IdFactura { get; set; }
    }

    public class SesionAuthFilterTests
    {
        private static SesionActual EmpresaA() => new()
        {
            IdUsuario = 10,
            IdEmpresa = 1,
            IdPerfil = 1,
            Estado = true,
            EsEmpresaSistema = false,
            UserName = "userA"
        };

        private static SesionActual MacroBits() => new()
        {
            IdUsuario = 99,
            IdEmpresa = 1,
            IdPerfil = 1,
            Estado = true,
            EsEmpresaSistema = true,
            UserName = "macro"
        };

        private static ActionExecutingContext CrearContexto(
            string? authorization,
            IDictionary<string, object>? args = null,
            params object[] metadata)
        {
            var http = new DefaultHttpContext();
            if (!string.IsNullOrEmpty(authorization))
                http.Request.Headers["Authorization"] = authorization;

            var endpoint = new Endpoint(
                _ => Task.CompletedTask,
                new EndpointMetadataCollection(metadata),
                "test");
            http.SetEndpoint(endpoint);

            var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
            return new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                args ?? new Dictionary<string, object>(),
                controller: new object());
        }

        private static async Task<(ActionExecutingContext ctx, bool nextCalled, SesionAuthFilter filter)> Ejecutar(
            ActionExecutingContext ctx,
            SesionActual? sesion)
        {
            var resolver = new FakeSesionTokenResolver { Sesion = sesion };
            var filter = new SesionAuthFilter(resolver);
            var nextCalled = false;
            await filter.OnActionExecutionAsync(ctx, () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(
                    ctx,
                    new List<IFilterMetadata>(),
                    ctx.Controller));
            });
            return (ctx, nextCalled, filter);
        }

        [Fact]
        public async Task TokenValido_PermiteAcceso()
        {
            var ctx = CrearContexto("Bearer valid-token");
            var (_, next, _) = await Ejecutar(ctx, EmpresaA());
            Assert.True(next);
            Assert.Null(ctx.Result);
            Assert.Equal(1, SesionHttp.TryGet(ctx.HttpContext)!.IdEmpresa);
        }

        [Fact]
        public async Task TokenInvalido_Retorna401()
        {
            var ctx = CrearContexto("Bearer invalid");
            var (_, next, _) = await Ejecutar(ctx, EmpresaA());
            Assert.False(next);
            var json = Assert.IsType<JsonResult>(ctx.Result);
            Assert.Equal(401, json.StatusCode);
        }

        [Fact]
        public async Task SinToken_Retorna401()
        {
            var ctx = CrearContexto(null);
            var (_, next, _) = await Ejecutar(ctx, EmpresaA());
            Assert.False(next);
            var json = Assert.IsType<JsonResult>(ctx.Result);
            Assert.Equal(401, json.StatusCode);
        }

        [Fact]
        public async Task UsuarioEmpresaA_AccesoEmpresaA_Ok()
        {
            var ctx = CrearContexto("Bearer valid", new Dictionary<string, object>
            {
                ["IdEmpresa"] = 1
            });
            var (_, next, _) = await Ejecutar(ctx, EmpresaA());
            Assert.True(next);
            Assert.Equal(1, ctx.ActionArguments["IdEmpresa"]);
        }

        [Fact]
        public async Task UsuarioEmpresaA_AccesoEmpresaB_403()
        {
            var ctx = CrearContexto("Bearer valid", new Dictionary<string, object>
            {
                ["IdEmpresa"] = 2
            });
            var (_, next, _) = await Ejecutar(ctx, EmpresaA());
            Assert.False(next);
            var json = Assert.IsType<JsonResult>(ctx.Result);
            Assert.Equal(403, json.StatusCode);
        }

        [Fact]
        public async Task IdEmpresaManipuladaEnBody_403()
        {
            var ctx = CrearContexto("Bearer valid", new Dictionary<string, object>
            {
                ["dto"] = new FacturaBodyDto { IdEmpresa = 2, IdFactura = 99 }
            });
            var (_, next, _) = await Ejecutar(ctx, EmpresaA());
            Assert.False(next);
            Assert.Equal(403, Assert.IsType<JsonResult>(ctx.Result).StatusCode);
        }

        [Fact]
        public async Task IdEmpresaManipuladaEnQuery_SeIgnoraORechaza()
        {
            var ctx = CrearContexto("Bearer valid", new Dictionary<string, object>
            {
                ["idEmpresa"] = 2
            });
            var (_, next, _) = await Ejecutar(ctx, EmpresaA());
            Assert.False(next);
            Assert.Equal(403, Assert.IsType<JsonResult>(ctx.Result).StatusCode);
        }

        [Fact]
        public async Task IdEmpresaCeroEnQuery_SeSustituyePorSesion()
        {
            var ctx = CrearContexto("Bearer valid", new Dictionary<string, object>
            {
                ["idEmpresa"] = 0
            });
            var (_, next, _) = await Ejecutar(ctx, EmpresaA());
            Assert.True(next);
            Assert.Equal(1, ctx.ActionArguments["idEmpresa"]);
        }

        [Fact]
        public async Task IdEmpresaManipuladaEnRoute_403()
        {
            var ctx = CrearContexto("Bearer valid", new Dictionary<string, object>
            {
                ["IdEmpresa"] = 99
            });
            var (_, next, _) = await Ejecutar(ctx, EmpresaA());
            Assert.False(next);
            Assert.Equal(403, Assert.IsType<JsonResult>(ctx.Result).StatusCode);
        }

        [Fact]
        public async Task EndpointPublico_SinToken_Continua()
        {
            var ctx = CrearContexto(null, metadata: new AllowAnonymousAttribute());
            var (_, next, _) = await Ejecutar(ctx, EmpresaA());
            Assert.True(next);
            Assert.Null(ctx.Result);
        }

        [Fact]
        public async Task UsuarioSinPermisoMacroBits_403()
        {
            var ctx = CrearContexto("Bearer valid", metadata: new RequiereEmpresaSistemaAttribute());
            var (_, next, _) = await Ejecutar(ctx, EmpresaA());
            Assert.False(next);
            Assert.Equal(403, Assert.IsType<JsonResult>(ctx.Result).StatusCode);
        }

        [Fact]
        public async Task MacroBits_PuedeEmpresaObjetivo()
        {
            var ctx = CrearContexto(
                "Bearer valid",
                new Dictionary<string, object> { ["IdEmpresa"] = 56 },
                new PermitirEmpresaObjetivoAttribute(),
                new RequiereEmpresaSistemaAttribute());
            var (_, next, _) = await Ejecutar(ctx, MacroBits());
            Assert.True(next);
        }
    }

    public class TenantIdEmpresaGuardTests
    {
        [Fact]
        public void RecursoOtraEmpresa_NoEsDeLaSesion()
        {
            var http = new DefaultHttpContext();
            SesionHttp.Set(http, new SesionActual { IdEmpresa = 1, IdUsuario = 1, Estado = true });
            Assert.False(TenantRecurso.EsDeLaSesion(http, 2));
            Assert.IsType<NotFoundResult>(TenantRecurso.RechazarSiOtraEmpresa(http, 2));
        }

        [Fact]
        public void RecursoMismaEmpresa_EsDeLaSesion()
        {
            var http = new DefaultHttpContext();
            SesionHttp.Set(http, new SesionActual { IdEmpresa = 1, IdUsuario = 1, Estado = true });
            Assert.True(TenantRecurso.EsDeLaSesion(http, 1));
            Assert.Null(TenantRecurso.RechazarSiOtraEmpresa(http, 1));
        }

        [Fact]
        public void ExtraerToken_BearerYCrudo()
        {
            Assert.Equal("abc", SesionHttp.ExtraerToken("Bearer abc"));
            Assert.Equal("abc", SesionHttp.ExtraerToken("abc"));
            Assert.Null(SesionHttp.ExtraerToken("  "));
        }
    }

    public class TenantSucursalGuardTests
    {
        private static SesionActual ConSucursal() => new()
        {
            IdUsuario = 10,
            IdEmpresa = 1,
            IdSucursal = 5,
            IdPerfil = 1,
            Estado = true,
            UserName = "userA"
        };

        [Fact]
        public void IdSucursalCero_SeSustituyePorSesion()
        {
            var args = new Dictionary<string, object> { ["idSucursal"] = 0 };
            var r = TenantSucursalGuard.Apply(args, 5, omitir: false);
            Assert.False(r.Forbidden);
            Assert.Equal(5, args["idSucursal"]);
        }

        [Fact]
        public void IdSucursalAjeno_Forbidden()
        {
            var args = new Dictionary<string, object> { ["idSucursal"] = 9 };
            var r = TenantSucursalGuard.Apply(args, 5, omitir: false);
            Assert.True(r.Forbidden);
        }

        [Fact]
        public void SesionSinSucursal_NoBloquea()
        {
            var args = new Dictionary<string, object> { ["idSucursal"] = 9 };
            var r = TenantSucursalGuard.Apply(args, 0, omitir: false);
            Assert.False(r.Forbidden);
            Assert.Equal(9, args["idSucursal"]);
        }

        [Fact]
        public async Task HeaderSucursalAjeno_403()
        {
            var http = new DefaultHttpContext();
            http.Request.Headers["Authorization"] = "Bearer valid";
            http.Request.Headers["X-IdSucursal"] = "99";
            var endpoint = new Endpoint(
                _ => Task.CompletedTask,
                new EndpointMetadataCollection(),
                "test");
            http.SetEndpoint(endpoint);
            var ctx = new ActionExecutingContext(
                new ActionContext(http, new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                controller: new object());

            var resolver = new FakeSesionTokenResolver { Sesion = ConSucursal() };
            var filter = new SesionAuthFilter(resolver);
            var nextCalled = false;
            await filter.OnActionExecutionAsync(ctx, () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(ctx, new List<IFilterMetadata>(), ctx.Controller));
            });

            Assert.False(nextCalled);
            Assert.Equal(403, Assert.IsType<JsonResult>(ctx.Result).StatusCode);
        }

        [Fact]
        public async Task BodySucursalAjena_403()
        {
            var http = new DefaultHttpContext();
            http.Request.Headers["Authorization"] = "Bearer valid";
            var endpoint = new Endpoint(
                _ => Task.CompletedTask,
                new EndpointMetadataCollection(),
                "test");
            http.SetEndpoint(endpoint);
            var ctx = new ActionExecutingContext(
                new ActionContext(http, new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>(),
                new Dictionary<string, object> { ["idSucursal"] = 9 },
                controller: new object());

            var resolver = new FakeSesionTokenResolver { Sesion = ConSucursal() };
            var filter = new SesionAuthFilter(resolver);
            var nextCalled = false;
            await filter.OnActionExecutionAsync(ctx, () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(ctx, new List<IFilterMetadata>(), ctx.Controller));
            });

            Assert.False(nextCalled);
            Assert.Equal(403, Assert.IsType<JsonResult>(ctx.Result).StatusCode);
        }
    }
}
