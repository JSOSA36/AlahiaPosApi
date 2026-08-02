using AlahiaPos.DataAccess.Servicios.Cotizador;
using AlahiaPos.Entities.Domain.Cotizador;
using AlahiaPos.Entities.Dto.Cotizador;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AlahiaPosTest
{
    [TestClass]
    public class CotizadorMotorTests
    {
        private static Dictionary<int, string> Mapa() => new()
        {
            [1] = "CONCILIACION_BANCARIA",
            [2] = "CUENTAS_FINANCIERAS",
            [3] = "CONTABILIDAD",
            [4] = "CENTRO_PRODUCCION",
            [5] = "ALMACENES",
            [6] = "EMPLEADOS_COMISION",
            [7] = "EMPLEADOS",
            [8] = "PRODUCTOS"
        };

        private static List<ModuloDependencia> DependenciasBase() => new()
        {
            new() { ModuloId = 1, ModuloRequeridoId = 2, Tipo = "REQUIERE", Activo = true,
                Mensaje = "Hemos recomendado Banco porque seleccionó Conciliación Bancaria." },
            new() { ModuloId = 2, ModuloRequeridoId = 3, Tipo = "RECOMIENDA", Activo = true,
                Mensaje = "Seleccionó Banco. Recomendamos Contabilidad." },
            new() { ModuloId = 4, ModuloRequeridoId = 5, Tipo = "RECOMIENDA", Activo = true,
                Mensaje = "Si seleccionó Producción recomendamos Inventario/Almacenes." },
            new() { ModuloId = 4, ModuloRequeridoId = 8, Tipo = "REQUIERE", Activo = true },
            new() { ModuloId = 6, ModuloRequeridoId = 7, Tipo = "REQUIERE", Activo = true,
                Mensaje = "Comisiones requiere Recursos Humanos (Empleados)." }
        };

        [TestMethod]
        public void Dependencias_ConciliacionSinBanco_RequiereCuentasFinancieras()
        {
            var matches = DependenciaValidator.Evaluar(
                new[] { "CONCILIACION_BANCARIA" },
                DependenciasBase(),
                Mapa());

            Assert.IsTrue(matches.Any(m =>
                m.CodigoRequerido == "CUENTAS_FINANCIERAS" && m.Tipo == "REQUIERE"));
        }

        [TestMethod]
        public void Dependencias_BancoSinContabilidad_RecomiendaContabilidad()
        {
            var matches = DependenciaValidator.Evaluar(
                new[] { "CUENTAS_FINANCIERAS" },
                DependenciasBase(),
                Mapa());

            Assert.IsTrue(matches.Any(m =>
                m.CodigoRequerido == "CONTABILIDAD" && m.Tipo == "RECOMIENDA"));
        }

        [TestMethod]
        public void Dependencias_ProduccionSinInventario_RecomiendaAlmacenes()
        {
            var matches = DependenciaValidator.Evaluar(
                new[] { "CENTRO_PRODUCCION" },
                DependenciasBase(),
                Mapa());

            Assert.IsTrue(matches.Any(m => m.CodigoRequerido == "ALMACENES"));
            Assert.IsTrue(matches.Any(m => m.CodigoRequerido == "PRODUCTOS" && m.Tipo == "REQUIERE"));
        }

        [TestMethod]
        public void Dependencias_NominaComisiones_RequiereEmpleadosRRHH()
        {
            var matches = DependenciaValidator.Evaluar(
                new[] { "EMPLEADOS_COMISION" },
                DependenciasBase(),
                Mapa());

            Assert.IsTrue(matches.Any(m =>
                m.CodigoRequerido == "EMPLEADOS" && m.Tipo == "REQUIERE"));
        }

        [TestMethod]
        public void Precio_ConfiguracionMinima_AplicaPiso30()
        {
            var result = PrecioCalculator.Calcular(new PrecioCalculoInput
            {
                Modulos = new List<(string, string, decimal, bool)>
                {
                    ("DASHBOARD", "Dashboard", 0m, false)
                },
                Usuarios = 1,
                Sucursales = 1,
                UsuariosIncluidos = 1,
                SucursalesIncluidas = 1,
                PisoMensualUSD = 30m
            });

            Assert.AreEqual(30m, result.PrecioMensualUSD);
            Assert.IsTrue(result.AplicoPisoMinimo);
            Assert.IsTrue(result.Desglose.Any(d => d.TipoLinea == "AJUSTE_PISO"));
        }

        [TestMethod]
        public void Precio_TramoEcf_SumaCargoPorVolumen()
        {
            var tramos = new List<CotizadorTramoDocumento>
            {
                new() { DesdeDocs = 0, HastaDocs = 100, CargoUSD = 5m, Activo = true, Orden = 1 },
                new() { DesdeDocs = 101, HastaDocs = 500, CargoUSD = 15m, Activo = true, Orden = 2 }
            };

            var result = PrecioCalculator.Calcular(new PrecioCalculoInput
            {
                Modulos = new List<(string, string, decimal, bool)>
                {
                    ("POS", "POS", 20m, true)
                },
                Usuarios = 1,
                Sucursales = 1,
                UsuariosIncluidos = 1,
                SucursalesIncluidas = 1,
                UsaFacturacionElectronica = true,
                DocumentosElectronicosMensuales = 250,
                PisoMensualUSD = 30m,
                Tramos = tramos
            });

            Assert.IsTrue(result.Desglose.Any(d => d.TipoLinea == "ECF" && d.MontoUSD == 15m));
            Assert.AreEqual(35m, result.PrecioMensualUSD);
            Assert.IsFalse(result.AplicoPisoMinimo);
        }

        [TestMethod]
        public void Explicaciones_IncluyenMensajesDeDependenciaYEcf()
        {
            var matches = DependenciaValidator.Evaluar(
                new[] { "CONCILIACION_BANCARIA" },
                DependenciasBase(),
                Mapa());

            var precio = PrecioCalculator.Calcular(new PrecioCalculoInput
            {
                Modulos = Array.Empty<(string, string, decimal, bool)>(),
                PisoMensualUSD = 30m
            });

            var textos = ExplicacionBuilder.Construir(
                new CotizadorCalcularRequest
                {
                    UsaFacturacionElectronica = true,
                    DocumentosElectronicosMensuales = 80
                },
                matches,
                precio,
                new Dictionary<string, string>
                {
                    ["CONCILIACION_BANCARIA"] = "Conciliación Bancaria",
                    ["CUENTAS_FINANCIERAS"] = "Cuentas Financieras"
                });

            Assert.IsTrue(textos.Any(t => t.Contains("Banco", StringComparison.OrdinalIgnoreCase)
                || t.Contains("Cuentas Financieras", StringComparison.OrdinalIgnoreCase)));
            Assert.IsTrue(textos.Any(t => t.Contains("e-CF", StringComparison.OrdinalIgnoreCase)));
            Assert.IsTrue(textos.Any(t => t.Contains("30", StringComparison.OrdinalIgnoreCase)));
        }
    }
}
