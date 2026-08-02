using AlahiaPos.DataAccess.Servicios.ExtractosBancarios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AlahiaPosTest
{
    [TestClass]
    public class PopularTextoStatementAdapterTests
    {
        /// <summary>
        /// Fixture textual anonimizado (sin PII real) que reproduce la forma
        /// del estado de cuenta Popular / PDF-extraído.
        /// Totales y saldos alineados con Scripts/Test-TesoreriaExtractoPopular.ps1.
        /// </summary>
        private static string FixturePopularAnonimizado() => @"
ESTADO DE CUENTA
Banco: Banco Popular Dominicano
Empresa: EMPRESA DEMO SRL
Cuenta: 1234567890
Moneda: DOP (RD$)
Período: 01/06/2026 - 30/06/2026
Saldo Inicial: 150,000.00
Total Créditos: 179,675.00
Total Débitos: 120,950.00
Saldo Final: 208,725.00
Fecha Referencia Descripción Débito Crédito Balance
01/06/2026 SALDO Saldo inicial 150,000.00
02/06/2026 DEP-1001 Deposito en ventanilla 25,000.00 175,000.00
03/06/2026 TRF-2001 Transferencia a proveedor 18,500.00 156,500.00
04/06/2026 DEP-1002 Deposito ACH cliente 40,000.00 196,500.00
05/06/2026 COM-001 Comision transferencia 150.00 196,350.00
06/06/2026 CH-3001 Cheque pagado 12,000.00 184,350.00
07/06/2026 DEP-1003 Deposito cheque tercero 15,500.00 199,850.00
08/06/2026 TRF-2002 Pago nomina 35,000.00 164,850.00
09/06/2026 COM-002 Comision ACH 75.00 164,775.00
10/06/2026 DEP-1004 Transferencia recibida 50,000.00 214,775.00
11/06/2026 TRF-2003 Pago servicios 8,200.00 206,575.00
12/06/2026 COM-003 Cargo mensual cuenta 125.00 206,450.00
13/06/2026 DEP-1005 Deposito efectivo 20,175.00 226,625.00
14/06/2026 TRF-2004 Pago proveedor local 9,800.00 216,825.00
15/06/2026 DEP-1006 Credito intereses 1,000.00 217,825.00
16/06/2026 TRF-2005 Debito automatico seguro 5,000.00 212,825.00
17/06/2026 COM-004 Impuesto bancario 100.00 212,725.00
18/06/2026 DEP-1007 Deposito final mes 28,000.00 240,725.00
19/06/2026 TRF-2006 Transferencia salida 32,000.00 208,725.00
";

        [TestMethod]
        public void CanParse_DetectaEstadoCuentaPopular()
        {
            var adapter = new PopularTextoStatementAdapter();
            var ok = adapter.CanParse(new BankStatementParseRequest
            {
                Formato = "CSV",
                ContenidoCsv = FixturePopularAnonimizado()
            });
            Assert.IsTrue(ok);
        }

        [TestMethod]
        public void Parse_FixtureAnonimizado_TotalesYLineasGolden()
        {
            var adapter = new PopularTextoStatementAdapter();
            var result = adapter.Parse(new BankStatementParseRequest
            {
                Formato = "CSV",
                ContenidoCsv = FixturePopularAnonimizado()
            });

            Assert.AreEqual("Banco Popular Dominicano", result.Banco);
            Assert.AreEqual("1234567890", result.NumeroCuentaBanco);
            Assert.AreEqual("DOP", result.Moneda);
            Assert.AreEqual(150000.00m, result.SaldoInicial);
            Assert.AreEqual(208725.00m, result.SaldoFinal);
            Assert.AreEqual(179675.00m, result.TotalCreditos);
            Assert.AreEqual(120950.00m, result.TotalDebitos);
            Assert.AreEqual(18, result.Lineas.Count, $"Esperaba 18 movimientos, obtuvo {result.Lineas.Count}");

            var deposito = result.Lineas.Single(x => x.Referencia == "DEP-1001");
            Assert.AreEqual(0m, deposito.Debito);
            Assert.AreEqual(25000.00m, deposito.Credito);

            var transferencia = result.Lineas.Single(x => x.Referencia == "TRF-2001");
            Assert.AreEqual(18500.00m, transferencia.Debito);
            Assert.AreEqual(0m, transferencia.Credito);

            var com003 = result.Lineas.Single(x => x.Referencia == "COM-003");
            Assert.AreEqual(125.00m, com003.Debito);
            Assert.AreEqual(0m, com003.Credito);
        }

        [TestMethod]
        public void Parse_OrphanDescription_SeUneAFilaCorrecta()
        {
            // Descripción huérfana entre filas fechadas (patrón PDF Popular).
            var texto = @"
Banco: Banco Popular Dominicano
Cuenta: 9999
Moneda: DOP
Saldo Inicial: 1000.00
01/06/2026 DEP-10 500.00 1500.00
Descripcion huerfana para siguiente
02/06/2026 TRF-10 200.00 1300.00
";
            var adapter = new PopularTextoStatementAdapter();
            var result = adapter.Parse(new BankStatementParseRequest
            {
                Formato = "CSV",
                ContenidoCsv = texto
            });
            Assert.AreEqual(2, result.Lineas.Count);
            var trf = result.Lineas.Single(x => x.Referencia == "TRF-10");
            StringAssert.Contains(trf.Descripcion ?? string.Empty, "Descripcion huerfana");
        }

        [TestMethod]
        public void Orchestrator_SeleccionaPopularSobreCsv()
        {
            var orch = BankStatementParserOrchestrator.CreateDefault();
            var result = orch.Parse(new BankStatementParseRequest
            {
                Formato = "CSV",
                ContenidoCsv = FixturePopularAnonimizado()
            });

            Assert.AreEqual(PopularTextoStatementAdapter.NombreAdapter, result.AdapterUsado);
            Assert.AreEqual(18, result.Lineas.Count);
        }
    }

    [TestClass]
    public class CsvStatementAdapterTests
    {
        [TestMethod]
        public void Parse_CsvConEncabezado_LeeDebitoCredito()
        {
            var csv = @"Fecha,Referencia,Descripcion,Debito,Credito,Balance
01/06/2026,REF-1,Pago proveedor,100.00,0,900.00
02/06/2026,REF-2,Deposito cliente,0,250.00,1150.00
";
            var adapter = new CsvStatementAdapter();
            var result = adapter.Parse(new BankStatementParseRequest
            {
                Formato = "CSV",
                ContenidoCsv = csv
            });
            Assert.AreEqual(2, result.Lineas.Count);
            Assert.AreEqual(100m, result.Lineas[0].Debito);
            Assert.AreEqual(250m, result.Lineas[1].Credito);
        }

        [TestMethod]
        public void Orchestrator_CsvGenerico_CuandoNoEsPopular()
        {
            var orch = BankStatementParserOrchestrator.CreateDefault();
            var result = orch.Parse(new BankStatementParseRequest
            {
                Formato = "CSV",
                ContenidoCsv = "Fecha,Debito,Credito\n01/06/2026,10,0\n"
            });
            Assert.AreEqual(CsvStatementAdapter.NombreAdapter, result.AdapterUsado);
            Assert.AreEqual(1, result.Lineas.Count);
        }
    }
}
