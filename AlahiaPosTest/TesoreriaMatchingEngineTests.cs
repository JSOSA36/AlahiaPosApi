using AlahiaPos.DataAccess.Servicios;
using AlahiaPos.Entities.Dto;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AlahiaPosTest
{
    [TestClass]
    public class TesoreriaMatchingEngineTests
    {
        private static TesoreriaExtractoLinea LineaDebito(DateTime fecha, decimal monto, string? referencia = null, string? desc = null)
            => new()
            {
                FechaMovimiento = fecha,
                Debito = monto,
                Credito = 0,
                Referencia = referencia,
                Descripcion = desc
            };

        private static MovimientoFinanciero MovSalida(DateTime fecha, decimal monto, int idCuenta, string? comprobante = null, string? motivo = null)
            => new()
            {
                FechaMovimiento = fecha,
                Monto = monto,
                TipoMovimiento = "SALIDA",
                IdCuentaOrigen = idCuenta,
                NumeroComprobante = comprobante,
                Motivo = motivo
            };

        [TestMethod]
        public void Score_FechaExactaYReferencia_EsAlto()
        {
            var fecha = new DateTime(2026, 7, 1);
            var linea = LineaDebito(fecha, 100m, "CHK-100", "Comisión");
            var mov = MovSalida(fecha, 100m, 23, "CHK-100", "Comisión");

            var score = TesoreriaMatchingEngine.CalcularScore(linea, mov);

            Assert.IsTrue(score >= 0.95m, $"Score esperado alto, obtuvo {score}");
        }

        [TestMethod]
        public void Score_SoloMontoFechaLejana_EsBajo()
        {
            var linea = LineaDebito(new DateTime(2026, 7, 1), 100m);
            var mov = MovSalida(new DateTime(2026, 7, 10), 100m, 23);

            var score = TesoreriaMatchingEngine.CalcularScore(linea, mov);

            Assert.IsTrue(score < 0.95m);
            Assert.IsTrue(score >= TesoreriaMatchingEngine.ScoreMinimoCandidato);
        }

        [TestMethod]
        public void Decidir_UnicoAlto_SinEvidencia_Sugerir()
        {
            var d = TesoreriaMatchingEngine.Decidir(0.95m, null, 1, evidenciaIdentificadora: false);
            Assert.AreEqual(MatchDecision.Sugerir, d);
        }

        [TestMethod]
        public void Decidir_UnicoAlto_ConEvidencia_AutoConciliar()
        {
            var d = TesoreriaMatchingEngine.Decidir(0.95m, null, 1, evidenciaIdentificadora: true);
            Assert.AreEqual(MatchDecision.AutoConciliar, d);
        }

        [TestMethod]
        public void Decidir_DosCercanos_Ambiguo()
        {
            var d = TesoreriaMatchingEngine.Decidir(0.90m, 0.88m, 2);
            Assert.AreEqual(MatchDecision.Ambiguo, d);
        }

        [TestMethod]
        public void Decidir_SegundoBajo_AutoSiScoreAlto()
        {
            var d = TesoreriaMatchingEngine.Decidir(0.96m, 0.70m, 2, evidenciaIdentificadora: true);
            Assert.AreEqual(MatchDecision.AutoConciliar, d);
        }

        [TestMethod]
        public void Decidir_ScoreMedio_Sugerir()
        {
            var d = TesoreriaMatchingEngine.Decidir(0.80m, null, 1);
            Assert.AreEqual(MatchDecision.Sugerir, d);
        }

        [TestMethod]
        public void Direccion_DebitoBanco_CoincideConSalida()
        {
            var linea = LineaDebito(DateTime.Today, 50m);
            var mov = MovSalida(DateTime.Today, 50m, 10);
            Assert.IsTrue(TesoreriaMatchingEngine.MismaDireccion(linea, mov, 10));
            Assert.IsFalse(TesoreriaMatchingEngine.MismaDireccion(linea, mov, 99));
        }

        [TestMethod]
        public void Direccion_CreditoBanco_CoincideConEntrada()
        {
            var linea = new TesoreriaExtractoLinea
            {
                FechaMovimiento = DateTime.Today,
                Debito = 0,
                Credito = 75m
            };
            var mov = new MovimientoFinanciero
            {
                TipoMovimiento = "ENTRADA",
                IdCuentaDestino = 10,
                Monto = 75m,
                FechaMovimiento = DateTime.Today
            };
            Assert.IsTrue(TesoreriaMatchingEngine.MismaDireccion(linea, mov, 10));
        }

        [TestMethod]
        public void Cruzado_MismaDireccionSinCuenta_CreditoConEntrada()
        {
            var linea = new TesoreriaExtractoLinea
            {
                FechaMovimiento = DateTime.Today,
                Debito = 0,
                Credito = 10000m
            };
            var movCaja = new MovimientoFinanciero
            {
                TipoMovimiento = "ENTRADA",
                IdCuentaDestino = 5, // Caja, distinta al banco
                Monto = 10000m,
                FechaMovimiento = DateTime.Today
            };
            Assert.IsTrue(TesoreriaMatchingEngine.MismaDireccionSinCuenta(linea, movCaja));
            Assert.IsFalse(TesoreriaMatchingEngine.MismaDireccion(linea, movCaja, 23));
        }

        [TestMethod]
        public void Cruzado_Decidir_NuncaAutoConciliar()
        {
            var d = TesoreriaMatchingEngine.DecidirCruzado(0.99m, null, 1);
            Assert.AreEqual(MatchDecision.Sugerir, d);
            Assert.AreNotEqual(MatchDecision.AutoConciliar, d);
        }

        [TestMethod]
        public void Cruzado_Decidir_AmbiguoSiDosCercanos()
        {
            var d = TesoreriaMatchingEngine.DecidirCruzado(0.90m, 0.88m, 2);
            Assert.AreEqual(MatchDecision.Ambiguo, d);
        }

        [TestMethod]
        public void Cruzado_NivelConfianza_AltaMediaBaja()
        {
            Assert.AreEqual("ALTA", TesoreriaMatchingEngine.NivelConfianzaCruzado(0.96m, true, 1));
            Assert.AreEqual("MEDIA", TesoreriaMatchingEngine.NivelConfianzaCruzado(0.85m, false, 1));
            Assert.AreEqual("BAJA", TesoreriaMatchingEngine.NivelConfianzaCruzado(0.96m, true, 2));
        }
    }
}
