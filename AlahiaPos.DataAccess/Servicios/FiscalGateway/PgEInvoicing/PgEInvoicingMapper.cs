using AlahiaPos.DataAccess.Servicios.FacturacionElectronica;
using AlahiaPos.Entities.Dto.Fiscal;
using AlahiaPos.Entities.Fiscal;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.PgEInvoicing
{
    internal static class PgEInvoicingMapper
    {
        public static PgDgiiDocumentDto ToProviderModel(FiscalDocumentoElectronico doc)
        {
            var enc = doc.Encabezado;

            var esE34 = enc.TipoEcf == 34;

            var idDoc = new PgIdDocDto
            {
                TipoeCF = enc.TipoEcf,
                ENCF = enc.Encf,
                IndicadorNotaCredito = enc.IndicadorNotaCredito,
                IndicadorMontoGravado = enc.IndicadorMontoGravado,
                // E34: FechaVencimientoSecuencia no existe en XSD; no enviar.
                FechaVencimientoSecuencia = esE34
                    ? null
                    : enc.FechaVencimientoSecuencia?.ToString("yyyy-MM-ddTHH:mm:ss"),
                TipoIngresos = enc.TipoIngreso,
                TipoPago = enc.TipoPago
            };

            // E34: TablaFormasPago prohibida en IdDoc (DGII rechaza por XSD).
            if (!esE34 && doc.FormasPago.Any())
            {
                idDoc.TablaFormasPago = doc.FormasPago.Select(f => new PgFormaPagoDto
                {
                    FormaPago = f.FormaPago,
                    MontoPago = f.Monto
                }).ToList();
            }

            var emisor = new PgEmisorDto
            {
                RNCEmisor = enc.RncEmisor,
                RazonSocialEmisor = enc.RazonSocialEmisor,
                NombreComercial = enc.NombreComercialEmisor,
                DireccionEmisor = enc.DireccionEmisor ?? "",
                Municipio = enc.MunicipioEmisor,
                Provincia = enc.ProvinciaEmisor,
                CorreoEmisor = enc.CorreoEmisor,
                WebSite = enc.WebSite,
                Sucursal = enc.Sucursal,
                ActividadEconomica = enc.ActividadEconomica,
                CodigoVendedor = enc.CodigoVendedor,
                NumeroFacturaInterna = enc.NumeroFacturaInterna,
                NumeroPedidoInterno = enc.NumeroPedidoInterno,
                ZonaVenta = enc.ZonaVenta,
                InformacionAdicionalEmisor = enc.InformacionAdicionalEmisor,
                FechaEmision = enc.FechaEmision.ToString("yyyy-MM-ddTHH:mm:ss")
            };

            if (!string.IsNullOrEmpty(enc.TelefonoEmisor))
            {
                emisor.TablaTelefonoEmisor = new()
                {
                    new PgTelefonoEmisorDto { TelefonoEmisor = enc.TelefonoEmisor }
                };
            }

            var razonComprador = string.IsNullOrWhiteSpace(enc.RazonSocialComprador)
                ? "CONSUMIDOR"
                : enc.RazonSocialComprador;

            var pg = new PgDgiiDocumentDto
            {
                IdEmpresa = doc.IdEmpresa,
                TipoDocumentoAlahia = doc.TipoDocumentoAlahia,
                AmbienteDgii = doc.AmbienteDgii,
                CeldasExcel = doc.CeldasExcel is { Count: > 0 } ? doc.CeldasExcel : null,
                Encabezado = new PgEncabezadoWrapper
                {
                    IdDoc = idDoc,
                    Emisor = emisor,
                    Comprador = new PgCompradorDto
                    {
                        RNCComprador = enc.RncComprador ?? "",
                        RazonSocialComprador = razonComprador,
                        ContactoComprador = string.IsNullOrWhiteSpace(enc.ContactoComprador) ? null : enc.ContactoComprador,
                        DireccionComprador = string.IsNullOrWhiteSpace(enc.DireccionComprador) ? null : enc.DireccionComprador,
                        CorreoComprador = string.IsNullOrWhiteSpace(enc.CorreoComprador) ? null : enc.CorreoComprador,
                        MunicipioComprador = string.IsNullOrWhiteSpace(enc.MunicipioComprador) ? null : enc.MunicipioComprador,
                        ProvinciaComprador = string.IsNullOrWhiteSpace(enc.ProvinciaComprador) ? null : enc.ProvinciaComprador,
                        FechaEntrega = enc.FechaEntrega?.ToString("dd-MM-yyyy"),
                        FechaOrdenCompra = enc.FechaOrdenCompra?.ToString("dd-MM-yyyy"),
                        NumeroOrdenCompra = string.IsNullOrWhiteSpace(enc.NumeroOrdenCompra) ? null : enc.NumeroOrdenCompra,
                        CodigoInternoComprador = string.IsNullOrWhiteSpace(enc.CodigoInternoComprador) ? null : enc.CodigoInternoComprador
                    },
                    Totales = new PgTotalesDto
                    {
                        MontoTotal = enc.MontoTotal,
                        MontoNoFacturable = enc.MontoNoFacturable,
                        MontoPeriodo = enc.MontoPeriodo,
                        SaldoAnterior = enc.SaldoAnterior,
                        MontoAvancePago = enc.MontoAvancePago,
                        ValorPagar = enc.ValorPagar,
                        MontoGravadoTotal = NullIfZero(enc.MontoGravadoTotal),
                        MontoGravado1 = NullIfZero(enc.MontoGravadoI1),
                        MontoGravado2 = NullIfZero(enc.MontoGravadoI2),
                        MontoGravado3 = NullIfZero(enc.MontoGravadoI3),
                        MontoExento = NullIfZero(enc.MontoExento),
                        TotalITBIS = NullIfZero(enc.TotalItbis),
                        TotalITBIS1 = NullIfZero(enc.TotalItbis1),
                        TotalITBIS2 = NullIfZero(enc.TotalItbis2),
                        TotalITBIS3 = NullIfZero(enc.TotalItbis3),
                        MontoImpuestoAdicional = enc.MontoImpuestoAdicional is > 0 ? enc.MontoImpuestoAdicional : null,
                        ImpuestosAdicionales = enc.ImpuestosAdicionales is { Count: > 0 }
                            ? enc.ImpuestosAdicionales.Select(i => new PgImpuestoAdicionalDto
                            {
                                TipoImpuesto = i.TipoImpuesto,
                                TasaImpuestoAdicional = i.TasaImpuestoAdicional,
                                MontoImpuestoSelectivoConsumoEspecifico = i.MontoImpuestoSelectivoConsumoEspecifico,
                                MontoImpuestoSelectivoConsumoAdvalorem = i.MontoImpuestoSelectivoConsumoAdvalorem,
                                OtrosImpuestosAdicionales = i.OtrosImpuestosAdicionales
                            }).ToList()
                            : null,
                        TotalITBISRetenido = NullIfZero(enc.TotalItbisRetenido),
                        TotalISRRetencion = NullIfZero(enc.TotalIsrRetencion),
                        MontoPropinaLegal = NullIfZero(enc.MontoPropinaLegal)
                    }
                }
            };

            pg.Detalle = doc.Lineas.Select(l => new PgItemDto
            {
                NumeroLinea = l.NumeroLinea,
                IndicadorFacturacion = l.IndicadorFacturacion,
                NombreItem = l.NombreItem,
                DescripcionItem = string.IsNullOrWhiteSpace(l.DescripcionItem) ? null : l.DescripcionItem,
                IndicadorBienoServicio = l.EsBien ? 1 : 2,
                CantidadItem = l.Cantidad,
                CantidadReferencia = l.CantidadReferencia,
                UnidadReferencia = l.UnidadReferencia,
                GradosAlcohol = l.GradosAlcohol is > 0 ? l.GradosAlcohol : null,
                PrecioUnitarioReferencia = l.PrecioUnitarioReferencia is > 0 ? l.PrecioUnitarioReferencia : null,
                FechaElaboracion = l.FechaElaboracion?.ToString("dd-MM-yyyy"),
                FechaVencimientoItem = l.FechaVencimientoItem?.ToString("dd-MM-yyyy"),
                Subcantidad = l.Subcantidad,
                CodigoSubcantidad = l.CodigoSubcantidad,
                PrecioUnitarioItem = l.PrecioUnitario,
                MontoItem = l.MontoItem,
                DescuentoMonto = NullIfZero(l.DescuentoMonto),
                RecargoMonto = NullIfZero(l.RecargoMonto),
                UnidadMedida = l.UnidadMedida,
                TipoImpuestoAdicional = string.IsNullOrWhiteSpace(l.TipoImpuestoAdicional) ? null : l.TipoImpuestoAdicional.Trim(),
                Retencion = l.IndicadorAgenteRetencionoPercepcion == null
                    && l.MontoItbisRetenido == null
                    && l.MontoIsrRetenido == null
                    ? null
                    : new PgRetencionDto
                    {
                        IndicadorAgenteRetencionoPercepcion = l.IndicadorAgenteRetencionoPercepcion ?? 1,
                        MontoITBISRetenido = l.MontoItbisRetenido,
                        MontoISRRetenido = l.MontoIsrRetenido
                    }
            }).ToList();

            if (doc.Descuentos.Any())
            {
                pg.DescuentosORecargos = doc.Descuentos.Select(d => new PgDescuentoRecargoDto
                {
                    NumeroLinea = d.NumeroLinea,
                    TipoAjuste = d.EsDescuento ? "D" : "R",
                    DescripcionDescuentooRecargo = d.Descripcion,
                    TipoValor = d.EsMontoFijo ? "$" : "%",
                    MontoDescuentooRecargo = d.Monto,
                    IndicadorFacturacionDescuentooRecargo = d.IndicadorFacturacion
                }).ToList();
            }

            if (doc.Referencia != null)
            {
                pg.Referencia = new PgReferenciaDto
                {
                    NCFModificado = doc.Referencia.NcfModificado,
                    RNCOtroContribuyente = doc.Referencia.RncOtroContribuyente,
                    FechaNCFModificado = doc.Referencia.FechaNcfModificado?.ToString("yyyy-MM-dd"),
                    CodigoModificacion = doc.Referencia.CodigoModificacion,
                    RazonModificacion = doc.Referencia.RazonModificacion
                };
            }

            return pg;
        }

        private static readonly string ZeroGuid = "00000000-0000-0000-0000-000000000000";

        public static FiscalEnvioResultado ToEnvioResultado(PgTrackIdResponse? resp, int tipoEcf = 0)
        {
            if (resp == null)
                return FiscalEnvioResultado.Error("NULL_RESPONSE", "Sin respuesta del proveedor");

            var tieneTrackIdValido = !string.IsNullOrEmpty(resp.trackId) && resp.trackId != ZeroGuid;
            var esAceptado = resp.estado == "Aceptado" || resp.estado == "AceptadoCondicional" || resp.codigo == "1";
            var esResumen = tipoEcf == 32;
            var mensajes = resp.mensajes?
                .Where(m => !string.IsNullOrEmpty(m.valor))
                .Select(m => m.valor!)
                .ToList() ?? new List<string>();
            var secuenciaUsada = resp.secuenciaUtilizada == true
                || EcfSecuenciaYaUtilizada.EnMensajes(null, mensajes);

            if (secuenciaUsada && !esAceptado)
            {
                var error = FiscalEnvioResultado.Error(
                    resp.codigo ?? "SECUENCIA_UTILIZADA",
                    mensajes.FirstOrDefault()
                    ?? $"Este número de secuencia ya ha sido utilizado (codigo={resp.codigo}, estado={resp.estado})");
                error.SecuenciaUtilizada = true;
                error.Encf = resp.encf;
                error.TrackId = resp.trackId;
                error.TransmissionJobId = resp.transmissionJobId;
                error.SecurityCode = resp.securityCode;
                error.UrlQR = resp.qr;
                error.Estado = string.IsNullOrWhiteSpace(resp.estado) ? "Rechazado" : resp.estado;
                error.XmlRespuesta = resp.xmlRespuestaDgii;
                if (mensajes.Count > 0)
                    error.Mensajes = mensajes;
                return error;
            }

            if (!esAceptado && !tieneTrackIdValido && !esResumen)
            {
                var msg = mensajes.FirstOrDefault();
                var error = FiscalEnvioResultado.Error(
                    resp.codigo ?? "NO_PROCESADO",
                    msg ?? $"Proveedor no procesó el documento (codigo={resp.codigo}, estado={resp.estado}, secuenciaUtilizada={resp.secuenciaUtilizada})");
                error.SecuenciaUtilizada = secuenciaUsada;
                error.Encf = resp.encf;
                error.XmlRespuesta = resp.xmlRespuestaDgii;
                if (mensajes.Count > 0)
                    error.Mensajes = mensajes;
                return error;
            }

            return new FiscalEnvioResultado
            {
                Exitoso = true,
                TrackId = resp.trackId,
                TransmissionJobId = resp.transmissionJobId,
                Estado = esResumen && string.IsNullOrEmpty(resp.estado) ? "Resumen" : (resp.estado ?? "Desconocido"),
                Encf = resp.encf,
                CodigoError = esAceptado ? null : resp.codigo,
                Mensajes = resp.mensajes?
                    .Where(m => !string.IsNullOrEmpty(m.valor))
                    .Select(m => m.valor!)
                    .ToList() ?? new(),
                SecurityCode = resp.securityCode,
                UrlQR = resp.qr,
                FechaFirma = ParseFecha(resp.fechaFirma)
                            ?? EcfDgiiFecha.ExtraerFechaHoraFirmaXml(resp.xmlFirmado),
                FechaRecepcion = ParseFecha(resp.fechaRecepcion),
                XmlRespuesta = resp.xmlRespuestaDgii
            };
        }

        public static FiscalConsultaResultado ToConsultaResultado(PgTrackIdResponse? resp, string trackId)
        {
            if (resp == null)
                return new FiscalConsultaResultado { TrackId = trackId, Estado = "Error", Mensajes = new() { "Sin respuesta" } };

            return new FiscalConsultaResultado
            {
                TrackId = trackId,
                Estado = resp.estado ?? "Desconocido",
                Encf = resp.encf,
                Rnc = resp.rnc,
                CodigoError = resp.estado == "Aceptado" ? null : resp.codigo,
                Mensajes = resp.mensajes?
                    .Where(m => !string.IsNullOrEmpty(m.valor))
                    .Select(m => m.valor!)
                    .ToList() ?? new(),
                SecurityCode = resp.securityCode,
                UrlQR = resp.qr,
                FechaFirma = ParseFecha(resp.fechaFirma)
                            ?? EcfDgiiFecha.ExtraerFechaHoraFirmaXml(resp.xmlFirmado)
            };
        }

        private static DateTime? ParseFecha(string? fecha) => EcfDgiiFecha.Parse(fecha);

        private static decimal? NullIfZero(decimal? v) => v is null or 0 ? null : v;
        private static decimal? NullIfZero(decimal v) => v == 0 ? null : v;
    }
}
