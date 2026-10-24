using AlahiaPos.DataAccess.Servicios;
using Xunit;

namespace AlahiaPosApi.Pase.Tests
{
    public class PoliticaFiscalAutomaticaTests
    {
        [Fact]
        public void Politica_apagada_conserva_el_comprobante_y_no_rechaza_itbis()
        {
            var r = PoliticaFiscalAutomatica.Evaluar(new EntradaPoliticaFiscal
            {
                PoliticaActiva = false,
                EmpresaFacturaConItbis = true,
                FacturacionElectronica = true,
                TipoComprobante = "FACT",
                Pagos = new[] { Pago("Tarjeta", 100m) },
                Lineas = new[] { Gravada(0m) }
            });

            Assert.False(r.Rechazar);
            Assert.False(r.ForzarConsumo);
            Assert.Equal("FACT", r.TipoComprobante);
            Assert.Equal("Ninguna", r.Regla);
            Assert.False(r.ConsumioSecuencia);
        }

        [Fact]
        public void Tarjeta_con_politica_asigna_consumo_e32()
        {
            var r = EvaluarActiva("Tarjeta", "FACT", null, null);
            Assert.True(r.ForzarConsumo);
            Assert.Equal(PoliticaFiscalAutomatica.TipoConsumo, r.TipoComprobante);
            Assert.Equal(32, r.TipoEcfDgii);
            Assert.Equal("Tarjeta", r.Regla);
            Assert.Equal("Tarjeta", r.MetodoPago);
            Assert.False(r.ConsumioSecuencia);
        }

        [Fact]
        public void Billet_Bhd_asigna_consumo_y_no_confunde_transferencia_bhd()
        {
            var billetera = EvaluarActiva("Billet BHD", "FACT", null, null);
            Assert.True(billetera.ForzarConsumo);
            Assert.Equal("BilleteraBhd", billetera.Regla);
            Assert.Equal(32, billetera.TipoEcfDgii);

            var banco = EvaluarActiva("Transferencia BHD", "FACT", null, null);
            Assert.False(banco.ForzarConsumo);
            Assert.Equal("Ninguna", banco.Regla);

            var soloBhd = EvaluarActiva("BHD", "FACT", null, null);
            Assert.False(soloBhd.ForzarConsumo);
        }

        [Fact]
        public void Rechaza_apagar_itbis_en_toda_la_venta_sin_consumir_secuencia()
        {
            var r = PoliticaFiscalAutomatica.Evaluar(new EntradaPoliticaFiscal
            {
                PoliticaActiva = true,
                EmpresaFacturaConItbis = true,
                FacturacionElectronica = true,
                TipoComprobante = "Consumidor Final",
                TipoEcfDgii = 32,
                Pagos = new[] { Pago("Azul", 50m) },
                Lineas = new[] { Gravada(0m), Gravada(0m) }
            });

            Assert.True(r.Rechazar);
            Assert.False(r.ForzarConsumo);
            Assert.Equal("ItbisBloqueado", r.Regla);
            Assert.False(r.ConsumioSecuencia);
            Assert.False(string.IsNullOrWhiteSpace(r.Mensaje));
        }

        [Fact]
        public void Pos_puede_apagar_itbis_y_la_tarjeta_sigue_en_consumo()
        {
            var r = PoliticaFiscalAutomatica.Evaluar(new EntradaPoliticaFiscal
            {
                PoliticaActiva = true,
                PermitirApagarItbisPos = true,
                EmpresaFacturaConItbis = true,
                FacturacionElectronica = true,
                TipoComprobante = "Consumidor Final",
                TipoEcfDgii = 32,
                Pagos = new[] { Pago("Tarjeta", 50m) },
                Lineas = new[] { Gravada(0m) }
            });

            Assert.False(r.Rechazar);
            Assert.True(r.ForzarConsumo);
            Assert.Equal(32, r.TipoEcfDgii);
            Assert.Equal("Tarjeta", r.Regla);
        }

        [Fact]
        public void Producto_exento_no_se_trata_como_apagado_global()
        {
            var r = PoliticaFiscalAutomatica.Evaluar(new EntradaPoliticaFiscal
            {
                PoliticaActiva = true,
                EmpresaFacturaConItbis = true,
                FacturacionElectronica = true,
                TipoComprobante = "FACT",
                Pagos = new[] { Pago("Visa", 80m) },
                Lineas = new[]
                {
                    Gravada(18m),
                    new LineaPoliticaFiscal { GravaItbis = false, Itbis = 0m }
                }
            });

            Assert.False(r.Rechazar);
            Assert.True(r.ForzarConsumo);
            Assert.Equal(32, r.TipoEcfDgii);
        }

        [Fact]
        public void Api_no_evade_consumo_si_pide_otro_tipo_sin_rnc()
        {
            var r = EvaluarActiva("Cardnet", "Crédito Fiscal", 31, "000000000");
            Assert.True(r.ForzarConsumo);
            Assert.Equal(32, r.TipoEcfDgii);
        }

        [Fact]
        public void Respeta_credito_fiscal_con_rnc_y_gubernamental()
        {
            var credito = EvaluarActiva("Tarjeta", "Crédito Fiscal", 31, "131793916");
            Assert.False(credito.ForzarConsumo);
            Assert.Equal("ExcepcionAutorizada", credito.Regla);
            Assert.Equal(31, credito.TipoEcfDgii);

            var gubernamental = EvaluarActiva("Billet BHD", "Gubernamental", 45, null);
            Assert.False(gubernamental.ForzarConsumo);
            Assert.Equal("ExcepcionAutorizada", gubernamental.Regla);
        }

        [Fact]
        public void Sucursal_pisa_a_la_empresa_y_otra_empresa_no_se_mezcla()
        {
            var empresa1 = new[]
            {
                new ParametroFiscalValor(null, "false"),
                new ParametroFiscalValor(4, "true")
            };
            var empresa2 = new[]
            {
                new ParametroFiscalValor(null, "false")
            };

            Assert.True(PoliticaFiscalAutomatica.ValorActivo(
                PoliticaFiscalAutomatica.ResolverValor(empresa1, 4)));
            Assert.False(PoliticaFiscalAutomatica.ValorActivo(
                PoliticaFiscalAutomatica.ResolverValor(empresa1, 9)));
            Assert.False(PoliticaFiscalAutomatica.ValorActivo(
                PoliticaFiscalAutomatica.ResolverValor(empresa2, 4)));
            Assert.Null(PoliticaFiscalAutomatica.ResolverValor(null, 1));
        }

        [Fact]
        public void No_bloquea_itbis_que_ya_estaba_apagado_ni_a_quien_no_activa_la_politica()
        {
            Assert.False(PoliticaFiscalAutomatica.BloqueaApagadoItbisGlobal(
                true, false, "FACTURAR_CON_ITBIS", "false"));
            Assert.False(PoliticaFiscalAutomatica.BloqueaApagadoItbisGlobal(
                false, true, "FACTURAR_CON_ITBIS", "false"));
            Assert.True(PoliticaFiscalAutomatica.BloqueaApagadoItbisGlobal(
                true, true, "FACTURAR_CON_ITBIS", "false"));
            Assert.False(PoliticaFiscalAutomatica.BloqueaApagadoItbisGlobal(
                true, true, "FACTURAR_CON_ITBIS", "true"));
        }

        [Fact]
        public void Sin_facturacion_electronica_no_cambia_el_tipo()
        {
            var r = PoliticaFiscalAutomatica.Evaluar(new EntradaPoliticaFiscal
            {
                PoliticaActiva = true,
                EmpresaFacturaConItbis = true,
                FacturacionElectronica = false,
                TipoComprobante = "FACT",
                Pagos = new[] { Pago("Tarjeta", 10m) },
                Lineas = new[] { Gravada(1.8m) }
            });

            Assert.False(r.ForzarConsumo);
            Assert.Equal("OmitidaSinFacturacionElectronica", r.Regla);
            Assert.Equal("FACT", r.TipoComprobante);
            Assert.False(r.ConsumioSecuencia);
        }

        [Fact]
        public void ProcesarFactura_decide_antes_de_consumir_ncf()
        {
            var src = PaseRepo.ReadApiFile(Path.Combine("Controllers", "FacturaHeaderController.cs"));
            var proc = src.IndexOf("public async Task<IActionResult> ProcesarFactura", StringComparison.Ordinal);
            var politica = src.IndexOf("AplicarPoliticaFiscalAsync", proc, StringComparison.Ordinal);
            var ncf = src.IndexOf("GenerarNCF", politica, StringComparison.Ordinal);
            Assert.True(proc > 0 && politica > proc && ncf > politica);
        }

        [Fact]
        public void Emision_ajusta_el_tipo_antes_de_reservar_secuencia()
        {
            var src = PaseRepo.ReadDataAccessFile(Path.Combine(
                "Servicios",
                "FacturacionElectronica",
                "FacturacionElectronicaService.cs"));
            var emitir = src.IndexOf("EmitirDocumentoAsync", StringComparison.Ordinal);
            var ajustar = src.IndexOf("AjustarTipoEcfPorPoliticaAsync", emitir, StringComparison.Ordinal);
            var reservar = src.IndexOf("ReservarSiguienteAsync", ajustar, StringComparison.Ordinal);
            Assert.True(emitir > 0 && ajustar > emitir && reservar > ajustar);
            Assert.Contains("IdempotencyKey = $\"ECF_{request.OrigenDocumento}_{request.IdOrigen}_{request.TipoEcfDgii}\"", src);
        }

        private static ResultadoPoliticaFiscal EvaluarActiva(
            string metodo,
            string tipo,
            int? tipoEcf,
            string? rnc)
        {
            return PoliticaFiscalAutomatica.Evaluar(new EntradaPoliticaFiscal
            {
                PoliticaActiva = true,
                EmpresaFacturaConItbis = true,
                FacturacionElectronica = true,
                TipoComprobante = tipo,
                TipoEcfDgii = tipoEcf,
                Rnc = rnc,
                Pagos = new[] { Pago(metodo, 100m) },
                Lineas = new[] { Gravada(18m) }
            });
        }

        private static PagoPoliticaFiscal Pago(string metodo, decimal monto)
            => new() { Metodo = metodo, Monto = monto };

        private static LineaPoliticaFiscal Gravada(decimal itbis)
            => new() { GravaItbis = true, Itbis = itbis };
    }
}
