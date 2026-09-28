using AlahiaPos.Entities.Dto;
using Xunit;

namespace AlahiaPosApi.Auth.Tests
{
    public class SucursalConsultaScopeTests
    {
        [Fact]
        public void Cajero_no_ve_documentos_de_otra_sucursal_ni_sin_sucursal()
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
            Assert.False(scope.Incluye(0));
        }

        [Fact]
        public void Admin_consolidado_atribuye_sin_sucursal_a_la_principal()
        {
            var scope = new SucursalConsultaScope
            {
                IdsPermitidos = new[] { 26, 39 },
                IdPrincipal = 26,
                Sucursales = Array.Empty<SucursalSesionDto>()
            };

            Assert.True(scope.Incluye(null));
            Assert.True(scope.Incluye(26));
            Assert.True(scope.Incluye(39));
        }

        [Fact]
        public void Admin_filtrado_a_sucursal_no_incluye_documentos_huerfanos()
        {
            var scope = new SucursalConsultaScope
            {
                IdsPermitidos = new[] { 39 },
                IdPrincipal = 26,
                Sucursales = Array.Empty<SucursalSesionDto>()
            };

            Assert.False(scope.Incluye(null));
            Assert.True(scope.Incluye(39));
        }
    }
}
