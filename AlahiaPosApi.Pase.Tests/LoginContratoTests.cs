using Xunit;

namespace AlahiaPosApi.Pase.Tests
{
    public class LoginContratoTests
    {
        [Fact]
        public void Login_es_anonimo_y_exige_usuario_clave()
        {
            var src = PaseRepo.ReadApiFile(Path.Combine("Controllers", "LoginController.cs"));
            Assert.Contains("[AllowAnonymous]", src);
            Assert.Contains("HttpPost(\"login\")", src);
            Assert.Contains("Usuario y contraseña son obligatorios", src);
        }

        [Fact]
        public void Filtro_global_de_sesion_sigue_apagado()
        {
            Assert.False(AlahiaPosApi.Auth.ErpVivoAuth.RequerirSesion);
        }
    }
}
