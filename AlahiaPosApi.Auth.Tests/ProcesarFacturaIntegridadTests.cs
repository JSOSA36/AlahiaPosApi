using AlahiaPos.DataAccess.Servicios.Ventas;
using Xunit;

namespace AlahiaPosApi.Auth.Tests
{
    public class ProcesarFacturaIntegridadTests
    {
        [Fact]
        public void ClaveTesoreria_incluye_factura_y_metodo()
        {
            Assert.Equal("VENTA-9531-Efectivo", ProcesarFacturaIntegridad.ClaveTesoreria(9531, "Efectivo"));
        }

        [Fact]
        public void ReferenciaInventario_es_estable()
        {
            Assert.Equal("Factura #9531", ProcesarFacturaIntegridad.ReferenciaInventario(9531));
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("  ", "")]
        [InlineData(" abc-1 ", "abc-1")]
        public void NormalizarClaveIdempotencia(string? raw, string expected)
        {
            Assert.Equal(expected, ProcesarFacturaIntegridad.NormalizarClaveIdempotencia(raw));
        }

        [Fact]
        public async Task VentaYaConfirmada_id_invalido_es_false()
        {
            Assert.False(await ProcesarFacturaIntegridad.VentaYaConfirmadaAsync(null!, 0));
            Assert.False(await ProcesarFacturaIntegridad.VentaYaConfirmadaAsync(null!, 10));
        }
    }
}
