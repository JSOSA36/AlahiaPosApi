using AlahiaPos.DataAccess.Servicios.FacturacionElectronica;
using AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto.Fiscal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway
{
    public static class FiscalDocumentoBuilder
    {
        public static FiscalDocumentoElectronico Build(
            ECFEncabezado ecf,
            DocumentoOrigenInfo docInfo,
            int idOrigen,
            int origenDocumento,
            int tipoEcfDgii,
            Empresas? empresa,
            SecuenciaECF? secuencia)
        {
            var hayGravadoItbis = docInfo.Lineas.Any(l => l.MontoItbis > 0)
                || docInfo.TotalItbis > 0
                || docInfo.Lineas.Any(l => l.TasaItbis is > 0);
            var rncEmisor = CertecfArtefactos.RncEmisorParaEmpresa(empresa);

            var doc = new FiscalDocumentoElectronico
            {
                IdEmpresa = empresa?.IdEmpresa ?? ecf.IdEmpresa,
                IdDocumentoInterno = idOrigen,
                TipoDocumentoAlahia = ((OrigenDocumento)origenDocumento).ToString(),
                AmbienteDgii = DgiiAmbienteHelper.Normalize(
                    !string.IsNullOrWhiteSpace(empresa?.AmbienteFE)
                        ? empresa!.AmbienteFE
                        : secuencia?.Ambiente),
                Encabezado = new FiscalDocumentoEncabezado
                {
                    TipoEcf = tipoEcfDgii,
                    Encf = ecf.ENCF,
                    TipoIngreso = 1,
                    TipoPago = 1,
                    IndicadorMontoGravado = hayGravadoItbis ? 0 : null,
                    IndicadorNotaCredito = tipoEcfDgii == 34
                        ? CalcularIndicadorNotaCredito(
                            docInfo.FechaDocumentoModificado,
                            docInfo.FechaDocumento)
                        : null,
                    // E34: FechaVencimientoSecuencia no aplica en XSD.
                    FechaVencimientoSecuencia = tipoEcfDgii == 34
                        ? null
                        : secuencia?.fechaVencimiento,
                    FechaEmision = docInfo.FechaDocumento,
                    NumeroFacturaInterna = docInfo.NumeroDocumentoInterno,

                    RncEmisor = rncEmisor,
                    RazonSocialEmisor = CertecfArtefactos.RazonSocialRealParaRnc(
                        rncEmisor,
                        CertecfArtefactos.EsRncDraSena(rncEmisor) ? CertecfArtefactos.RazonSocialDraSena : null,
                        empresa?.NombreComercial),
                    NombreComercialEmisor = CertecfArtefactos.NombreComercialRealParaRnc(
                        rncEmisor, empresa?.NombreComercial),
                    DireccionEmisor = empresa?.Direccion,
                    TelefonoEmisor = FormatearTelefono(empresa?.Telefono),
                    CorreoEmisor = empresa?.CorreElectronico,

                    RncComprador = docInfo.RncCliente,
                    RazonSocialComprador = docInfo.NombreCliente,
                    DireccionComprador = docInfo.DireccionCliente,
                    CorreoComprador = docInfo.CorreoCliente
                }
            };

            int linea = 1;
            decimal gravado1 = 0, gravado2 = 0, gravado3 = 0, exento = 0;
            decimal itbis1 = 0, itbis2 = 0, itbis3 = 0;

            foreach (var l in docInfo.Lineas)
            {
                int indFact = ResolverIndicadorFacturacion(l.TasaItbis);
                var montoItem = Math.Round(l.MontoItem, 2);
                var precio = Math.Round(l.PrecioUnitario, 2);
                var itbis = Math.Round(l.MontoItbis, 2);

                // Si hay ITBIS pero no tasa, asumir 18% (tasa 1).
                if (itbis > 0 && indFact == 3)
                    indFact = 1;

                // Coherencia Cantidad × Precio ≈ MontoItem (ajustando redondeo de 1 unidad).
                if (l.Cantidad == 1 && precio != montoItem)
                    precio = montoItem;
                else if (l.Cantidad > 0)
                {
                    var esperado = Math.Round(precio * l.Cantidad, 2);
                    if (esperado != montoItem)
                        montoItem = esperado;
                }

                doc.Lineas.Add(new FiscalDocumentoLinea
                {
                    NumeroLinea = linea++,
                    NombreItem = l.Descripcion,
                    EsBien = l.EsBien,
                    Cantidad = l.Cantidad,
                    PrecioUnitario = precio,
                    MontoItem = montoItem,
                    IndicadorFacturacion = indFact,
                    UnidadMedida = 43
                });

                switch (indFact)
                {
                    case 1:
                        gravado1 += montoItem;
                        itbis1 += itbis > 0 ? itbis : Math.Round(montoItem * 0.18m, 2);
                        break;
                    case 2:
                        gravado2 += montoItem;
                        itbis2 += itbis > 0 ? itbis : Math.Round(montoItem * 0.16m, 2);
                        break;
                    case 3:
                        // ITBIS 0% (indicador 3). El total va en MontoGravadoI3, no en exento.
                        gravado3 += montoItem;
                        itbis3 += itbis;
                        break;
                    case 4:
                        exento += montoItem;
                        break;
                }
            }

            gravado1 = Math.Round(gravado1, 2);
            gravado2 = Math.Round(gravado2, 2);
            gravado3 = Math.Round(gravado3, 2);
            exento = Math.Round(exento, 2);

            // DGII: TotalITBISx debe = MontoGravadoIx × tasa (con IndicadorMontoGravado=0).
            if (gravado1 > 0)
                itbis1 = Math.Round(gravado1 * 0.18m, 2);
            if (gravado2 > 0)
                itbis2 = Math.Round(gravado2 * 0.16m, 2);

            var gravadoTotal = Math.Round(gravado1 + gravado2 + gravado3, 2);
            var totalItbis = Math.Round(itbis1 + itbis2 + itbis3, 2);
            var montoTotal = Math.Round(gravadoTotal + totalItbis + exento, 2);

            doc.Encabezado.MontoGravadoTotal = gravadoTotal;
            doc.Encabezado.MontoGravadoI1 = gravado1;
            doc.Encabezado.MontoGravadoI2 = gravado2;
            doc.Encabezado.MontoGravadoI3 = gravado3;
            doc.Encabezado.MontoExento = exento;
            doc.Encabezado.TotalItbis = totalItbis;
            doc.Encabezado.TotalItbis1 = itbis1;
            doc.Encabezado.TotalItbis2 = itbis2;
            doc.Encabezado.TotalItbis3 = itbis3;
            doc.Encabezado.MontoTotal = montoTotal;

            foreach (var f in docInfo.FormasPago)
            {
                doc.FormasPago.Add(new FiscalDocumentoFormaPagoDgii
                {
                    FormaPago = f.FormaPagoDgii,
                    Monto = Math.Round(f.Monto, 2)
                });
            }

            // E34 no usa TablaFormasPago.
            if (tipoEcfDgii == 34)
            {
                doc.FormasPago.Clear();
            }
            else if (!doc.FormasPago.Any() && montoTotal > 0)
            {
                doc.FormasPago.Add(new FiscalDocumentoFormaPagoDgii
                {
                    FormaPago = 1,
                    Monto = montoTotal
                });
            }
            else if (doc.FormasPago.Count == 1)
            {
                // Alinear pago único al MontoTotal fiscal recalculado.
                doc.FormasPago[0].Monto = montoTotal;
            }

            // Alinear con validación DGII: si quedó alguna línea gravada (1/2/3), forzar indicador.
            if (doc.Lineas.Any(l => l.IndicadorFacturacion is 1 or 2 or 3)
                && doc.Encabezado.IndicadorMontoGravado is not (0 or 1))
            {
                doc.Encabezado.IndicadorMontoGravado = 0;
            }

            if (!string.IsNullOrEmpty(docInfo.NcfModificado))
            {
                doc.Referencia = new FiscalDocumentoReferencia
                {
                    NcfModificado = docInfo.NcfModificado,
                    FechaNcfModificado = docInfo.FechaDocumentoModificado,
                    CodigoModificacion = docInfo.CodigoModificacion ?? 3,
                    RazonModificacion = docInfo.RazonModificacion
                };
            }

            AlinearConDefinicionDgii(doc);
            return doc;
        }

        /// <summary>
        /// Misma reglas que ya exigen/emiten las definiciones e-CF (E41/43/44/46/47
        /// e IndicadorMontoGravado). No inventa montos de retención.
        /// </summary>
        public static void AlinearConDefinicionDgii(FiscalDocumentoElectronico doc)
        {
            if (doc?.Encabezado == null) return;
            QuitarLineasPlantillaVacias(doc);
            var amb = (doc.AmbienteDgii ?? "").Trim().ToLowerInvariant();
            if (amb is "certecf" or "cert" or "certificacion")
                return;

            var enc = doc.Encabezado;
            var tipo = enc.TipoEcf;
            var lineas = doc.Lineas ?? new List<FiscalDocumentoLinea>();

            switch (tipo)
            {
                case 41:
                    foreach (var l in lineas)
                    {
                        l.EsBien = false;
                        if (l.IndicadorAgenteRetencionoPercepcion is not (1 or 2))
                            l.IndicadorAgenteRetencionoPercepcion = 1;
                    }
                    AlinearRetencion(enc, lineas, incluirItbis: true);
                    break;
                case 43:
                    enc.RncComprador = null;
                    enc.RazonSocialComprador = null;
                    enc.DireccionComprador = null;
                    enc.CorreoComprador = null;
                    enc.IndicadorMontoGravado = null;
                    foreach (var l in lineas) l.IndicadorFacturacion = 4;
                    if (enc.MontoExento <= 0) enc.MontoExento = enc.MontoTotal;
                    break;
                case 44:
                    enc.IndicadorMontoGravado = null;
                    foreach (var l in lineas) l.IndicadorFacturacion = 4;
                    if (enc.MontoExento <= 0) enc.MontoExento = enc.MontoTotal;
                    break;
                case 46:
                    enc.IndicadorMontoGravado = null;
                    foreach (var l in lineas) l.IndicadorFacturacion = 3;
                    break;
                case 47:
                    enc.IndicadorMontoGravado = null;
                    foreach (var l in lineas)
                    {
                        l.IndicadorFacturacion = 4;
                        l.MontoIsrRetenido ??= 0m;
                    }
                    AlinearRetencion(enc, lineas, incluirItbis: false);
                    break;
            }

            var hayGravado = lineas.Any(l => l.IndicadorFacturacion is 1 or 2 or 3);
            if (hayGravado && enc.IndicadorMontoGravado is not (0 or 1) && tipo is 31 or 32 or 33 or 34 or 41 or 45)
                enc.IndicadorMontoGravado = 1;

            if (enc.IndicadorMontoGravado == 0)
            {
                var esperado = Math.Round(enc.MontoGravadoTotal + enc.TotalItbis + enc.MontoExento, 2);
                if (Math.Abs(enc.MontoTotal - esperado) > 0.01m)
                    enc.IndicadorMontoGravado = 1;
            }
        }

        /// <summary>
        /// El Excel CerteCF trae ~60 columnas de ítem; las vacías llegan como #e / monto 0.
        /// </summary>
        private static void QuitarLineasPlantillaVacias(FiscalDocumentoElectronico doc)
        {
            if (doc.Lineas == null || doc.Lineas.Count == 0) return;
            doc.Lineas = doc.Lineas.Where(l => !EsLineaPlantillaVacia(l)).ToList();
            for (var i = 0; i < doc.Lineas.Count; i++)
                doc.Lineas[i].NumeroLinea = i + 1;
        }

        private static bool EsLineaPlantillaVacia(FiscalDocumentoLinea l)
        {
            var nombre = (l.NombreItem ?? "").Trim();
            if (nombre.StartsWith("#", StringComparison.Ordinal))
                return true;
            if (l.MontoItem <= 0 && l.PrecioUnitario <= 0 && l.Cantidad <= 0)
                return string.IsNullOrWhiteSpace(nombre) || nombre.StartsWith("Item ", StringComparison.OrdinalIgnoreCase);
            return false;
        }

        private static void AlinearRetencion(
            FiscalDocumentoEncabezado enc,
            List<FiscalDocumentoLinea> lineas,
            bool incluirItbis)
        {
            if (lineas.Count == 0) return;
            var isrLineas = lineas.Sum(l => l.MontoIsrRetenido ?? 0m);
            var itbLineas = lineas.Sum(l => l.MontoItbisRetenido ?? 0m);

            if (enc.TotalIsrRetencion <= 0 && isrLineas > 0)
                enc.TotalIsrRetencion = isrLineas;
            if (incluirItbis && enc.TotalItbisRetenido <= 0 && itbLineas > 0)
                enc.TotalItbisRetenido = itbLineas;

            if (enc.TotalIsrRetencion > 0 && isrLineas <= 0)
                RepartirMonto(lineas, enc.TotalIsrRetencion, (l, v) => l.MontoIsrRetenido = v);
            if (incluirItbis && enc.TotalItbisRetenido > 0 && itbLineas <= 0)
                RepartirMonto(lineas, enc.TotalItbisRetenido, (l, v) => l.MontoItbisRetenido = v);
        }

        private static void RepartirMonto(
            List<FiscalDocumentoLinea> lineas,
            decimal total,
            Action<FiscalDocumentoLinea, decimal> set)
        {
            var baseSum = lineas.Sum(l => l.MontoItem);
            if (baseSum <= 0 || lineas.Count == 1)
            {
                set(lineas[0], Math.Round(total, 2));
                for (var i = 1; i < lineas.Count; i++) set(lineas[i], 0m);
                return;
            }

            decimal acumulado = 0;
            for (var i = 0; i < lineas.Count; i++)
            {
                var parte = i == lineas.Count - 1
                    ? Math.Round(total - acumulado, 2)
                    : Math.Round(total * (lineas[i].MontoItem / baseSum), 2);
                set(lineas[i], parte);
                acumulado += parte;
            }
        }

        public static string? FormatearTelefono(string? tel)
        {
            if (string.IsNullOrWhiteSpace(tel)) return null;
            var digits = new string(tel.Where(char.IsDigit).ToArray());
            if (digits.Length == 10)
                return $"{digits[..3]}-{digits[3..6]}-{digits[6..]}";
            return digits;
        }

        public static int ResolverIndicadorFacturacion(decimal? tasaItbis)
        {
            // DGII: 1 = 18%, 2 = 16%, 3 = tasa cero, 4 = exento.
            // Sin ITBIS (tasa nula o 0) es exento. Tasa cero no se infiere.
            if (tasaItbis == null || tasaItbis.Value == 0m) return 4;
            var tasa = tasaItbis.Value;
            if (tasa >= 16m && tasa <= 18m) return 1;
            if (tasa > 0m && tasa < 16m) return 2;
            if (tasa > 0.15m && tasa <= 0.18m) return 1;
            if (tasa > 0m && tasa <= 0.16m) return 2;
            return 1;
        }

        /// <summary>
        /// E34: 0 si emisión ≤30 días del NCF modificado; 1 si &gt;30 días.
        /// </summary>
        public static int CalcularIndicadorNotaCredito(
            DateTime? fechaNcfModificado,
            DateTime fechaEmision)
        {
            if (!fechaNcfModificado.HasValue)
                return 0;

            var dias = (fechaEmision.Date - fechaNcfModificado.Value.Date).TotalDays;
            return dias > 30 ? 1 : 0;
        }
    }
}
