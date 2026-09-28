using Xunit;

namespace AlahiaPosApi.Pase.Tests
{
    public class CatalogoCoberturaTests
    {
        [Fact]
        public void Todas_las_clases_de_modulo_existen()
        {
            var assembly = typeof(PaseCobertura).Assembly;
            var names = assembly.GetTypes()
                .Where(t => t.IsClass && t.IsPublic)
                .Select(t => t.Name)
                .ToHashSet(StringComparer.Ordinal);

            var faltan = PaseCobertura.ClasesObligatorias.Where(n => !names.Contains(n)).ToList();
            Assert.True(faltan.Count == 0, "Faltan tests de modulo: " + string.Join(", ", faltan));
        }
    }
}
