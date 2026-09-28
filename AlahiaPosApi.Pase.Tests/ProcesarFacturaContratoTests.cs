using AlahiaPos.DataAccess.Servicios.Ventas;
using Xunit;

namespace AlahiaPosApi.Pase.Tests
{
    public class ProcesarFacturaContratoTests
    {
        [Fact]
        public void Claves_de_tesoreria_e_inventario_no_chocan()
        {
            Assert.Equal("VENTA-10-Efectivo", ProcesarFacturaIntegridad.ClaveTesoreria(10, "Efectivo"));
            Assert.Equal("Factura #10", ProcesarFacturaIntegridad.ReferenciaInventario(10));
            Assert.NotEqual(
                ProcesarFacturaIntegridad.ClaveTesoreria(10, "Efectivo"),
                ProcesarFacturaIntegridad.ReferenciaInventario(10));
        }

        [Fact]
        public void Idempotencia_vacia_no_se_usa_como_clave()
        {
            Assert.Equal("", ProcesarFacturaIntegridad.NormalizarClaveIdempotencia(null));
            Assert.Equal("", ProcesarFacturaIntegridad.NormalizarClaveIdempotencia("  "));
            Assert.Equal("k-1", ProcesarFacturaIntegridad.NormalizarClaveIdempotencia(" k-1 "));
        }

        [Fact]
        public void ProcesarFactura_usa_resolver_de_usuario()
        {
            var src = PaseRepo.ReadApiFile(Path.Combine("Controllers", "FacturaHeaderController.cs"));
            Assert.Contains("ProcesarFactura", src);
        Assert.Contains("IdUsuarioCobroResolver.ResolverAsync", src);
        }

        [Fact]
        public void ProcesarFactura_aplica_cobertura_ars_sin_tesoreria()
        {
            var src = PaseRepo.ReadApiFile(Path.Combine("Controllers", "FacturaHeaderController.cs"));
            Assert.Contains("AplicarCoberturaEnVentaAsync", src);
            Assert.Contains("FormaPagoArs.EsArs", src);
            Assert.Contains("PendienteArs", src);
        }
    }
}
