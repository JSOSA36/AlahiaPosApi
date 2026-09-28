using Xunit;

namespace AlahiaPosApi.Pase.Tests
{
    public class InventarioSucursalContratoTests
    {
        [Fact]
        public void Movimientos_exigen_acceso_a_sucursal()
        {
            var src = PaseRepo.ReadDataAccessFile(Path.Combine("Servicios", "MovimientosInventarioServices.cs"));
            Assert.Contains("TieneAccesoAsync", src);
        }

        [Fact]
        public void Almacenes_y_existencias_validan_sucursal()
        {
            var almacenes = PaseRepo.ReadApiFile(Path.Combine("Controllers", "AlmacenesController.cs"));
            var exist = PaseRepo.ReadApiFile(Path.Combine("Controllers", "AlmacenExistenciaController.cs"));
            Assert.Contains("TieneAccesoAsync", almacenes);
            Assert.Contains("TieneAccesoAsync", exist);
        }
    }
}
