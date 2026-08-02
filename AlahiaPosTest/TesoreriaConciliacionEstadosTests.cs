using AlahiaPos.DataAccess.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AlahiaPosTest
{
    [TestClass]
    public class TesoreriaConciliacionEstadosTests
    {
        [TestMethod]
        public void Pendientes_IncluyeDuplicadoYDiferencia()
        {
            Assert.IsTrue(TesoreriaConciliacionEstados.EsPendienteBanco("PENDIENTE"));
            Assert.IsTrue(TesoreriaConciliacionEstados.EsPendienteBanco("SUGERIDO"));
            Assert.IsTrue(TesoreriaConciliacionEstados.EsPendienteBanco("AMBIGUO"));
            Assert.IsTrue(TesoreriaConciliacionEstados.EsPendienteBanco("DUPLICADO"));
            Assert.IsTrue(TesoreriaConciliacionEstados.EsPendienteBanco("DIFERENCIA"));
            Assert.IsFalse(TesoreriaConciliacionEstados.EsPendienteBanco("CONFIRMADO"));
            Assert.IsFalse(TesoreriaConciliacionEstados.EsPendienteBanco("IGNORADO"));
        }

        [TestMethod]
        public void Conciliadas_Y_Excluidas_SonDisjuntasDePendientes()
        {
            Assert.IsTrue(TesoreriaConciliacionEstados.EsConciliadaBanco("AUTO_CONCILIADO"));
            Assert.IsTrue(TesoreriaConciliacionEstados.EsConciliadaBanco("NUEVO_MOV"));
            Assert.IsTrue(TesoreriaConciliacionEstados.EsExcluidaBanco("DESCARTADO"));
            Assert.IsTrue(TesoreriaConciliacionEstados.EsResueltaBanco("IGNORADO"));
            Assert.IsFalse(TesoreriaConciliacionEstados.EsConciliadaBanco("IGNORADO"));
        }

        [TestMethod]
        public void SeleccionableMasivo_CoincideConPendiente()
        {
            foreach (var estado in new[] { "PENDIENTE", "SUGERIDO", "AMBIGUO", "DUPLICADO", "DIFERENCIA" })
            {
                Assert.AreEqual(
                    TesoreriaConciliacionEstados.EsPendienteBanco(estado),
                    TesoreriaConciliacionEstados.EsSeleccionableMasivo(estado));
            }
        }
    }
}
