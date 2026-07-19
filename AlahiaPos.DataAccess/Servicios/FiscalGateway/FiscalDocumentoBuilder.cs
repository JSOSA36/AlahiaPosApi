using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto.Fiscal;
using System;
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
            // Misma convención que certificación directa / ejemplos aceptados:
            // IndicadorMontoGravado = 0 → MontoItem y MontoGravado SIN ITBIS.
            var hayGravadoItbis = docInfo.Lineas.Any(l => l.MontoItbis > 0)
                || docInfo.TotalItbis > 0;

            var doc = new FiscalDocumentoElectronico
            {
                IdDocumentoInterno = idOrigen,
                TipoDocumentoAlahia = ((OrigenDocumento)origenDocumento).ToString(),
                Encabezado = new FiscalDocumentoEncabezado
                {
                    TipoEcf = tipoEcfDgii,
                    Encf = ecf.ENCF,
                    TipoIngreso = 1,
                    TipoPago = 1,
                    IndicadorMontoGravado = hayGravadoItbis ? 0 : null,
                    FechaVencimientoSecuencia = secuencia?.fechaVencimiento,
                    FechaEmision = docInfo.FechaDocumento,
                    NumeroFacturaInterna = docInfo.NumeroDocumentoInterno,

                    RncEmisor = empresa?.RNC ?? "",
                    RazonSocialEmisor = empresa?.NombreComercial ?? "",
                    NombreComercialEmisor = empresa?.NombreComercial,
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
                        exento += montoItem;
                        break;
                    case 4:
                        // tasa 0% (export/cero) — en E31/E32 local usamos 3 para exento;
                        // 4 se trata como gravado tasa 0 en algunos flujos.
                        gravado3 += montoItem;
                        itbis3 += itbis;
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

            if (!doc.FormasPago.Any() && montoTotal > 0)
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

            return doc;
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
            if (tasaItbis == null) return 1;
            var tasa = tasaItbis.Value;
            if (tasa >= 16m && tasa <= 18m) return 1;
            if (tasa > 0m && tasa < 16m) return 2;
            if (tasa > 0.15m && tasa <= 0.18m) return 1;
            if (tasa > 0m && tasa <= 0.16m) return 2;
            if (tasa == 0m) return 3;
            return 1;
        }
    }
}
