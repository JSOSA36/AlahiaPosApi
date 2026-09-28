using AlahiaPosApi.Auth;
using Xunit;

namespace AlahiaPosApi.Pase.Tests
{
    public class TenantSucursalGuardTests
    {
        [Fact]
        public void Cero_o_nulo_se_ajusta_no_se_bloquea()
        {
            var r = TenantSucursalGuard.CheckId(0, 39);
            Assert.False(r.Forbidden);
            Assert.True(r.Ajustes > 0);
        }

        [Fact]
        public void Misma_sucursal_ok()
        {
            var r = TenantSucursalGuard.CheckId(39, 39);
            Assert.False(r.Forbidden);
        }

        [Fact]
        public void Otra_sucursal_en_el_body_es_forbidden()
        {
            var r = TenantSucursalGuard.CheckId(26, 39);
            Assert.True(r.Forbidden);
        }

        [Fact]
        public void Sesion_sin_sucursal_no_bloquea()
        {
            var r = TenantSucursalGuard.CheckId(26, 0);
            Assert.False(r.Forbidden);
        }
    }
}
