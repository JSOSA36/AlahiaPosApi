using Xunit;

namespace AlahiaPosApi.Pase.Tests
{
    public class ExtractoBancoContratoTests
    {
        [Fact]
        public void Movimiento_de_extracto_no_valida_fondos()
        {
            var src = PaseRepo.ReadDataAccessFile(Path.Combine("Servicios", "MovimientoFinancieroService.cs"));
            Assert.Contains("EXTRACTO", src);
            Assert.Contains("no se valida saldo", src);
            Assert.Contains("esRegistroDesdeExtracto", src);
        }
    }
}
