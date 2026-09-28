using Xunit;

namespace AlahiaPosApi.Pase.Tests
{
    public class FrontendVivoContratoTests
    {
        [Fact]
        public void AuthSesion_sigue_apagada_en_el_ERP()
        {
            var path = PaseRepo.FindFrontendAppConfig();
            if (path == null)
                return;

            var src = File.ReadAllText(path);
            Assert.Contains("authSesionHabilitada: boolean = false", src);
            var activas = src.Split('\n')
                .Select(l => l.Trim())
                .Where(l => l.Length > 0 && !l.StartsWith("//") && !l.StartsWith("*"));
            Assert.DoesNotContain(activas, l => l.Contains("alahiabeautyapiprod", StringComparison.OrdinalIgnoreCase));
        }
    }
}
