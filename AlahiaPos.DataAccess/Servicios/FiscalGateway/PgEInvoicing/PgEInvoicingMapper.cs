using AlahiaPos.Entities.Dto.Fiscal;
using System;
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
                NumeroFacturaInterna = enc.NumeroFacturaInterna,
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
                Encabezado = new PgEncabezadoWrapper
                {
                    IdDoc = idDoc,
                    Emisor = emisor,
                    Comprador = new PgCompradorDto
                    {
                        RNCComprador = enc.RncComprador ?? "",
                        RazonSocialComprador = razonComprador,
                        DireccionComprador = string.IsNullOrWhiteSpace(enc.DireccionComprador) ? null : enc.DireccionComprador,
                        CorreoComprador = string.IsNullOrWhiteSpace(enc.CorreoComprador) ? null : enc.CorreoComprador
                    },
                    Totales = new PgTotalesDto
                    {
                        MontoTotal = enc.MontoTotal,
                        MontoGravadoTotal = NullIfZero(enc.MontoGravadoTotal),
                        MontoGravado1 = NullIfZero(enc.MontoGravadoI1),
                        MontoGravado2 = NullIfZero(enc.MontoGravadoI2),
                        MontoGravado3 = NullIfZero(enc.MontoGravadoI3),
                        MontoExento = NullIfZero(enc.MontoExento),
                        TotalITBIS = NullIfZero(enc.TotalItbis),
                        TotalITBIS1 = NullIfZero(enc.TotalItbis1),
                        TotalITBIS2 = NullIfZero(enc.TotalItbis2),
                        TotalITBIS3 = NullIfZero(enc.TotalItbis3),
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
                IndicadorBienoServicio = l.EsBien ? 1 : 2,
                CantidadItem = l.Cantidad,
                PrecioUnitarioItem = l.PrecioUnitario,
                MontoItem = l.MontoItem,
                DescuentoMonto = NullIfZero(l.DescuentoMonto),
                RecargoMonto = NullIfZero(l.RecargoMonto),
                UnidadMedida = l.UnidadMedida
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
                    FechaNCFModificado = doc.Referencia.FechaNcfModificado?.ToString("yyyy-MM-ddTHH:mm:ss"),
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

            if (!esAceptado && !tieneTrackIdValido && !esResumen)
            {
                var msg = resp.mensajes?.FirstOrDefault()?.valor;
                return FiscalEnvioResultado.Error(
                    resp.codigo ?? "NO_PROCESADO",
                    msg ?? $"Proveedor no procesó el documento (codigo={resp.codigo}, estado={resp.estado}, secuenciaUtilizada={resp.secuenciaUtilizada})");
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
                FechaFirma = ParseFecha(resp.fechaFirma),
                FechaRecepcion = ParseFecha(resp.fechaRecepcion)
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
            };
        }

        private static DateTime? ParseFecha(string? fecha)
        {
            if (string.IsNullOrEmpty(fecha)) return null;
            if (DateTime.TryParse(fecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;
            return null;
        }

        private static decimal? NullIfZero(decimal? v) => v is null or 0 ? null : v;
        private static decimal? NullIfZero(decimal v) => v == 0 ? null : v;
    }
}
