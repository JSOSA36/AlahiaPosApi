using AlahiaPosApi.Auth;
using Xunit;

namespace AlahiaPosApi.Auth.Tests
{
    public class SesionExclusivaTests
    {
        [Fact]
        public void Sesion_exclusiva_desactivada_nunca_bloquea()
        {
            Assert.False(SesionExclusiva.TieneSesionEnOtroDispositivo(null, "pc-1", "pc-2"));
            Assert.False(SesionExclusiva.TieneSesionEnOtroDispositivo("", "pc-1", "pc-2"));
            Assert.False(SesionExclusiva.TieneSesionEnOtroDispositivo(
                "token-vivo", "caja-frente", "caja-frente"));
            Assert.False(SesionExclusiva.TieneSesionEnOtroDispositivo(
                "token-vivo", "caja-frente", "caja-barra"));
            Assert.False(SesionExclusiva.TieneSesionEnOtroDispositivo("token-vivo", null, "pc-2"));
        }
    }
}
