using Xunit;

namespace AlahiaPosApi.Pase.Tests
{
    public class NominaContabilidadContratoTests
    {
        [Fact]
        public void Nomina_y_contabilidad_tienen_API()
        {
            Assert.True(File.Exists(Path.Combine(PaseRepo.FindRepoRoot(), "AlahiaPosApi", "Controllers", "RrhhLaboralController.cs")));
            Assert.True(File.Exists(Path.Combine(PaseRepo.FindRepoRoot(), "AlahiaPosApi", "Controllers", "AsientoContableController.cs")));
            Assert.True(File.Exists(Path.Combine(PaseRepo.FindRepoRoot(), "AlahiaPosApi", "Controllers", "TesoreriaConciliacionController.cs")));
        }

        [Fact]
        public void Produccion_y_gastos_tienen_API()
        {
            Assert.True(File.Exists(Path.Combine(PaseRepo.FindRepoRoot(), "AlahiaPosApi", "Controllers", "ProduccionController.cs")));
            Assert.True(File.Exists(Path.Combine(PaseRepo.FindRepoRoot(), "AlahiaPosApi", "Controllers", "GastosController.cs")));
            Assert.True(File.Exists(Path.Combine(PaseRepo.FindRepoRoot(), "AlahiaPosApi", "Controllers", "IngresosController.cs")));
        }
    }
}
