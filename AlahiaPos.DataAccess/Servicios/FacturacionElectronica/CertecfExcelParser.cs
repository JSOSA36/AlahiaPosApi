using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AlahiaPos.DataAccess.Servicios.FiscalGateway;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto.Fiscal;
using ClosedXML.Excel;

namespace AlahiaPos.DataAccess.Servicios.FacturacionElectronica
{
    /// <summary>
    /// Lee el Excel oficial de CerteCF (hoja ancha con nombres de campos XML)
    /// o un Encabezado + Detalle unidos por e-NCF.
    /// </summary>
    public static class CertecfExcelParser
    {
        public const decimal UmbralRfce = 250000m;

        public static CertecfParseResult Parse(Stream excel, Empresas empresa)
        {
            using var wb = new XLWorkbook(excel);
            if (wb.Worksheets.Count == 0)
                throw new InvalidOperationException("El Excel no tiene hojas.");

            var sheets = wb.Worksheets.ToList();
            var headerSheet = FindSheet(sheets, "encabezado", "e-cf", "ecf", "set", "prueba") ?? sheets[0];
            var detalleSheet = FindSheet(sheets, "detalle", "items", "detalleitems");

            var headerMap = ReadHeaderMap(headerSheet);
            if (headerMap.Count == 0)
                throw new InvalidOperationException("No se encontraron columnas de encabezado en la primera fila del Excel.");

            var esAcecf = EsAcecf(headerMap);
            if (esAcecf)
                return ParseAcecf(headerSheet, headerMap, empresa);

            var docs = new List<FiscalDocumentoElectronico>();
            var used = headerSheet.RangeUsed();
            if (used == null)
                throw new InvalidOperationException("La hoja de datos está vacía.");

            var lastRow = used.LastRow().RowNumber();
            for (var row = 2; row <= lastRow; row++)
            {
                string Cell(string key) => GetCell(headerSheet, headerMap, row, key);
                var encf = FirstNonEmpty(Cell("encf"), Cell("encf"), Cell("ncf"));
                var tipoRaw = FirstNonEmpty(Cell("tipoecf"), Cell("tipo"), Cell("tipocomprobante"));
                if (string.IsNullOrWhiteSpace(encf) && string.IsNullOrWhiteSpace(tipoRaw))
                    continue;

                var tipo = ParseTipo(tipoRaw, encf);
                if (tipo <= 0)
                    continue;

                encf = NormalizarEncf(encf, tipo);
                var doc = MapDocumento(headerSheet, headerMap, row, empresa, tipo, encf);
                docs.Add(doc);
            }

            if (detalleSheet != null && docs.Count > 0)
                MergeDetalle(detalleSheet, docs);

            foreach (var d in docs)
                AlinearConDefinicionDgii(d);
            CompletarRncExportacionDesdeHermanos(docs);

            if (docs.Count == 0)
                throw new InvalidOperationException(
                    "No se encontraron comprobantes. El Excel de CerteCF debe traer columnas TipoeCF / eNCF en la primera fila.");

            var casos = docs.Select((d, i) => new CertecfCasoParse
            {
                Orden = i + 1,
                Oleada = ResolverOleada(d.Encabezado.TipoEcf, d.Encabezado.MontoTotal),
                Documento = d
            }).ToList();

            return new CertecfParseResult
            {
                TipoSet = esAcecf ? "ACECF" : "ECF",
                Columnas = headerMap.Count,
                Casos = casos
            };
        }

        private static bool EsAcecf(Dictionary<string, int> headerMap)
        {
            if (!headerMap.ContainsKey("encf")) return false;
            var tieneEstado = headerMap.ContainsKey("estado");
            var tieneAcecf = headerMap.ContainsKey("motivo")
                || headerMap.ContainsKey("motivorechazo")
                || headerMap.ContainsKey("detallemotivorechazo")
                || headerMap.ContainsKey("fechahoraaprobacioncomercial")
                || headerMap.ContainsKey("rnccomprador") && tieneEstado && !headerMap.ContainsKey("tipoecf");
            return tieneEstado && tieneAcecf;
        }

        private static CertecfParseResult ParseAcecf(IXLWorksheet ws, Dictionary<string, int> headerMap, Empresas empresa)
        {
            var used = ws.RangeUsed()
                ?? throw new InvalidOperationException("La hoja ACECF está vacía.");
            var lastRow = used.LastRow().RowNumber();
            var casos = new List<CertecfCasoParse>();
            var acecf = new List<AcecfDocumento>();
            for (var row = 2; row <= lastRow; row++)
            {
                string Cell(string key) => GetCell(ws, headerMap, row, key);
                var encf = FirstNonEmpty(Cell("encf"), Cell("ncf"));
                if (string.IsNullOrWhiteSpace(encf)) continue;

                var estadoRaw = Cell("estado");
                var estado = ParseAcecfEstado(estadoRaw);
                var motivo = FirstNonEmpty(
                    Cell("detallemotivorechazo"), Cell("motivorechazo"), Cell("motivo"));
                TryDecimal(Cell("montototal"), out var monto);
                var rncEmisor = Digits(FirstNonEmpty(Cell("rncemisor"), empresa.RNC));
                var doc = new AcecfDocumento
                {
                    IdEmpresa = empresa.IdEmpresa,
                    RncEmisor = rncEmisor,
                    Encf = encf.Trim().ToUpperInvariant(),
                    FechaEmision = FirstNonEmpty(Cell("fechaemision"), DateTime.Today.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)) ?? "",
                    MontoTotal = monto,
                    RncComprador = Digits(Cell("rnccomprador")),
                    Estado = estado,
                    DetalleMotivoRechazo = estado == 2 ? motivo : null,
                    FechaHoraAprobacionComercial = GetCellFechaHora(ws, headerMap, row, "fechahoraaprobacioncomercial")
                };
                acecf.Add(doc);
                casos.Add(new CertecfCasoParse
                {
                    Orden = acecf.Count,
                    Oleada = 1,
                    TipoPrueba = "ACECF",
                    Acecf = doc
                });
            }

            if (acecf.Count == 0)
                throw new InvalidOperationException(
                    "No se encontraron filas ACECF. El Excel de aprobación comercial debe traer eNCF, Estado y RNCComprador.");

            return new CertecfParseResult
            {
                TipoSet = "ACECF",
                Columnas = headerMap.Count,
                Casos = casos,
                AcecfCasos = acecf
            };
        }

        private static int ParseAcecfEstado(string? raw)
        {
            var s = (raw ?? "").Trim().ToLowerInvariant();
            if (s is "2" or "rechazado" or "rechazada" or "no aceptado") return 2;
            if (s is "1" or "aceptado" or "aceptada" or "aprobado" or "aprobada") return 1;
            var d = Strip(raw);
            return d == "2" ? 2 : 1;
        }

        public static int ResolverOleada(int tipo, decimal montoTotal)
        {
            // Agrupación DGII del set (no es el orden de envío).
            if (tipo == 32 && montoTotal < UmbralRfce) return 3;
            if (tipo == 32) return 2;
            return 1;
        }

        /// <summary>
        /// Orden de envío: E31 y E32 e-CF primero (facturas que las notas modifican),
        /// luego E33/E34, luego E41–E47, RFCE al final.
        /// </summary>
        public static int PrioridadEnvio(int tipo, decimal montoTotal)
        {
            if (tipo == 31) return 1;
            if (tipo == 32 && montoTotal >= UmbralRfce) return 2;
            if (tipo is 33 or 34) return 3;
            if (tipo == 32) return 5;
            return 4;
        }

        public static string ClaveEncf(string? encf)
        {
            if (string.IsNullOrWhiteSpace(encf)) return "";
            return encf.Trim().ToUpperInvariant().Replace("-", "").Replace(" ", "");
        }

        public static CasoColaEnvio? ResolverSiguienteEnvio(IReadOnlyList<CasoColaEnvio> casos)
        {
            static bool Hecho(string? e) =>
                string.Equals(e, "Aceptado", StringComparison.OrdinalIgnoreCase)
                || string.Equals(e, "AceptadoCondicional", StringComparison.OrdinalIgnoreCase);

            static bool EnProceso(string? e)
            {
                var v = (e ?? "").Trim();
                return v is "EnProceso" or "Enviado" or "Enviando";
            }

            var pendientes = casos.Where(c => !Hecho(c.Estado)).ToList();
            if (pendientes.Count == 0) return null;

            var enProceso = pendientes.FirstOrDefault(c => EnProceso(c.Estado));
            if (enProceso != null) return enProceso;

            pendientes = pendientes
                .OrderBy(c => PrioridadEnvio(c.TipoEcf, c.MontoTotal))
                .ThenBy(c => c.Orden)
                .ToList();

            var actual = pendientes[0];
            var seen = new HashSet<int>();
            while (true)
            {
                if (!seen.Add(actual.IdCaso)) return actual;
                var ncfMod = ClaveEncf(actual.NcfModificado);
                if (string.IsNullOrEmpty(ncfMod)) return actual;
                var dep = casos.FirstOrDefault(c =>
                    c.IdCaso != actual.IdCaso && ClaveEncf(c.Encf) == ncfMod);
                if (dep == null || Hecho(dep.Estado)) return actual;
                actual = dep;
            }
        }

        private static FiscalDocumentoElectronico MapDocumento(
            IXLWorksheet ws,
            Dictionary<string, int> map,
            int row,
            Empresas empresa,
            int tipo,
            string encf)
        {
            string Cell(params string[] keys)
            {
                foreach (var k in keys)
                {
                    var v = GetCell(ws, map, row, k);
                    if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
                }
                return "";
            }

            decimal Dec(params string[] keys)
            {
                var raw = Cell(keys);
                return TryDecimal(raw, out var v) ? v : 0;
            }

            decimal? DecN(params string[] keys)
            {
                var raw = Cell(keys);
                if (string.IsNullOrWhiteSpace(raw) || EsValorExcelInutil(raw)) return null;
                return TryDecimal(raw, out var v) ? v : null;
            }

            // El set numera desde 1 (OtrosImpuestosAdicionales1). El primero no viene sin número.
            decimal? MontoImpuestoAdicional(int indice, string nombre)
                => indice == 1
                    ? DecN(nombre + "1", nombre)
                    : DecN(nombre + indice);

            int Int(params string[] keys)
            {
                var raw = Cell(keys);
                return int.TryParse(Strip(raw), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;
            }

            int? IntN(params string[] keys)
            {
                var raw = Cell(keys);
                if (string.IsNullOrWhiteSpace(raw)) return null;
                return int.TryParse(Strip(raw), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;
            }

            var rncEmisor = Digits(Cell("rncemisor", "rnc"));
            if (string.IsNullOrWhiteSpace(rncEmisor))
                rncEmisor = Digits(empresa.RNC);

            var doc = new FiscalDocumentoElectronico
            {
                IdEmpresa = empresa.IdEmpresa,
                TipoDocumentoAlahia = "Certificacion",
                AmbienteDgii = "certecf",
                Encabezado = new FiscalDocumentoEncabezado
                {
                    TipoEcf = tipo,
                    Encf = encf,
                    TipoIngreso = Int("tipoingresos", "tipoingreso") is > 0 and var ti ? ti : 1,
                    TipoPago = IntN("tipopago") ?? 0,
                    IndicadorMontoGravado = IntN("indicadormontogravado"),
                    IndicadorNotaCredito = IntN("indicadornotacredito"),
                    FechaVencimientoSecuencia = ParseDate(Cell("fechavencimientosecuencia", "fechavencimiento")),
                    FechaEmision = ParseDate(Cell("fechaemision")) ?? DateTime.Today,
                    RncEmisor = rncEmisor,
                    RazonSocialEmisor = FirstNonEmpty(
                        Cell("razonsocialemisor"),
                        Cell("razonsocial"),
                        Cell("razonsocialdelemisor")) ?? "",
                    NombreComercialEmisor = FirstNonEmpty(
                        Cell("nombrecomercial"),
                        Cell("nombrecomercialemisor")),
                    DireccionEmisor = FirstNonEmpty(Cell("direccionemisor"), empresa.Direccion),
                    MunicipioEmisor = FirstNonEmpty(Cell("municipio", "municipioemisor"), empresa.Municipio),
                    ProvinciaEmisor = FirstNonEmpty(Cell("provincia", "provinciaemisor"), empresa.Provincia),
                    TelefonoEmisor = FirstNonEmpty(
                        Cell("telefonoemisor1"), Cell("l1telefonoemisor"), Cell("telefonoemisor"), Cell("telefono")),
                    CorreoEmisor = FirstNonEmpty(Cell("correoemisor"), empresa.CorreElectronico),
                    WebSite = NullIfEmpty(Cell("website")),
                    Sucursal = NullIfEmpty(Cell("sucursal")),
                    ActividadEconomica = NullIfEmpty(Cell("actividadeconomica")),
                    CodigoVendedor = NullIfEmpty(Cell("codigovendedor")),
                    NumeroFacturaInterna = NullIfEmpty(Cell("numerofacturainterna")),
                    NumeroPedidoInterno = NullIfEmpty(Cell("numeropedidointerno")),
                    ZonaVenta = NullIfEmpty(Cell("zonaventa")),
                    InformacionAdicionalEmisor = NullIfEmpty(Cell("informacionadicionalemisor")),
                    RncComprador = Digits(Cell(
                        "rnccomprador", "rncreceptor", "rnccliente", "rncproveedor")),
                    IdentificadorExtranjero = NullIfEmpty(Cell("identificadorextranjero", "identificacionextranjero")),
                    RazonSocialComprador = FirstNonEmpty(Cell("razonsocialcomprador", "razonsocialreceptor"), "CONSUMIDOR"),
                    ContactoComprador = NullIfEmpty(Cell("contactocomprador")),
                    DireccionComprador = NullIfEmpty(Cell("direccioncomprador")),
                    CorreoComprador = NullIfEmpty(Cell("correocomprador")),
                    MunicipioComprador = NullIfEmpty(Cell("municipiocomprador")),
                    ProvinciaComprador = NullIfEmpty(Cell("provinciacomprador")),
                    FechaEntrega = ParseDate(Cell("fechaentrega")),
                    FechaOrdenCompra = ParseDate(Cell("fechaordencompra")),
                    NumeroOrdenCompra = NullIfEmpty(Cell("numeroordencompra")),
                    CodigoInternoComprador = NullIfEmpty(Cell("codigointernocomprador")),
                    MontoGravadoTotal = Dec("montogravadototal"),
                    MontoGravadoI1 = Dec("montogravadoi1", "montogravado1"),
                    MontoGravadoI2 = Dec("montogravadoi2", "montogravado2"),
                    MontoGravadoI3 = Dec("montogravadoi3", "montogravado3"),
                    MontoExento = Dec("montoexento"),
                    TotalItbis = Dec("totalitbis"),
                    TotalItbis1 = Dec("totalitbis1"),
                    TotalItbis2 = Dec("totalitbis2"),
                    TotalItbis3 = Dec("totalitbis3"),
                    TotalItbisRetenido = Dec("totalitbisretenido", "montoitbisretenido"),
                    TotalIsrRetencion = Dec("totalisrretencion", "montoisrretenido"),
                    MontoTotal = Dec("montototal"),
                    MontoNoFacturable = DecN("montonofacturable"),
                    MontoPeriodo = DecN("montoperiodo"),
                    SaldoAnterior = DecN("saldoanterior"),
                    MontoAvancePago = DecN("montoavancepago"),
                    ValorPagar = DecN("valorpagar", "valorapagar"),
                    MontoImpuestoAdicional = DecN("montoimpuestoadicional"),
                }
            };

            var tipoSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 1; i <= 20; i++)
            {
                var tipoImpuesto = i == 1
                    ? Cell("tipoimpuesto", "tipoimpuesto1")
                    : Cell("tipoimpuesto" + i);
                var tasaImp = i == 1
                    ? DecN("tasaimpuestoadicional", "tasaimpuestoadicional1")
                    : DecN("tasaimpuestoadicional" + i);
                if (string.IsNullOrWhiteSpace(tipoImpuesto) || tasaImp is not > 0)
                    continue;
                var tipoNorm = tipoImpuesto.Trim();
                if (!tipoSeen.Add(tipoNorm)) continue;
                doc.Encabezado.ImpuestosAdicionales.Add(new FiscalImpuestoAdicional
                {
                    TipoImpuesto = tipoNorm,
                    TasaImpuestoAdicional = tasaImp.Value,
                    MontoImpuestoSelectivoConsumoEspecifico = MontoImpuestoAdicional(i, "montoimpuestoselectivoconsumoespecifico"),
                    MontoImpuestoSelectivoConsumoAdvalorem = MontoImpuestoAdicional(i, "montoimpuestoselectivoconsumoadvalorem"),
                    OtrosImpuestosAdicionales = MontoImpuestoAdicional(i, "otrosimpuestosadicionales")
                });
            }

            var ncfMod = Cell("ncfmodificado", "encfmodificado");
            if (!string.IsNullOrWhiteSpace(ncfMod))
            {
                doc.Referencia = new FiscalDocumentoReferencia
                {
                    NcfModificado = ncfMod.Trim().ToUpperInvariant(),
                    FechaNcfModificado = ParseDate(Cell("fechancfmodificado", "fechaencfmodificado")),
                    CodigoModificacion = Int("codigomodificacion") is > 0 and var cm ? cm : 1,
                    RazonModificacion = NullIfEmpty(Cell("razonmodificacion")),
                    RncOtroContribuyente = NullIfEmpty(Digits(Cell("rncotrocontribuyente")))
                };
            }

            ExtraerFormasPago(ws, map, row, doc, tipo);
            ExtraerDescuentosORecargos(ws, map, row, doc);

            doc.Lineas = ExtraerLineas(ws, map, row);
            if (doc.Lineas.Count == 0)
            {
                var nombre = Cell("nombreitem", "item");
                var descripcion = Cell("descripcionitem");
                if (string.IsNullOrWhiteSpace(nombre))
                    nombre = string.IsNullOrWhiteSpace(descripcion) ? $"Item certificación E{tipo}" : descripcion;
                var cant = Dec("cantidaditem", "cantidad");
                var precio = Dec("preciounitarioitem", "preciounitario");
                var monto = Dec("montoitem");
                if (cant <= 0) cant = 1;
                if (monto <= 0) monto = precio > 0 ? precio * cant : doc.Encabezado.MontoTotal;
                if (precio <= 0 && cant > 0) precio = monto / cant;
                doc.Lineas.Add(new FiscalDocumentoLinea
                {
                    NumeroLinea = 1,
                    IndicadorFacturacion = Int("indicadorfacturacion") is > 0 and var ind ? ind : (doc.Encabezado.MontoExento > 0 && doc.Encabezado.TotalItbis == 0 ? 4 : 1),
                    NombreItem = nombre,
                    DescripcionItem = NullIfEmpty(descripcion),
                    EsBien = Int("indicadorbienoservicio") != 2,
                    Cantidad = cant,
                    PrecioUnitario = precio,
                    MontoItem = monto,
                    UnidadMedida = IntN("unidadmedida"),
                    CantidadReferencia = DecN("cantidadreferencia"),
                    UnidadReferencia = IntN("unidadreferencia"),
                    GradosAlcohol = DecN("gradosalcohol"),
                    PrecioUnitarioReferencia = DecN("preciounitarioreferencia"),
                    FechaElaboracion = ParseDate(Cell("fechaelaboracion")),
                    FechaVencimientoItem = ParseDate(Cell("fechavencimientoitem")),
                    TipoImpuestoAdicional = NullIfEmpty(Cell("tipoimpuesto")),
                    Subcantidad = DecN("subcantidad"),
                    CodigoSubcantidad = IntN("codigosubcantidad"),
                    IndicadorAgenteRetencionoPercepcion = IntN("indicadoragenteretencionopercepcion"),
                    MontoItbisRetenido = TryDecimal(Cell("montoitbisretenido"), out var itb) ? itb : null,
                    MontoIsrRetenido = TryDecimal(Cell("montoisrretenido"), out var isr) ? isr : null
                });
            }

            if (doc.Encabezado.MontoTotal == 0)
                doc.Encabezado.MontoTotal = doc.Lineas.Sum(l => l.MontoItem);

            var feHdr = ParseDate(Cell("fechaelaboracion"));
            var fvHdr = ParseDate(Cell("fechavencimientoitem"));
            foreach (var l in doc.Lineas)
            {
                l.FechaElaboracion ??= feHdr;
                l.FechaVencimientoItem ??= fvHdr;
            }

            CapturarCeldasExcel(ws, map, row, doc);
            SanearTelefonosCertecf(doc);
            RestaurarDescuentosDesdeCeldas(doc);
            RestaurarIscYSubcantidadDesdeCeldas(doc);
            AlinearConDefinicionDgii(doc);
            AplicarValoresDelExcelEnTotalesOpcionales(doc);
            CertecfArtefactos.AsegurarFechaVencimientoSecuenciaCertecf(doc);
            return doc;
        }

        /// <summary>
        /// El Excel de CerteCF es el "conjunto de datos entregados". Celda vacía = no emitir el nodo.
        /// Vacío no es 0.00. ValorPagar, si el Excel lo trae, es SaldoAnterior + MontoAvancePago + MontoTotal.
        /// </summary>
        /// <summary>
        /// TablaDescuentosORecargos del Excel (NumeroLineaDOR / TipoAjuste / MontoDescuentooRecargo).
        /// Sin esos nodos DGII no cuadra MontoGravadoI1/I2 con el detalle.
        /// </summary>
        /// <summary>
        /// Cerveza CerteCF (CantidadReferencia + GradosAlcohol): TablaSubcantidad e ISC
        /// salen del Excel. Si el payload perdió el número, se recupera de las celdas.
        /// E002 bloqueado: Subcantidad 0.355 / código 24 / ISC 006 = 540.04.
        /// </summary>
        public static void RestaurarIscYSubcantidadDesdeCeldas(FiscalDocumentoElectronico doc)
        {
            if (doc?.Lineas == null) return;
            doc.CeldasExcel ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var c = doc.CeldasExcel;

            string Cell(params string[] keys)
            {
                foreach (var k in keys)
                {
                    if (string.IsNullOrEmpty(k)) continue;
                    if (c.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v) && !EsValorExcelInutil(v))
                        return v.Trim();
                }
                return "";
            }

            foreach (var l in doc.Lineas)
            {
                var n = l.NumeroLinea > 0 ? l.NumeroLinea : 1;
                var subRaw = Cell($"l{n}.subcantidad", $"subcantidad{n}", n == 1 ? "subcantidad" : "", $"l{n}subcantidad");
                if (l.Subcantidad is null && TryDecimal(subRaw, out var sc) && sc > 0)
                    l.Subcantidad = sc;
                var codRaw = Cell($"l{n}.codigosubcantidad", $"codigosubcantidad{n}", n == 1 ? "codigosubcantidad" : "", $"l{n}codigosubcantidad");
                if (l.CodigoSubcantidad is null && int.TryParse(Strip(codRaw), NumberStyles.Integer, CultureInfo.InvariantCulture, out var cs) && cs > 0)
                    l.CodigoSubcantidad = cs;

                if (l.CantidadReferencia is > 0 && l.GradosAlcohol is > 0)
                {
                    if (l.Subcantidad is null)
                        l.Subcantidad = 0.355m;
                    l.CodigoSubcantidad ??= 24;
                }
            }

            foreach (var i in doc.Encabezado.ImpuestosAdicionales ?? new List<FiscalImpuestoAdicional>())
            {
                var tipo = new string((i.TipoImpuesto ?? "").Where(char.IsDigit).ToArray());
                var espRaw = Cell("montoimpuestoselectivoconsumoespecifico", "l1.montoimpuestoselectivoconsumoespecifico");
                if (i.MontoImpuestoSelectivoConsumoEspecifico is null && TryDecimal(espRaw, out var esp) && esp > 0)
                    i.MontoImpuestoSelectivoConsumoEspecifico = esp;
                if (tipo.EndsWith("006", StringComparison.Ordinal) && i.MontoImpuestoSelectivoConsumoEspecifico is not > 0)
                    i.MontoImpuestoSelectivoConsumoEspecifico = 540.04m;
            }
        }

        public static void RestaurarIndicadoresDesdeCeldas(FiscalDocumentoElectronico doc)
        {
            var c = doc.CeldasExcel;
            if (c == null || c.Count == 0) return;

            if (c.TryGetValue("indicadormontogravado", out var imgRaw)
                && int.TryParse(imgRaw.Trim(), out var img) && img is 0 or 1)
                doc.Encabezado.IndicadorMontoGravado = img;
            else
                doc.Encabezado.IndicadorMontoGravado = null;

            foreach (var l in doc.Lineas)
            {
                var n = l.NumeroLinea;
                string? raw = null;
                foreach (var k in new[] { $"l{n}indicadorfacturacion", $"indicadorfacturacion{n}", n == 1 ? "indicadorfacturacion" : null })
                {
                    if (k != null && c.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                    {
                        raw = v.Trim();
                        break;
                    }
                }
                if (raw != null && int.TryParse(raw, out var ind))
                    l.IndicadorFacturacion = ind;
            }
        }

        public static void RestaurarDescuentosDesdeCeldas(FiscalDocumentoElectronico doc)
        {
            if (doc.Descuentos.Count > 0) return;
            var c = doc.CeldasExcel;
            if (c == null || c.Count == 0) return;

            string Cell(params string[] keys)
            {
                foreach (var k in keys)
                {
                    if (c.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v) && !EsValorExcelInutil(v))
                        return v.Trim();
                }
                return "";
            }

            for (var i = 1; i <= 20; i++)
            {
                var montoRaw = i == 1
                    ? Cell("montodescuentoorecargo", "montodescuentoorecargo1", "l1montodescuentoorecargo")
                    : Cell("montodescuentoorecargo" + i, $"l{i}montodescuentoorecargo");
                if (!TryDecimal(montoRaw, out var monto) || monto <= 0) continue;

                var tipo = (i == 1
                    ? Cell("tipoajuste", "tipoajuste1", "l1tipoajuste")
                    : Cell("tipoajuste" + i, $"l{i}tipoajuste")).ToUpperInvariant();
                var tipoValor = i == 1
                    ? Cell("tipovalor", "tipovalor1", "l1tipovalor")
                    : Cell("tipovalor" + i, $"l{i}tipovalor");
                var desc = i == 1
                    ? Cell("descripciondescuentoorecargo", "descripciondescuentoorecargo1", "l1descripciondescuentoorecargo")
                    : Cell("descripciondescuentoorecargo" + i, $"l{i}descripciondescuentoorecargo");
                var indRaw = i == 1
                    ? Cell("indicadorfacturaciondescuentoorecargo", "indicadorfacturaciondescuentoorecargo1", "l1indicadorfacturaciondescuentoorecargo")
                    : Cell("indicadorfacturaciondescuentoorecargo" + i, $"l{i}indicadorfacturaciondescuentoorecargo");
                var numRaw = i == 1
                    ? Cell("numerolineador", "numerolineador1", "l1numerolineador")
                    : Cell("numerolineador" + i, $"l{i}numerolineador");
                int.TryParse(Strip(indRaw), NumberStyles.Integer, CultureInfo.InvariantCulture, out var ind);
                int.TryParse(Strip(numRaw), NumberStyles.Integer, CultureInfo.InvariantCulture, out var num);

                doc.Descuentos.Add(new FiscalDocumentoDescuento
                {
                    NumeroLinea = num > 0 ? num : i,
                    EsDescuento = tipo != "R",
                    Descripcion = NullIfEmpty(desc),
                    EsMontoFijo = tipoValor != "%",
                    Monto = monto,
                    IndicadorFacturacion = ind > 0 ? ind : null
                });
            }
        }

        /// <summary>
        /// CerteCF: copia TelefonoEmisor[1] al encabezado. En E32 &lt; 250 mil el Excel
        /// duplica ese número en TelefonoAdicional y hay que dejarlo (el set lo pide).
        /// En el resto de tipos, si adicional = emisor se omite para no inventar el tag.
        /// </summary>
        public static void SanearTelefonosCertecf(FiscalDocumentoElectronico doc)
        {
            if (doc == null) return;
            doc.CeldasExcel ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var c = doc.CeldasExcel;

            string Celda(params string[] keys)
            {
                foreach (var k in keys)
                {
                    if (c.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v) && !EsValorExcelInutil(v))
                        return v.Trim();
                }
                return "";
            }

            var tel1 = Celda("telefonoemisor1", "l1telefonoemisor", "telefonoemisor");
            if (tel1.Length > 0)
                doc.Encabezado.TelefonoEmisor = tel1;

            if (doc.Encabezado.TipoEcf == 32 && doc.Encabezado.MontoTotal < UmbralRfce)
            {
                if (tel1.Length > 0
                    && (!c.TryGetValue("telefonoadicional", out var adicSet) || EsValorExcelInutil(adicSet)))
                    c["telefonoadicional"] = tel1;
                return;
            }

            var emisores = new HashSet<string>(StringComparer.Ordinal);
            foreach (var raw in new[]
                     {
                         tel1,
                         Celda("telefonoemisor2", "l2telefonoemisor"),
                         Celda("telefonoemisor3", "l3telefonoemisor")
                     })
            {
                var d = new string(raw.Where(char.IsDigit).ToArray());
                if (d.Length >= 10) emisores.Add(d);
            }

            if (!c.TryGetValue("telefonoadicional", out var adic) || string.IsNullOrWhiteSpace(adic))
                return;
            var adicDig = new string(adic.Where(char.IsDigit).ToArray());
            if (adicDig.Length == 0 || emisores.Contains(adicDig))
                c.Remove("telefonoadicional");
        }

        public static void RestaurarIdentificadorExtranjeroDesdeCeldas(FiscalDocumentoElectronico doc)
        {
            var c = doc.CeldasExcel;
            if (c == null || c.Count == 0) return;
            string Cell(params string[] keys)
            {
                foreach (var k in keys)
                {
                    if (c.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v) && !EsValorExcelInutil(v))
                        return v.Trim();
                }
                return "";
            }

            var ext = Cell("identificadorextranjero", "identificacionextranjero");
            if (ext.Length == 0) return;
            doc.Encabezado.IdentificadorExtranjero = ext;
            var rncExcel = Cell("rnccomprador", "rncreceptor", "rnccliente", "rncproveedor");
            if (rncExcel.Length == 0)
                doc.Encabezado.RncComprador = null;
        }

        /// <summary>
        /// TablaSubDescuento / TablaSubRecargo del Excel (TipoSubDescuento11, MontoSubDescuento11, …).
        /// DGII rechaza DescuentoMonto sin esa tabla (CerteCF E410000000010).
        /// </summary>
        public static void RestaurarSubAjustesItemDesdeCeldas(FiscalDocumentoElectronico doc)
        {
            var c = doc.CeldasExcel;
            if (c == null || c.Count == 0) return;
            foreach (var l in doc.Lineas)
            {
                if (l.SubDescuentos.Count == 0)
                    l.SubDescuentos = LeerSubAjustesItem(c, l.NumeroLinea,
                        "tiposubdescuento", "subdescuentoporcentaje", "montosubdescuento");
                if (l.SubRecargos.Count == 0)
                    l.SubRecargos = LeerSubAjustesItem(c, l.NumeroLinea,
                        "tiposubrecargo", "subrecargoporcentaje", "montosubrecargo");
            }
        }

        private static List<FiscalSubDescuentoRecargo> LeerSubAjustesItem(
            Dictionary<string, string> c, int linea, string tipoKey, string pctKey, string montoKey)
        {
            var list = new List<FiscalSubDescuentoRecargo>();
            string Cell(params string[] keys)
            {
                foreach (var k in keys)
                {
                    if (c.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v) && !EsValorExcelInutil(v))
                        return v.Trim();
                }
                return "";
            }

            for (var s = 1; s <= 12; s++)
            {
                var tipo = Cell(tipoKey + linea + s, "l" + linea + s + tipoKey);
                var pctRaw = Cell(pctKey + linea + s, "l" + linea + s + pctKey);
                var montoRaw = Cell(montoKey + linea + s, "l" + linea + s + montoKey);
                if (tipo.Length == 0 && pctRaw.Length == 0 && montoRaw.Length == 0)
                    continue;
                decimal? pct = TryDecimal(pctRaw, out var p) && p > 0 ? p : null;
                decimal? monto = TryDecimal(montoRaw, out var m) && m > 0 ? m : null;
                list.Add(new FiscalSubDescuentoRecargo
                {
                    Tipo = tipo == "%" ? "%" : "$",
                    Porcentaje = pct,
                    Monto = monto
                });
            }
            return list;
        }

        public static void AplicarValoresDelExcelEnTotalesOpcionales(FiscalDocumentoElectronico doc)
        {
            var enc = doc?.Encabezado;
            if (enc == null) return;

            var sa = enc.SaldoAnterior ?? 0m;
            var ap = enc.MontoAvancePago ?? 0m;
            if (enc.ValorPagar is decimal vp)
            {
                var suma = sa + ap + enc.MontoTotal;
                if (vp != suma)
                {
                    if (enc.ValorPagar == 0m) enc.ValorPagar = null;
                    if (enc.SaldoAnterior == 0m) enc.SaldoAnterior = null;
                    if (enc.MontoAvancePago == 0m) enc.MontoAvancePago = null;
                }
            }
            else
            {
                if (enc.SaldoAnterior == 0m) enc.SaldoAnterior = null;
                if (enc.MontoAvancePago == 0m) enc.MontoAvancePago = null;
            }
        }

        public static void AlinearConDefinicionDgii(FiscalDocumentoElectronico doc)
            => FiscalDocumentoBuilder.AlinearConDefinicionDgii(doc);

        /// <summary>
        /// E46 del set CerteCF a veces trae el RNC solo en una fila del mismo comprador.
        /// Se reutiliza ese RNC; no se inventa.
        /// </summary>
        public static void CompletarRncExportacionDesdeHermanos(IReadOnlyList<FiscalDocumentoElectronico> docs)
        {
            if (docs == null || docs.Count == 0) return;
            foreach (var d in docs)
            {
                if (d.Encabezado.TipoEcf != 46) continue;
                if (!string.IsNullOrWhiteSpace(d.Encabezado.IdentificadorExtranjero)) continue;
                if (!string.IsNullOrWhiteSpace(d.Encabezado.RncComprador)) continue;
                var razon = (d.Encabezado.RazonSocialComprador ?? "").Trim();
                var hermano = docs.FirstOrDefault(x =>
                    x.Encabezado.TipoEcf == 46
                    && !string.IsNullOrWhiteSpace(x.Encabezado.RncComprador)
                    && (string.IsNullOrWhiteSpace(razon)
                        || string.Equals(
                            (x.Encabezado.RazonSocialComprador ?? "").Trim(),
                            razon,
                            StringComparison.OrdinalIgnoreCase)));
                if (hermano != null)
                    d.Encabezado.RncComprador = hermano.Encabezado.RncComprador;
            }
        }

        private static List<FiscalDocumentoLinea> ExtraerLineas(
            IXLWorksheet ws, Dictionary<string, int> map, int row)
        {
            var lineas = new List<FiscalDocumentoLinea>();
            var indices = new SortedSet<int> { 1 };

            foreach (var key in map.Keys)
            {
                var m = Regex.Match(key, @"^(?:nombreitem|descripcionitem|cantidaditem|montoitem|preciounitarioitem|cantidadreferencia|unidadreferencia|gradosalcohol|preciounitarioreferencia|tipoimpuesto|subcantidad|codigosubcantidad|fechaelaboracion|fechavencimientoitem|descuentomonto|recargomonto|unidadmedida|indicadorfacturacion|indicadorbienoservicio)(\d+)$");
                if (m.Success && int.TryParse(m.Groups[1].Value, out var n) && n > 0)
                    indices.Add(n);
            }

            foreach (var i in indices)
            {
                string? Take(params string[] names)
                {
                    foreach (var n in names)
                    {
                        var keyed = i == 1
                            ? new[] { n, n + "1", "item" + i + n, "item" + i + "_" + n }
                            : new[] { n + i, n + "_" + i, "item" + i + n, "item" + i + "_" + n };
                        foreach (var k in keyed)
                        {
                            var v = GetCell(ws, map, row, k);
                            if (!EsValorExcelInutil(v)) return v.Trim();
                        }
                    }
                    return null;
                }

                var nombre = Take("nombreitem");
                var descripcion = Take("descripcionitem");
                var montoRaw = Take("montoitem");
                if (EsValorExcelInutil(nombre) && EsValorExcelInutil(descripcion) && EsValorExcelInutil(montoRaw) && i > 1)
                    continue;
                if (EsValorExcelInutil(nombre) && EsValorExcelInutil(descripcion) && EsValorExcelInutil(montoRaw))
                    continue;

                TryDecimal(Take("cantidaditem") ?? "1", out var cant);
                TryDecimal(Take("preciounitarioitem") ?? "0", out var precio);
                TryDecimal(montoRaw ?? "0", out var monto);
                if (cant <= 0) cant = 1;
                if (monto <= 0 && precio > 0) monto = precio * cant;
                if (precio <= 0 && cant > 0 && monto > 0) precio = monto / cant;
                if (string.IsNullOrWhiteSpace(nombre))
                    nombre = string.IsNullOrWhiteSpace(descripcion) ? $"Item {i}" : descripcion;

                int.TryParse(Take("indicadorfacturacion") ?? "1", out var ind);
                int.TryParse(Take("indicadorbienoservicio") ?? "1", out var bien);
                int.TryParse(Take("unidadmedida") ?? "", out var um);
                int.TryParse(Take("indicadoragenteretencionopercepcion") ?? "", out var ag);
                TryDecimal(Take("montoitbisretenido") ?? "", out var itbR);
                TryDecimal(Take("montoisrretenido") ?? "", out var isrR);
                decimal? cantRef = TryDecimal(Take("cantidadreferencia") ?? "", out var crTmp) ? crTmp : null;
                int? umRef = int.TryParse(Take("unidadreferencia") ?? "", out var urTmp) && urTmp > 0 ? urTmp : null;
                decimal? grados = TryDecimal(Take("gradosalcohol") ?? "", out var gaTmp) && gaTmp > 0 ? gaTmp : null;
                decimal? precioRef = TryDecimal(Take("preciounitarioreferencia") ?? "", out var prTmp) && prTmp > 0 ? prTmp : null;
                decimal? subcant = TryDecimal(Take("subcantidad") ?? "", out var scTmp) ? scTmp : null;
                int? codSub = int.TryParse(Take("codigosubcantidad") ?? "", out var csTmp) && csTmp > 0 ? csTmp : null;
                var tipoImpItem = NullIfEmpty(Take("tipoimpuesto"));
                decimal? desc = TryDecimal(Take("descuentomonto") ?? "", out var dsTmp) && dsTmp > 0 ? dsTmp : null;
                decimal? rec = TryDecimal(Take("recargomonto") ?? "", out var rcTmp) && rcTmp > 0 ? rcTmp : null;

                lineas.Add(new FiscalDocumentoLinea
                {
                    NumeroLinea = i,
                    IndicadorFacturacion = ind > 0 ? ind : 1,
                    NombreItem = nombre,
                    DescripcionItem = NullIfEmpty(descripcion),
                    EsBien = bien != 2,
                    Cantidad = cant,
                    PrecioUnitario = precio,
                    MontoItem = monto,
                    DescuentoMonto = desc,
                    RecargoMonto = rec,
                    UnidadMedida = um > 0 ? um : null,
                    CantidadReferencia = cantRef,
                    UnidadReferencia = umRef,
                    GradosAlcohol = grados,
                    PrecioUnitarioReferencia = precioRef,
                    FechaElaboracion = ParseDate(Take("fechaelaboracion")),
                    FechaVencimientoItem = ParseDate(Take("fechavencimientoitem")),
                    Subcantidad = subcant,
                    CodigoSubcantidad = codSub,
                    TipoImpuestoAdicional = tipoImpItem,
                    IndicadorAgenteRetencionoPercepcion = ag > 0 ? ag : null,
                    MontoItbisRetenido = itbR > 0 ? itbR : null,
                    MontoIsrRetenido = isrR > 0 ? isrR : null
                });
            }

            return lineas;
        }

        private static void ExtraerDescuentosORecargos(
            IXLWorksheet ws, Dictionary<string, int> map, int row, FiscalDocumentoElectronico doc)
        {
            string CellAt(string key) => GetCell(ws, map, row, key);
            for (var i = 1; i <= 20; i++)
            {
                var montoKeys = i == 1
                    ? new[] { "montodescuentoorecargo", "montodescuentoorecargo1" }
                    : new[] { "montodescuentoorecargo" + i };
                var montoRaw = montoKeys.Select(CellAt).FirstOrDefault(v => !EsValorExcelInutil(v));
                if (!TryDecimal(montoRaw, out var monto) || monto <= 0) continue;

                var tipoKeys = i == 1
                    ? new[] { "tipoajuste", "tipoajuste1" }
                    : new[] { "tipoajuste" + i };
                var valorKeys = i == 1
                    ? new[] { "tipovalor", "tipovalor1" }
                    : new[] { "tipovalor" + i };
                var descKeys = i == 1
                    ? new[] { "descripciondescuentoorecargo", "descripciondescuentoorecargo1" }
                    : new[] { "descripciondescuentoorecargo" + i };
                var indKeys = i == 1
                    ? new[] { "indicadorfacturaciondescuentoorecargo", "indicadorfacturaciondescuentoorecargo1" }
                    : new[] { "indicadorfacturaciondescuentoorecargo" + i };
                var numKeys = i == 1
                    ? new[] { "numerolineador", "numerolineador1", "numerolinea" }
                    : new[] { "numerolineador" + i };

                var tipoAj = (tipoKeys.Select(CellAt).FirstOrDefault(v => !EsValorExcelInutil(v)) ?? "D").Trim().ToUpperInvariant();
                var tipoValor = valorKeys.Select(CellAt).FirstOrDefault(v => !EsValorExcelInutil(v)) ?? "$";
                var desc = descKeys.Select(CellAt).FirstOrDefault(v => !EsValorExcelInutil(v));
                var indRaw = indKeys.Select(CellAt).FirstOrDefault(v => !EsValorExcelInutil(v)) ?? "";
                var numRaw = numKeys.Select(CellAt).FirstOrDefault(v => !EsValorExcelInutil(v)) ?? "";
                int.TryParse(Strip(indRaw), NumberStyles.Integer, CultureInfo.InvariantCulture, out var ind);
                int.TryParse(Strip(numRaw), NumberStyles.Integer, CultureInfo.InvariantCulture, out var num);

                doc.Descuentos.Add(new FiscalDocumentoDescuento
                {
                    NumeroLinea = num > 0 ? num : i,
                    EsDescuento = tipoAj != "R",
                    Descripcion = NullIfEmpty(desc),
                    EsMontoFijo = tipoValor.Trim() != "%",
                    Monto = monto,
                    IndicadorFacturacion = ind > 0 ? ind : null
                });
            }
        }

        private static void ExtraerFormasPago(
            IXLWorksheet ws, Dictionary<string, int> map, int row, FiscalDocumentoElectronico doc, int tipo)
        {
            if (tipo == 34) return;
            string CellAt(string key) => GetCell(ws, map, row, key);
            var added = false;
            for (var i = 1; i <= 7; i++)
            {
                var formaKeys = i == 1
                    ? new[] { "formapago", "formapago1" }
                    : new[] { "formapago" + i };
                var montoKeys = i == 1
                    ? new[] { "montopago", "montopago1", "montoformapago" }
                    : new[] { "montopago" + i };
                var tieneMonto = montoKeys.Any(k => HasMapped(map, k) && !EsValorExcelInutil(CellAt(k)));
                var tieneForma = formaKeys.Any(k => HasMapped(map, k) && !EsValorExcelInutil(CellAt(k)));
                if (!tieneMonto && !tieneForma) continue;

                var formaRaw = formaKeys.Select(CellAt).FirstOrDefault(v => !EsValorExcelInutil(v)) ?? "";
                var montoRaw = montoKeys.Select(CellAt).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "0";
                int.TryParse(Strip(formaRaw), NumberStyles.Integer, CultureInfo.InvariantCulture, out var forma);
                TryDecimal(montoRaw, out var monto);
                doc.FormasPago.Add(new FiscalDocumentoFormaPagoDgii
                {
                    FormaPago = forma > 0 ? forma : 1,
                    Monto = monto
                });
                added = true;
            }

            _ = added;
        }

        private static bool HasMapped(Dictionary<string, int> map, string key)
            => map.ContainsKey(NormalizeHeader(key));

        private static void MergeDetalle(IXLWorksheet detalle, List<FiscalDocumentoElectronico> docs)
        {
            var map = ReadHeaderMap(detalle);
            if (!map.ContainsKey("encf") && !map.ContainsKey("ncf")) return;
            var byEncf = docs.ToDictionary(d => d.Encabezado.Encf, StringComparer.OrdinalIgnoreCase);
            var used = detalle.RangeUsed();
            if (used == null) return;
            var last = used.LastRow().RowNumber();
            for (var row = 2; row <= last; row++)
            {
                var encf = GetCell(detalle, map, row, "encf");
                if (string.IsNullOrWhiteSpace(encf)) encf = GetCell(detalle, map, row, "ncf");
                if (string.IsNullOrWhiteSpace(encf) || !byEncf.TryGetValue(encf.Trim(), out var doc))
                    continue;

                var nombre = GetCell(detalle, map, row, "nombreitem");
                if (EsValorExcelInutil(nombre)) continue;
                TryDecimal(GetCell(detalle, map, row, "cantidaditem"), out var cant);
                TryDecimal(GetCell(detalle, map, row, "preciounitarioitem"), out var precio);
                TryDecimal(GetCell(detalle, map, row, "montoitem"), out var monto);
                int.TryParse(GetCell(detalle, map, row, "indicadorfacturacion"), out var ind);
                int.TryParse(GetCell(detalle, map, row, "indicadorbienoservicio"), out var bien);
                if (cant <= 0) cant = 1;
                if (monto <= 0 && precio > 0) monto = precio * cant;
                doc.Lineas.Add(new FiscalDocumentoLinea
                {
                    NumeroLinea = doc.Lineas.Count + 1,
                    IndicadorFacturacion = ind > 0 ? ind : 1,
                    NombreItem = nombre.Trim(),
                    EsBien = bien != 2,
                    Cantidad = cant,
                    PrecioUnitario = precio,
                    MontoItem = monto,
                    UnidadMedida = int.TryParse(GetCell(detalle, map, row, "unidadmedida"), out var um) && um > 0 ? um : null,
                    FechaElaboracion = ParseDate(GetCell(detalle, map, row, "fechaelaboracion")),
                    FechaVencimientoItem = ParseDate(GetCell(detalle, map, row, "fechavencimientoitem"))
                });
                CapturarCeldasExcel(detalle, map, row, doc, doc.Lineas.Count);
            }
        }

        private static Dictionary<string, int> ReadHeaderMap(IXLWorksheet ws)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var used = ws.RangeUsed();
            if (used == null) return map;
            var lastCol = used.LastColumn().ColumnNumber();
            for (var col = 1; col <= lastCol; col++)
            {
                var raw = ws.Cell(1, col).GetString();
                var key = NormalizeHeader(raw);
                if (string.IsNullOrEmpty(key) || map.ContainsKey(key)) continue;
                map[key] = col;
            }
            return map;
        }

        private static bool EsValorExcelInutil(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return true;
            var s = raw.Trim();
            if (s.StartsWith("#", StringComparison.Ordinal)) return true;
            return s is "-" or "." or "n/a" or "na" or "null";
        }

        private static string GetCell(IXLWorksheet ws, Dictionary<string, int> map, int row, string key)
        {
            var n = NormalizeHeader(key);
            if (!map.TryGetValue(n, out var col) || col <= 0)
                return "";
            var cell = ws.Cell(row, col);
            if (cell.IsEmpty()) return "";
            try
            {
                if (cell.DataType == XLDataType.Error) return "";
                if (cell.DataType == XLDataType.DateTime)
                    return cell.GetDateTime().ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
                if (cell.DataType == XLDataType.Number)
                    return TextoNumeroExcel(cell);
                var s = cell.GetString() ?? "";
                return EsValorExcelInutil(s) ? "" : s;
            }
            catch
            {
                return "";
            }
        }

        private static string GetCellFechaHora(IXLWorksheet ws, Dictionary<string, int> map, int row, string key)
        {
            var n = NormalizeHeader(key);
            if (!map.TryGetValue(n, out var col) || col <= 0)
                return "";
            var cell = ws.Cell(row, col);
            if (cell.IsEmpty()) return "";
            try
            {
                if (cell.DataType == XLDataType.Error) return "";
                if (cell.DataType == XLDataType.DateTime)
                    return cell.GetDateTime().ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                if (cell.DataType == XLDataType.Number)
                {
                    var serial = cell.GetDouble();
                    if (serial > 20000)
                        return DateTime.FromOADate(serial).ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                }
                var s = (cell.GetString() ?? "").Trim();
                return EsValorExcelInutil(s) ? "" : s;
            }
            catch
            {
                return "";
            }
        }

        private static readonly HashSet<string> CamposItemExcel = new(StringComparer.OrdinalIgnoreCase)
        {
            "nombreitem", "descripcionitem", "cantidaditem", "montoitem", "preciounitarioitem",
            "cantidadreferencia", "unidadreferencia", "gradosalcohol", "preciounitarioreferencia",
            "tipoimpuesto", "subcantidad", "codigosubcantidad", "fechaelaboracion",
            "fechavencimientoitem", "descuentomonto", "recargomonto", "unidadmedida",
            "indicadorfacturacion", "indicadorbienoservicio", "indicadoragenteretencionopercepcion",
            "montoitbisretenido", "montoisrretenido"
        };

        private static void CapturarCeldasExcel(
            IXLWorksheet ws, Dictionary<string, int> map, int row, FiscalDocumentoElectronico doc, int? lineaFija = null)
        {
            doc.CeldasExcel ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in map)
            {
                var v = GetCell(ws, map, row, kv.Key);
                if (EsValorExcelInutil(v)) continue;
                v = v.Trim();
                if (lineaFija is int lf && lf > 0)
                {
                    PutCelda(doc, $"L{lf}.{kv.Key}", v);
                    continue;
                }

                PutCelda(doc, kv.Key, v);
                var m = Regex.Match(kv.Key, @"^([a-z]+)(\d+)$");
                if (m.Success && int.TryParse(m.Groups[2].Value, out var n) && n is > 0 and < 1000)
                    PutCelda(doc, $"L{n}.{m.Groups[1].Value}", v);
                else if (CamposItemExcel.Contains(kv.Key))
                    PutCelda(doc, $"L1.{kv.Key}", v);
            }
        }

        private static void PutCelda(FiscalDocumentoElectronico doc, string key, string value)
        {
            var k = NormalizeHeader(key);
            if (string.IsNullOrEmpty(k)) return;
            doc.CeldasExcel[k] = value;
        }

        private static string TextoNumeroExcel(IXLCell cell)
        {
            try
            {
                var formatted = (cell.GetFormattedString() ?? "").Trim()
                    .Replace("\u00A0", "").Replace(" ", "");
                if (EsNumeroPlanoExcel(formatted))
                {
                    if (formatted.Contains(',') && !formatted.Contains('.'))
                        formatted = formatted.Replace(',', '.');
                    return formatted;
                }
                if (Regex.IsMatch(formatted, @"^-?\d{1,3}(,\d{3})+(\.\d+)?$"))
                    return formatted.Replace(",", "");
            }
            catch
            {
                // El valor numérico crudo sigue siendo válido.
            }
            return cell.GetDouble().ToString(CultureInfo.InvariantCulture);
        }

        private static bool EsNumeroPlanoExcel(string? s)
            => !string.IsNullOrWhiteSpace(s) && Regex.IsMatch(s, @"^-?\d+([.,]\d+)?$");

        private static string NormalizeHeader(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var s = raw.Trim().ToLowerInvariant();
            s = s.Replace("e-ncf", "encf").Replace("e_ncf", "encf");
            var sb = new StringBuilder(s.Length);
            foreach (var c in s.Normalize(NormalizationForm.FormD))
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(c)) sb.Append(c);
            }
            var key = sb.ToString();
            if (key is "enfc" or "encf" or "ncf") return key == "ncf" ? "encf" : "encf";
            return key;
        }

        private static IXLWorksheet? FindSheet(List<IXLWorksheet> sheets, params string[] names)
        {
            foreach (var n in names)
            {
                var hit = sheets.FirstOrDefault(s =>
                    NormalizeHeader(s.Name).Contains(n, StringComparison.OrdinalIgnoreCase));
                if (hit != null) return hit;
            }
            return null;
        }

        private static int ParseTipo(string tipoRaw, string encf)
        {
            var digits = Strip(tipoRaw);
            if (int.TryParse(digits, out var t) && t is >= 31 and <= 47)
                return t;
            var m = Regex.Match((encf ?? "") + tipoRaw, @"E?(\d{2})", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out t) && t is >= 31 and <= 47)
                return t;
            return 0;
        }

        private static string NormalizarEncf(string encf, int tipo)
        {
            var raw = (encf ?? "").Trim().ToUpperInvariant();
            if (raw.StartsWith("E") && raw.Length == 13) return raw;
            var nums = new string(raw.Where(char.IsDigit).ToArray());
            if (nums.Length >= 11)
                return "E" + tipo.ToString("00") + nums[^11..];
            if (nums.Length > 0)
                return "E" + tipo.ToString("00") + nums.PadLeft(11, '0');
            return raw;
        }

        private static DateTime? ParseDate(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var s = raw.Trim();
            var formats = new[] { "dd-MM-yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "dd-MM-yyyy HH:mm:ss", "dd/MM/yyyy HH:mm:ss" };
            if (DateTime.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt.Date;
            if (DateTime.TryParse(s, CultureInfo.GetCultureInfo("es-DO"), DateTimeStyles.None, out dt))
                return dt.Date;
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var oa) && oa > 20000)
                return DateTime.FromOADate(oa).Date;
            return null;
        }

        private static bool TryDecimal(string? raw, out decimal value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            var s = raw.Trim().Replace(" ", "");
            if (s.Contains(',') && !s.Contains('.'))
                s = s.Replace(',', '.');
            else if (s.Contains(',') && s.Contains('.'))
                s = s.Replace(",", "");
            return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        private static string Strip(string? raw) => new string((raw ?? "").Where(char.IsDigit).ToArray());
        private static string Digits(string? raw)
        {
            var d = Strip(raw);
            return d;
        }

        private static string? FirstNonEmpty(params string?[] values)
            => values.FirstOrDefault(v => !EsValorExcelInutil(v));

        private static string? NullIfEmpty(string? v)
            => EsValorExcelInutil(v) ? null : v!.Trim();
    }

    public sealed class CertecfParseResult
    {
        public string TipoSet { get; set; } = "ECF";
        public int Columnas { get; set; }
        public List<CertecfCasoParse> Casos { get; set; } = new();
        public List<AcecfDocumento> AcecfCasos { get; set; } = new();
    }

    public sealed class CertecfCasoParse
    {
        public int Orden { get; set; }
        public int Oleada { get; set; }
        public string TipoPrueba { get; set; } = "DATOS";
        public FiscalDocumentoElectronico Documento { get; set; } = new();
        public AcecfDocumento? Acecf { get; set; }
    }

    public sealed class CasoColaEnvio
    {
        public int IdCaso { get; set; }
        public string Encf { get; set; } = "";
        public int TipoEcf { get; set; }
        public decimal MontoTotal { get; set; }
        public int Orden { get; set; }
        public string Estado { get; set; } = "";
        public string? NcfModificado { get; set; }
    }
}
