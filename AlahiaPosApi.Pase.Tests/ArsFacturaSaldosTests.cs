using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using System.Linq;
using Xunit;

namespace AlahiaPosApi.Pase.Tests
{
    public class ArsFacturaSaldosTests
    {
        [Fact]
        public void Cien_por_ciento_paciente_no_crea_deuda_ars()
        {
            var header = new FacturaHeaders { Total = 5000m, Pagado = 5000m };
            FormaPagoArs.RecalcularSaldos(header);
            Assert.Equal(0, header.Pendiente);
            Assert.Equal(0, header.PendienteArs);
            Assert.Equal("Pagada", header.Estado);
            Assert.Equal(string.Empty, header.EstadoArs);
        }

        [Fact]
        public void Cien_por_ciento_ars_deja_cxc_en_la_aseguradora()
        {
            var header = new FacturaHeaders
            {
                Total = 5000m,
                Pagado = 0,
                MontoCubiertoArs = 5000m,
                IdArs = 7
            };
            FormaPagoArs.RecalcularSaldos(header);
            Assert.Equal(0, header.Pendiente);
            Assert.Equal("Pagada", header.Estado);
            Assert.Equal(5000m, header.PendienteArs);
            Assert.Equal(FormaPagoArs.EstadoPendiente, header.EstadoArs);
        }

        [Fact]
        public void Mixto_parte_ars_parte_paciente()
        {
            var header = new FacturaHeaders
            {
                Total = 5000m,
                Pagado = 1000m,
                MontoCubiertoArs = 4000m,
                IdArs = 7
            };
            FormaPagoArs.RecalcularSaldos(header);
            Assert.Equal(0, header.Pendiente);
            Assert.Equal("Pagada", header.Estado);
            Assert.Equal(4000m, header.PendienteArs);
            Assert.Equal(FormaPagoArs.EstadoPendiente, header.EstadoArs);
        }

        [Fact]
        public void Cobro_parcial_de_ars_pasa_a_parcial()
        {
            var header = new FacturaHeaders
            {
                Total = 5000m,
                Pagado = 1000m,
                MontoCubiertoArs = 4000m,
                PagadoArs = 1200m,
                IdArs = 7
            };
            FormaPagoArs.RecalcularSaldos(header);
            Assert.Equal(2800m, header.PendienteArs);
            Assert.Equal(FormaPagoArs.EstadoParcial, header.EstadoArs);
        }

        [Fact]
        public void Cobro_completo_de_ars_queda_pagada()
        {
            var header = new FacturaHeaders
            {
                Total = 5000m,
                Pagado = 1000m,
                MontoCubiertoArs = 4000m,
                PagadoArs = 4000m,
                IdArs = 7
            };
            FormaPagoArs.RecalcularSaldos(header);
            Assert.Equal(0, header.PendienteArs);
            Assert.Equal(FormaPagoArs.EstadoPagada, header.EstadoArs);
        }

        [Fact]
        public void Anulada_cierra_saldo_ars_y_cliente()
        {
            var header = new FacturaHeaders
            {
                Total = 5000m,
                Pagado = 1000m,
                MontoCubiertoArs = 4000m,
                EstaCancelada = true
            };
            FormaPagoArs.RecalcularSaldos(header);
            Assert.Equal(0, header.Pendiente);
            Assert.Equal(0, header.PendienteArs);
            Assert.Equal(FormaPagoArs.EstadoAnulada, header.Estado);
            Assert.Equal(FormaPagoArs.EstadoAnulada, header.EstadoArs);
        }

        [Fact]
        public void Detecta_metodo_ars()
        {
            Assert.True(FormaPagoArs.EsArs("ARS"));
            Assert.True(FormaPagoArs.EsArs("ars"));
            Assert.False(FormaPagoArs.EsArs("Efectivo"));
        }

        [Fact]
        public void Lote_cubre_todas_si_monto_es_cero_o_total()
        {
            var docs = new[] { (1, 6000m), (2, 8000m) };
            var partes = FormaPagoArs.DistribuirPagoLote(docs, 0);
            Assert.Equal(2, partes.Count);
            Assert.Equal(14000m, partes.Sum(p => p.Monto));
        }

        [Fact]
        public void Lote_aplica_fifo_cuando_el_monto_no_alcanza()
        {
            var docs = new[] { (1, 6000m), (2, 8000m), (3, 4000m) };
            var partes = FormaPagoArs.DistribuirPagoLote(docs, 10000m);
            Assert.Equal(2, partes.Count);
            Assert.Equal(1, partes[0].IdFactura);
            Assert.Equal(6000m, partes[0].Monto);
            Assert.Equal(2, partes[1].IdFactura);
            Assert.Equal(4000m, partes[1].Monto);
        }
    }
}
