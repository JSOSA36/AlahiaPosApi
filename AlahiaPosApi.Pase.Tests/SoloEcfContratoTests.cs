using AlahiaPos.DataAccess.Servicios.FacturacionElectronica;
using Xunit;

namespace AlahiaPosApi.Pase.Tests
{
    public class SoloEcfContratoTests
    {
        [Theory]
        [InlineData("E31", true)]
        [InlineData("E32", true)]
        [InlineData("e33", true)]
        [InlineData("B01", false)]
        [InlineData("B02", false)]
        [InlineData("B04", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void Pruebas_fiscales_solo_aceptan_eCF(string? tipo, bool ok)
        {
            Assert.Equal(ok, EsEcf(tipo));
        }

        [Fact]
        public void Parser_de_compra_prohibe_NCF_tradicional()
        {
            var src = PaseRepo.ReadDataAccessFile(Path.Combine("Servicios", "FacturaCompraImagenService.cs"));
            Assert.Contains("nunca B01/B02", src);
            Assert.Contains("E31", src);
        }

        [Fact]
        public void Certecf_secuencia_ya_utilizada_es_aceptado_no_reenvio()
        {
            Assert.True(EcfSecuenciaYaUtilizada.EsTexto("Este número de secuencia ya ha sido utilizado."));
            Assert.False(EcfSecuenciaYaUtilizada.EsTexto("TablaSubcantidad de la línea 1 no es válido"));
            var src = PaseRepo.ReadDataAccessFile(Path.Combine("Servicios", "FacturacionElectronica", "CertecfCertificacionService.cs"));
            Assert.Contains("EcfSecuenciaYaUtilizada.EnResultado", src);
            Assert.Contains("MensajeAceptadoPorConsumo", src);
            Assert.Contains("RestaurarIscYSubcantidadDesdeCeldas", src);
        }

        private static bool EsEcf(string? tipo)
        {
            var t = (tipo ?? "").Trim().ToUpperInvariant();
            return t.Length >= 3 && t[0] == 'E' && char.IsDigit(t[1]) && char.IsDigit(t[2]);
        }
    }
}
