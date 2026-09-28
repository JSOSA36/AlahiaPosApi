using Xunit;

namespace AlahiaPosApi.Pase.Tests
{
    public class ClientesComprasCitasContratoTests
    {
        [Fact]
        public void Clientes_compras_y_citas_siguen_en_el_API()
        {
            var clientes = PaseRepo.ReadApiFile(Path.Combine("Controllers", "ClientesController.cs"));
            var compras = PaseRepo.ReadApiFile(Path.Combine("Controllers", "ComprasController.cs"));
            var citas = PaseRepo.ReadApiFile(Path.Combine("Controllers", "CitasController.cs"));
            Assert.Contains("Clientes", clientes);
            Assert.Contains("Compras", compras);
            Assert.Contains("Citas", citas);
        }

        [Fact]
        public void CxC_y_notas_credito_existen()
        {
            var cxc = PaseRepo.ReadApiFile(Path.Combine("Controllers", "FacturaHeaderController.cs"));
            var nc = PaseRepo.ReadApiFile(Path.Combine("Controllers", "NotasCreditoController.cs"));
            Assert.Contains("GetCuentasPorCobrar", cxc);
            Assert.Contains("NotasCredito", nc);
        }
    }
}
