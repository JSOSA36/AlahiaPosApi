using AlahiaPos.DataAccess.Servicios.FiscalGateway.PgEInvoicing;
using AlahiaPos.Entities.Dto.Fiscal;

namespace Alahia.eCF.Api.Services
{
    /// <summary>
    /// Traduce el JSON estilo PG.eInvoicing (mismo contrato que usa AlahiaPosApi hoy)
    /// al modelo fiscal interno para firmar/enviar a DGII.
    /// </summary>
    public static class PgReceiptMapper
    {
        public static FiscalDocumentoElectronico ToFiscal(PgDgiiDocumentDto pg)
        {
            var id = pg.Encabezado.IdDoc;
            var em = pg.Encabezado.Emisor;
            var co = pg.Encabezado.Comprador;
            var to = pg.Encabezado.Totales;

            var doc = new FiscalDocumentoElectronico
            {
                IdEmpresa = pg.IdEmpresa,
                AmbienteDgii = pg.AmbienteDgii,
                CeldasExcel = pg.CeldasExcel is { Count: > 0 }
                    ? new Dictionary<string, string>(pg.CeldasExcel, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                Encabezado = new FiscalDocumentoEncabezado
                {
                    TipoEcf = id.TipoeCF,
                    Encf = id.ENCF,
                    TipoIngreso = id.TipoIngresos ?? 1,
                    TipoPago = id.TipoPago ?? 1,
                    IndicadorMontoGravado = id.IndicadorMontoGravado,
                    IndicadorNotaCredito = id.IndicadorNotaCredito,
                    FechaVencimientoSecuencia = ParseDate(id.FechaVencimientoSecuencia),
                    FechaEmision = ParseDate(em.FechaEmision) ?? DateTime.Today,
                    RncEmisor = em.RNCEmisor,
                    RazonSocialEmisor = em.RazonSocialEmisor,
                    NombreComercialEmisor = em.NombreComercial,
                    DireccionEmisor = em.DireccionEmisor,
                    MunicipioEmisor = em.Municipio,
                    ProvinciaEmisor = em.Provincia,
                    TelefonoEmisor = em.TablaTelefonoEmisor?.FirstOrDefault()?.TelefonoEmisor,
                    CorreoEmisor = em.CorreoEmisor,
                    WebSite = em.WebSite,
                    Sucursal = em.Sucursal,
                    ActividadEconomica = em.ActividadEconomica,
                    CodigoVendedor = em.CodigoVendedor,
                    NumeroFacturaInterna = em.NumeroFacturaInterna,
                    NumeroPedidoInterno = em.NumeroPedidoInterno,
                    ZonaVenta = em.ZonaVenta,
                    InformacionAdicionalEmisor = em.InformacionAdicionalEmisor,
                    RncComprador = co.RNCComprador,
                    RazonSocialComprador = co.RazonSocialComprador,
                    ContactoComprador = co.ContactoComprador,
                    DireccionComprador = co.DireccionComprador,
                    CorreoComprador = co.CorreoComprador,
                    MunicipioComprador = co.MunicipioComprador,
                    ProvinciaComprador = co.ProvinciaComprador,
                    FechaEntrega = ParseDate(co.FechaEntrega),
                    FechaOrdenCompra = ParseDate(co.FechaOrdenCompra),
                    NumeroOrdenCompra = co.NumeroOrdenCompra,
                    CodigoInternoComprador = co.CodigoInternoComprador,
                    MontoTotal = to.MontoTotal,
                    MontoNoFacturable = to.MontoNoFacturable,
                    MontoPeriodo = to.MontoPeriodo,
                    ValorPagar = to.ValorPagar,
                    SaldoAnterior = to.SaldoAnterior,
                    MontoAvancePago = to.MontoAvancePago,
                    MontoGravadoTotal = to.MontoGravadoTotal ?? 0,
                    MontoGravadoI1 = to.MontoGravado1 ?? 0,
                    MontoGravadoI2 = to.MontoGravado2 ?? 0,
                    MontoGravadoI3 = to.MontoGravado3 ?? 0,
                    MontoExento = to.MontoExento ?? 0,
                    TotalItbis = to.TotalITBIS ?? 0,
                    TotalItbis1 = to.TotalITBIS1 ?? 0,
                    TotalItbis2 = to.TotalITBIS2 ?? 0,
                    TotalItbis3 = to.TotalITBIS3 ?? 0,
                    MontoImpuestoAdicional = to.MontoImpuestoAdicional,
                    ImpuestosAdicionales = to.ImpuestosAdicionales?
                        .Select(i => new FiscalImpuestoAdicional
                        {
                            TipoImpuesto = i.TipoImpuesto,
                            TasaImpuestoAdicional = i.TasaImpuestoAdicional,
                            MontoImpuestoSelectivoConsumoEspecifico = i.MontoImpuestoSelectivoConsumoEspecifico,
                            MontoImpuestoSelectivoConsumoAdvalorem = i.MontoImpuestoSelectivoConsumoAdvalorem,
                            OtrosImpuestosAdicionales = i.OtrosImpuestosAdicionales
                        }).ToList() ?? new(),
                    TotalItbisRetenido = to.TotalITBISRetenido ?? 0,
                    TotalIsrRetencion = to.TotalISRRetencion ?? 0,
                    MontoPropinaLegal = to.MontoPropinaLegal ?? 0
                }
            };

            foreach (var i in pg.Detalle)
            {
                doc.Lineas.Add(new FiscalDocumentoLinea
                {
                    NumeroLinea = i.NumeroLinea,
                    IndicadorFacturacion = i.IndicadorFacturacion,
                    NombreItem = i.NombreItem,
                    DescripcionItem = i.DescripcionItem,
                    EsBien = i.IndicadorBienoServicio != 2,
                    Cantidad = i.CantidadItem,
                    CantidadReferencia = i.CantidadReferencia,
                    UnidadReferencia = i.UnidadReferencia,
                    GradosAlcohol = i.GradosAlcohol,
                    PrecioUnitarioReferencia = i.PrecioUnitarioReferencia,
                    FechaElaboracion = ParseDate(i.FechaElaboracion),
                    FechaVencimientoItem = ParseDate(i.FechaVencimientoItem),
                    Subcantidad = i.Subcantidad,
                    CodigoSubcantidad = i.CodigoSubcantidad,
                    PrecioUnitario = i.PrecioUnitarioItem,
                    MontoItem = i.MontoItem,
                    DescuentoMonto = i.DescuentoMonto,
                    RecargoMonto = i.RecargoMonto,
                    UnidadMedida = i.UnidadMedida,
                    TipoImpuestoAdicional = i.TipoImpuestoAdicional,
                    IndicadorAgenteRetencionoPercepcion = i.Retencion?.IndicadorAgenteRetencionoPercepcion,
                    MontoItbisRetenido = i.Retencion?.MontoITBISRetenido,
                    MontoIsrRetenido = i.Retencion?.MontoISRRetenido
                });
            }

            if (id.TablaFormasPago != null)
            {
                foreach (var f in id.TablaFormasPago)
                {
                    doc.FormasPago.Add(new FiscalDocumentoFormaPagoDgii
                    {
                        FormaPago = f.FormaPago,
                        Monto = f.MontoPago
                    });
                }
            }

            if (pg.DescuentosORecargos != null)
            {
                var n = 1;
                foreach (var d in pg.DescuentosORecargos)
                {
                    doc.Descuentos.Add(new FiscalDocumentoDescuento
                    {
                        NumeroLinea = d.NumeroLinea > 0 ? d.NumeroLinea : n++,
                        EsDescuento = string.Equals(d.TipoAjuste, "D", StringComparison.OrdinalIgnoreCase),
                        Descripcion = d.DescripcionDescuentooRecargo,
                        EsMontoFijo = d.TipoValor != "%",
                        Monto = d.MontoDescuentooRecargo,
                        IndicadorFacturacion = d.IndicadorFacturacionDescuentooRecargo
                    });
                }
            }

            if (pg.Referencia != null && !string.IsNullOrWhiteSpace(pg.Referencia.NCFModificado))
            {
                doc.Referencia = new FiscalDocumentoReferencia
                {
                    NcfModificado = pg.Referencia.NCFModificado!,
                    RncOtroContribuyente = pg.Referencia.RNCOtroContribuyente,
                    FechaNcfModificado = ParseDate(pg.Referencia.FechaNCFModificado),
                    CodigoModificacion = pg.Referencia.CodigoModificacion ?? 1,
                    RazonModificacion = pg.Referencia.RazonModificacion
                };
            }

            return doc;
        }

        public static PgTrackIdResponse ToPgResponse(FiscalEnvioResultado r)
        {
            return new PgTrackIdResponse
            {
                trackId = r.TrackId,
                estado = r.Estado,
                codigo = r.Exitoso ? "1" : (r.CodigoError ?? "0"),
                encf = r.Encf,
                fechaRecepcion = r.FechaRecepcion?.ToString("o"),
                fechaFirma = r.FechaFirma?.ToString("dd-MM-yyyy HH:mm:ss"),
                qr = r.UrlQR,
                securityCode = r.SecurityCode,
                mensajes = r.Mensajes.Select(m => new PgMensajeDto { valor = m }).ToList(),
                // Debug: útil mientras afinamos trama vs XSD DGII
                xmlFirmado = r.Exitoso ? null : r.XmlFirmado,
                xmlRespuestaDgii = r.Exitoso ? null : r.XmlRespuesta
            };
        }

        public static PgTrackIdResponse ToPgResponse(FiscalConsultaResultado r)
        {
            return new PgTrackIdResponse
            {
                trackId = r.TrackId,
                estado = r.Estado,
                codigo = r.CodigoError,
                encf = r.Encf,
                rnc = r.Rnc,
                qr = r.UrlQR,
                securityCode = r.SecurityCode,
                fechaFirma = r.FechaFirma?.ToString("dd-MM-yyyy HH:mm:ss"),
                mensajes = r.Mensajes.Select(m => new PgMensajeDto { valor = m }).ToList()
            };
        }

        private static DateTime? ParseDate(string? s)
            => AlahiaPos.Entities.Fiscal.EcfDgiiFecha.Parse(s);
    }
}
