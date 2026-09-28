using AlahiaPosApi.Auth;
using Xunit;

namespace AlahiaPosApi.Pase.Tests
{
    /// <summary>
    /// Si alguien enciende el filtro de sesión en el ERP vivo, el POS deja de guardar órdenes.
    /// </summary>
    public class ErpVivoAuthTests
    {
        [Fact]
        public void Erp_vivo_no_exige_token_de_sesion()
        {
            Assert.False(ErpVivoAuth.RequerirSesion);
        }

        [Fact]
        public void Erp_vivo_no_exige_cupo_de_terminal_POS()
        {
            Assert.False(ErpVivoAuth.RequerirTerminalPos);
        }

        [Fact]
        public void Program_usa_las_constantes_de_ErpVivoAuth()
        {
            var program = PaseRepo.ReadApiFile("Program.cs");
            Assert.Contains("ErpVivoAuth.RequerirSesion", program);
            Assert.Contains("ErpVivoAuth.RequerirTerminalPos", program);
            Assert.DoesNotContain("const bool requerirSesion = true", program);
        }
    }
}
