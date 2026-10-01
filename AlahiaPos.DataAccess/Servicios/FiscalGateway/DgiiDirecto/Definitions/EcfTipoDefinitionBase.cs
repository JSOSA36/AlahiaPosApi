using System.Xml.Linq;
using AlahiaPos.Entities.Dto.Fiscal;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto.Definitions
{
    /// <summary>Campos Emisor/Comprador/Totales/Item compartidos (orden XSD común).</summary>
    public abstract class EcfTipoDefinitionBase : IEcfTipoDefinition
    {
        public abstract int TipoeCF { get; }
        public abstract string Codigo { get; }
        public abstract string Nombre { get; }
        public abstract string XsdArchivo { get; }
        public abstract IReadOnlyList<string> ConocimientoAcumulado { get; }
        public abstract IReadOnlyList<EcfCampoDef> IdDoc { get; }
        public abstract IReadOnlyList<EcfCampoDef>? InformacionReferencia { get; }
        public abstract bool RequiereInformacionReferencia { get; }
        public virtual bool PermiteDescuentosORecargos => true;

        public virtual IReadOnlyList<EcfCampoDef> Emisor { get; } = BuildEmisorComun();
        public virtual IReadOnlyList<EcfCampoDef>? Comprador { get; } = BuildCompradorComun(forzarRncVacio: true);
        public virtual IReadOnlyList<EcfCampoDef> Totales { get; } = BuildTotalesComun();
        public virtual IReadOnlyList<EcfCampoDef> Item { get; } = BuildItemComun();

        public virtual void Validar(EcfBuildContext ctx)
        {
            if (string.IsNullOrWhiteSpace(ctx.Enc.Encf) || ctx.Enc.Encf.Trim().Length != 13)
                throw new InvalidOperationException($"{Codigo}: eNCF debe tener 13 caracteres.");
            if (ctx.Documento.Lineas.Count == 0)
                throw new InvalidOperationException($"{Codigo}: DetallesItems requiere al menos 1 Item.");
            if (RequiereInformacionReferencia &&
                (ctx.Documento.Referencia == null || string.IsNullOrWhiteSpace(ctx.Documento.Referencia.NcfModificado)))
                throw new InvalidOperationException($"{Codigo}: InformacionReferencia.NCFModificado es obligatorio.");

            EcfRncRules.ExigirRncValido(ctx.Enc.RncEmisor, Codigo, "RNCEmisor");

            if (RequiereInformacionReferencia && ctx.Documento.Referencia != null)
                ValidarCodigoModificacion(ctx);

            ValidarPresenciaCampos(ctx, "IdDoc", IdDoc);
            ValidarPresenciaCampos(ctx, "Emisor", Emisor);
            if (Comprador != null)
                ValidarPresenciaCampos(ctx, "Comprador", Comprador);
            ValidarPresenciaCampos(ctx, "Totales", Totales);
            if (InformacionReferencia != null)
                ValidarPresenciaCampos(ctx, "InformacionReferencia", InformacionReferencia);

            ValidarIndicadorMontoGravadoSiAplica(ctx);
            ValidarCoherenciaMontoTotalSiNeto(ctx);
        }

        /// <summary>E33/E34: CodigoModificacion ∈ 1..5 (0 se trata como 1 al emitir).</summary>
        protected void ValidarCodigoModificacion(EcfBuildContext ctx)
        {
            var cod = ctx.Documento.Referencia!.CodigoModificacion;
            if (cod <= 0) return; // el resolver emite 1
            if (cod is < 1 or > 5)
                throw new InvalidOperationException(
                    $"{Codigo}: CodigoModificacion debe estar entre 1 y 5 (recibido: {cod}).");
        }

        /// <summary>
        /// Si IdDoc permite IndicadorMontoGravado y hay ítems gravados → debe ser 0 o 1.
        /// </summary>
        protected void ValidarIndicadorMontoGravadoSiAplica(EcfBuildContext ctx)
        {
            var amb = (ctx.Documento.AmbienteDgii ?? "").Trim().ToLowerInvariant();
            if (amb is "certecf" or "cert" or "certificacion")
                return;

            var campo = IdDoc.FirstOrDefault(c => c.Nombre == "IndicadorMontoGravado");
            if (campo == null || campo.Presence == EcfCampoPresence.Prohibido)
                return;

            var hayGravado = ctx.Documento.Lineas.Any(l => l.IndicadorFacturacion is 1 or 2 or 3);
            if (hayGravado && ctx.Enc.IndicadorMontoGravado is not (0 or 1))
                throw new InvalidOperationException(
                    $"{Codigo}: IndicadorMontoGravado es obligatorio (0 o 1) cuando hay ítems gravados con ITBIS.");
        }

        /// <summary>
        /// Con IndicadorMontoGravado=0 (montos netos): MontoTotal ≈ gravado + ITBIS + exento (±0.01).
        /// En CerteCF el Excel es el conjunto de datos: no recalcular ni bloquear.
        /// </summary>
        protected void ValidarCoherenciaMontoTotalSiNeto(EcfBuildContext ctx)
        {
            var amb = (ctx.Documento.AmbienteDgii ?? "").Trim().ToLowerInvariant();
            if (amb is "certecf" or "cert" or "certificacion") return;
            if (ctx.Enc.IndicadorMontoGravado != 0) return;
            var esperado = Math.Round(
                ctx.Enc.MontoGravadoTotal + ctx.Enc.TotalItbis + ctx.Enc.MontoExento, 2);
            if (Math.Abs(ctx.Enc.MontoTotal - esperado) > 0.01m)
                throw new InvalidOperationException(
                    $"{Codigo}: MontoTotal ({ctx.Enc.MontoTotal:F2}) no coherente con " +
                    $"MontoGravadoTotal+TotalITBIS+MontoExento ({esperado:F2}).");
        }

        public virtual void PostProcesar(EcfBuildContext ctx, XElement rootEcf) { }

        protected static void ValidarPresenciaCampos(EcfBuildContext ctx, string seccion, IReadOnlyList<EcfCampoDef> campos)
        {
            foreach (var c in campos.OrderBy(x => x.Orden))
            {
                c.Validar?.Invoke(ctx);
                if (c.Presence != EcfCampoPresence.Obligatorio || c.Resolver == null) continue;
                var v = c.Resolver(ctx);
                if (EsValorVacio(v))
                    throw new InvalidOperationException($"{ctx.Definicion.Codigo}: {seccion}.{c.Nombre} es obligatorio.");
            }
        }

        private static bool EsValorVacio(object? v)
            => v is null || (v is string s && string.IsNullOrWhiteSpace(s));

        protected static IReadOnlyList<EcfCampoDef> BuildEmisorComun() => new[]
        {
            EcfXmlFormat.Campo("RNCEmisor", EcfCampoPresence.Obligatorio, 1,
                c => EcfXmlFormat.NormalizarRnc(c.Enc.RncEmisor),
                nota: "XSD: 9 u 11 dígitos"),
            EcfXmlFormat.Campo("RazonSocialEmisor", EcfCampoPresence.Obligatorio, 2,
                c => EcfXmlFormat.Esc(c.Enc.RazonSocialEmisor, 150)),
            EcfXmlFormat.Campo("NombreComercial", EcfCampoPresence.Opcional, 3,
                c => string.IsNullOrWhiteSpace(c.Enc.NombreComercialEmisor) ? null : EcfXmlFormat.Esc(c.Enc.NombreComercialEmisor, 150)),
            EcfXmlFormat.Campo("Sucursal", EcfCampoPresence.Opcional, 4,
                c => string.IsNullOrWhiteSpace(c.Enc.Sucursal) ? null : EcfXmlFormat.Esc(c.Enc.Sucursal, 20)),
            EcfXmlFormat.Campo("DireccionEmisor", EcfCampoPresence.Obligatorio, 5,
                c => EcfXmlFormat.Esc(string.IsNullOrWhiteSpace(c.Enc.DireccionEmisor) ? "N/D" : c.Enc.DireccionEmisor, 100)),
            EcfXmlFormat.Campo("Municipio", EcfCampoPresence.Opcional, 6,
                c => EcfXmlFormat.CodigoProvMun(c.Enc.MunicipioEmisor) ? c.Enc.MunicipioEmisor : null,
                nota: "Solo código DGII 6 dígitos"),
            EcfXmlFormat.Campo("Provincia", EcfCampoPresence.Opcional, 7,
                c => EcfXmlFormat.CodigoProvMun(c.Enc.ProvinciaEmisor) ? c.Enc.ProvinciaEmisor : null),
            EcfXmlFormat.Campo("TablaTelefonoEmisor", EcfCampoPresence.Opcional, 8,
                c => EcfXmlFormat.TablaTelefono(c), complejo: true,
                nota: "Teléfono ###-###-####"),
            EcfXmlFormat.Campo("CorreoEmisor", EcfCampoPresence.Opcional, 9,
                c => EcfXmlFormat.CorreoOk(c.Enc.CorreoEmisor) ? EcfXmlFormat.Esc(c.Enc.CorreoEmisor, 80) : null),
            EcfXmlFormat.Campo("WebSite", EcfCampoPresence.Opcional, 10,
                c => string.IsNullOrWhiteSpace(c.Enc.WebSite) ? null : EcfXmlFormat.Esc(c.Enc.WebSite, 50)),
            EcfXmlFormat.Campo("ActividadEconomica", EcfCampoPresence.Opcional, 11,
                c => string.IsNullOrWhiteSpace(c.Enc.ActividadEconomica) ? null : EcfXmlFormat.Esc(c.Enc.ActividadEconomica, 100)),
            EcfXmlFormat.Campo("CodigoVendedor", EcfCampoPresence.Opcional, 12,
                c => string.IsNullOrWhiteSpace(c.Enc.CodigoVendedor) ? null : EcfXmlFormat.Esc(c.Enc.CodigoVendedor, 60)),
            EcfXmlFormat.Campo("NumeroFacturaInterna", EcfCampoPresence.Opcional, 13,
                c => string.IsNullOrWhiteSpace(c.Enc.NumeroFacturaInterna) ? null : EcfXmlFormat.Esc(c.Enc.NumeroFacturaInterna, 20)),
            EcfXmlFormat.Campo("NumeroPedidoInterno", EcfCampoPresence.Opcional, 14,
                c => string.IsNullOrWhiteSpace(c.Enc.NumeroPedidoInterno) ? null : EcfXmlFormat.Esc(c.Enc.NumeroPedidoInterno, 20)),
            EcfXmlFormat.Campo("ZonaVenta", EcfCampoPresence.Opcional, 15,
                c => string.IsNullOrWhiteSpace(c.Enc.ZonaVenta) ? null : EcfXmlFormat.Esc(c.Enc.ZonaVenta, 20)),
            EcfXmlFormat.Campo("InformacionAdicionalEmisor", EcfCampoPresence.Opcional, 16,
                c => string.IsNullOrWhiteSpace(c.Enc.InformacionAdicionalEmisor) ? null : EcfXmlFormat.Esc(c.Enc.InformacionAdicionalEmisor, 250)),
            EcfXmlFormat.Campo("FechaEmision", EcfCampoPresence.Obligatorio, 17,
                c => EcfXmlFormat.Date(c.Enc.FechaEmision)),
        };

        /// <param name="forzarRncVacio">Si true y no hay RNC, emite 000000000 (crédito fiscal / NC / ND).</param>
        /// <param name="rncObligatorio">Si true, RNCComprador es Obligatorio (E41 Compras).</param>
        /// <param name="incluirDatosOrden">FechaEntrega / orden de compra (E31–E34, E44–E46). E41 no las tiene en XSD.</param>
        protected static IReadOnlyList<EcfCampoDef> BuildCompradorComun(
            bool forzarRncVacio, bool rncObligatorio = false, bool incluirDatosOrden = true)
        {
            var campos = new List<EcfCampoDef>
            {
            EcfXmlFormat.Campo("RNCComprador",
                rncObligatorio ? EcfCampoPresence.Obligatorio : EcfCampoPresence.Opcional, 1,
                c =>
                {
                    if (!string.IsNullOrWhiteSpace(c.Enc.RncComprador))
                        return EcfXmlFormat.NormalizarRnc(c.Enc.RncComprador);
                    if (rncObligatorio) return null;
                    return forzarRncVacio ? "000000000" : null;
                },
                nota: rncObligatorio
                    ? "XSD E41: RNCComprador minOccurs=1 (contraparte / proveedor)"
                    : forzarRncVacio
                        ? "Si no hay RNC se emite 000000000"
                        : "Opcional en consumo (E32)"),
            EcfXmlFormat.Campo("IdentificadorExtranjero", EcfCampoPresence.Opcional, 2,
                c => string.IsNullOrWhiteSpace(c.Enc.IdentificadorExtranjero)
                    ? null
                    : EcfXmlFormat.Esc(c.Enc.IdentificadorExtranjero, 20),
                nota: "E46 CerteCF: Excel trae ID extranjero y RNC vacío"),
            EcfXmlFormat.Campo("RazonSocialComprador", EcfCampoPresence.Obligatorio, 3,
                c => EcfXmlFormat.Esc(string.IsNullOrWhiteSpace(c.Enc.RazonSocialComprador) ? "CONSUMIDOR FINAL" : c.Enc.RazonSocialComprador, 150),
                nota: "Orden XSD: Contacto, Correo, Direccion, Municipio, Provincia"),
            EcfXmlFormat.Campo("ContactoComprador", EcfCampoPresence.Opcional, 4,
                c => string.IsNullOrWhiteSpace(c.Enc.ContactoComprador) ? null : EcfXmlFormat.Esc(c.Enc.ContactoComprador, 80)),
            EcfXmlFormat.Campo("CorreoComprador", EcfCampoPresence.Opcional, 5,
                c => EcfXmlFormat.CorreoOk(c.Enc.CorreoComprador) ? EcfXmlFormat.Esc(c.Enc.CorreoComprador, 80) : null),
            EcfXmlFormat.Campo("DireccionComprador", EcfCampoPresence.Opcional, 6,
                c => string.IsNullOrWhiteSpace(c.Enc.DireccionComprador) ? null : EcfXmlFormat.Esc(c.Enc.DireccionComprador, 100)),
            EcfXmlFormat.Campo("MunicipioComprador", EcfCampoPresence.Opcional, 7,
                c => EcfXmlFormat.CodigoProvMun(c.Enc.MunicipioComprador) ? c.Enc.MunicipioComprador!.Trim() : null),
            EcfXmlFormat.Campo("ProvinciaComprador", EcfCampoPresence.Opcional, 8,
                c => EcfXmlFormat.CodigoProvMun(c.Enc.ProvinciaComprador) ? c.Enc.ProvinciaComprador!.Trim() : null),
            };
            var orden = 9;
            if (incluirDatosOrden)
            {
                campos.Add(EcfXmlFormat.Campo("FechaEntrega", EcfCampoPresence.Opcional, orden++,
                    c => c.Enc.FechaEntrega is DateTime fe ? EcfXmlFormat.Date(fe) : null));
                campos.Add(EcfXmlFormat.Campo("ContactoEntrega", EcfCampoPresence.Opcional, orden++,
                    _ => null,
                    nota: "XSD entre FechaEntrega y DireccionEntrega. CerteCF: celda vacía = omitir"));
                campos.Add(EcfXmlFormat.Campo("DireccionEntrega", EcfCampoPresence.Opcional, orden++,
                    _ => null,
                    nota: "CerteCF: celda vacía = omitir"));
                campos.Add(EcfXmlFormat.Campo("TelefonoAdicional", EcfCampoPresence.Opcional, orden++,
                    _ => null,
                    nota: "CerteCF: emitir si el Excel trae valor, aunque copie TelefonoEmisor"));
                campos.Add(EcfXmlFormat.Campo("FechaOrdenCompra", EcfCampoPresence.Opcional, orden++,
                    c => c.Enc.FechaOrdenCompra is DateTime fo ? EcfXmlFormat.Date(fo) : null));
                campos.Add(EcfXmlFormat.Campo("NumeroOrdenCompra", EcfCampoPresence.Opcional, orden++,
                    c => string.IsNullOrWhiteSpace(c.Enc.NumeroOrdenCompra)
                        ? null
                        : EcfXmlFormat.Esc(c.Enc.NumeroOrdenCompra, 20)));
            }
            campos.Add(EcfXmlFormat.Campo("CodigoInternoComprador", EcfCampoPresence.Opcional, orden,
                c => string.IsNullOrWhiteSpace(c.Enc.CodigoInternoComprador)
                    ? null
                    : EcfXmlFormat.Esc(c.Enc.CodigoInternoComprador, 20)));
            return campos;
        }

        protected static IReadOnlyList<EcfCampoDef> BuildTotalesComun() => new[]
        {
            EcfXmlFormat.Campo("MontoGravadoTotal", EcfCampoPresence.Opcional, 1, c => Pos(c.Enc.MontoGravadoTotal)),
            EcfXmlFormat.Campo("MontoGravadoI1", EcfCampoPresence.Opcional, 2, c => Pos(c.Enc.MontoGravadoI1)),
            EcfXmlFormat.Campo("MontoGravadoI2", EcfCampoPresence.Opcional, 3, c => Pos(c.Enc.MontoGravadoI2)),
            EcfXmlFormat.Campo("MontoGravadoI3", EcfCampoPresence.Opcional, 4, c => Pos(c.Enc.MontoGravadoI3)),
            EcfXmlFormat.Campo("MontoExento", EcfCampoPresence.Opcional, 5, c => Pos(c.Enc.MontoExento)),
            EcfXmlFormat.Campo("ITBIS1", EcfCampoPresence.Opcional, 6,
                c => (c.Enc.MontoGravadoI1 > 0 || c.Enc.TotalItbis1 > 0) ? 18 : null),
            EcfXmlFormat.Campo("ITBIS2", EcfCampoPresence.Opcional, 7,
                c => (c.Enc.MontoGravadoI2 > 0 || c.Enc.TotalItbis2 > 0) ? 16 : null),
            EcfXmlFormat.Campo("ITBIS3", EcfCampoPresence.Opcional, 8,
                c => (c.Enc.MontoGravadoI3 > 0 || c.Enc.TotalItbis3 > 0) ? 0 : null),
            EcfXmlFormat.Campo("TotalITBIS", EcfCampoPresence.Opcional, 9, c => Pos(c.Enc.TotalItbis)),
            EcfXmlFormat.Campo("TotalITBIS1", EcfCampoPresence.Opcional, 10, c => Pos(c.Enc.TotalItbis1)),
            EcfXmlFormat.Campo("TotalITBIS2", EcfCampoPresence.Opcional, 11, c => Pos(c.Enc.TotalItbis2)),
            EcfXmlFormat.Campo("TotalITBIS3", EcfCampoPresence.Opcional, 12, c => Pos(c.Enc.TotalItbis3)),
            EcfXmlFormat.Campo("MontoImpuestoAdicional", EcfCampoPresence.Opcional, 13,
                c => c.Enc.MontoImpuestoAdicional is > 0 and var mia ? EcfXmlFormat.Money(mia) : null,
                nota: "XSD antes de MontoTotal. CerteCF: celda vacía = omitir"),
            EcfXmlFormat.Campo("ImpuestosAdicionales", EcfCampoPresence.Opcional, 14,
                c => EcfXmlFormat.ImpuestosAdicionales(c), complejo: true,
                nota: "Solo si Excel trae TipoImpuesto + Tasa"),
            EcfXmlFormat.Campo("MontoTotal", EcfCampoPresence.Obligatorio, 15, c => EcfXmlFormat.Money(c.Enc.MontoTotal)),
            EcfXmlFormat.Campo("MontoNoFacturable", EcfCampoPresence.Opcional, 16,
                c => c.Enc.MontoNoFacturable is decimal nf ? EcfXmlFormat.Money(nf) : null),
            EcfXmlFormat.Campo("MontoPeriodo", EcfCampoPresence.Opcional, 17,
                c => c.Enc.MontoPeriodo is decimal mp ? EcfXmlFormat.Money(mp) : null),
            EcfXmlFormat.Campo("SaldoAnterior", EcfCampoPresence.Opcional, 18,
                c => c.Enc.SaldoAnterior is decimal sa ? EcfXmlFormat.Money(sa) : null),
            EcfXmlFormat.Campo("MontoAvancePago", EcfCampoPresence.Opcional, 19,
                c => c.Enc.MontoAvancePago is decimal ap ? EcfXmlFormat.Money(ap) : null),
            EcfXmlFormat.Campo("ValorPagar", EcfCampoPresence.Opcional, 20,
                c => c.Enc.ValorPagar is decimal vp ? EcfXmlFormat.Money(vp) : null,
                nota: "Solo si el Excel trae número (0.00 incluido). Celda vacía = omitir, no enviar 0.00"),
            EcfXmlFormat.Campo("TotalITBISRetenido", EcfCampoPresence.Opcional, 21, c => Pos(c.Enc.TotalItbisRetenido)),
            EcfXmlFormat.Campo("TotalISRRetencion", EcfCampoPresence.Opcional, 22, c => Pos(c.Enc.TotalIsrRetencion)),
            EcfXmlFormat.Campo("MontoPropinaLegal", EcfCampoPresence.Prohibido, 99,
                nota: "No existe en XSD Totales e-CF; no emitir",
                prohibidoModo: EcfProhibidoModo.Omitir),
        };

        protected static IReadOnlyList<EcfCampoDef> BuildItemComun() => new[]
        {
            EcfXmlFormat.Campo("NumeroLinea", EcfCampoPresence.Obligatorio, 1,
                c => c.LineaActual!.NumeroLinea),
            EcfXmlFormat.Campo("IndicadorFacturacion", EcfCampoPresence.Obligatorio, 2,
                c => c.LineaActual!.IndicadorFacturacion),
            EcfXmlFormat.Campo("NombreItem", EcfCampoPresence.Obligatorio, 3,
                c => EcfXmlFormat.Esc(c.LineaActual!.NombreItem, 80)),
            EcfXmlFormat.Campo("IndicadorBienoServicio", EcfCampoPresence.Obligatorio, 4,
                c => c.LineaActual!.EsBien ? 1 : 2),
            EcfXmlFormat.Campo("DescripcionItem", EcfCampoPresence.Opcional, 5,
                c => string.IsNullOrWhiteSpace(c.LineaActual!.DescripcionItem)
                    ? null
                    : EcfXmlFormat.Esc(c.LineaActual.DescripcionItem, 1000)),
            EcfXmlFormat.Campo("CantidadItem", EcfCampoPresence.Obligatorio, 6,
                c => EcfXmlFormat.CantidadOPrecio(c, c.LineaActual!.Cantidad)),
            EcfXmlFormat.Campo("UnidadMedida", EcfCampoPresence.Opcional, 7,
                c => c.LineaActual!.UnidadMedida),
            EcfXmlFormat.Campo("CantidadReferencia", EcfCampoPresence.Opcional, 8,
                c => c.LineaActual!.CantidadReferencia is decimal cr ? EcfXmlFormat.DecimalComoDato(cr) : null),
            EcfXmlFormat.Campo("UnidadReferencia", EcfCampoPresence.Opcional, 9,
                c => c.LineaActual!.UnidadReferencia),
            EcfXmlFormat.Campo("TablaSubcantidad", EcfCampoPresence.Opcional, 10,
                c => EcfXmlFormat.TablaSubcantidadItem(c), complejo: true,
                nota: "Obligatoria si hay ISC 006-039 (alcohol/tabaco)"),
            EcfXmlFormat.Campo("GradosAlcohol", EcfCampoPresence.Opcional, 11,
                c => c.LineaActual!.GradosAlcohol is > 0 and var ga ? EcfXmlFormat.Money(ga) : null),
            EcfXmlFormat.Campo("PrecioUnitarioReferencia", EcfCampoPresence.Opcional, 12,
                c => c.LineaActual!.PrecioUnitarioReferencia is > 0 and var pur
                    ? EcfXmlFormat.Money(pur)
                    : null),
            EcfXmlFormat.Campo("FechaElaboracion", EcfCampoPresence.Opcional, 13,
                c => c.LineaActual!.FechaElaboracion is DateTime fe ? EcfXmlFormat.Date(fe) : null),
            EcfXmlFormat.Campo("FechaVencimientoItem", EcfCampoPresence.Opcional, 14,
                c => c.LineaActual!.FechaVencimientoItem is DateTime fv ? EcfXmlFormat.Date(fv) : null),
            EcfXmlFormat.Campo("PrecioUnitarioItem", EcfCampoPresence.Obligatorio, 15,
                c => EcfXmlFormat.CantidadOPrecio(c, c.LineaActual!.PrecioUnitario)),
            EcfXmlFormat.Campo("DescuentoMonto", EcfCampoPresence.Opcional, 16,
                c => c.LineaActual!.DescuentoMonto is > 0 ? EcfXmlFormat.Money(c.LineaActual.DescuentoMonto.Value) : null),
            EcfXmlFormat.Campo("TablaSubDescuento", EcfCampoPresence.Opcional, 17,
                c => EcfXmlFormat.TablaSubDescuentoItem(c), complejo: true,
                nota: "Obligatoria si hay DescuentoMonto"),
            EcfXmlFormat.Campo("RecargoMonto", EcfCampoPresence.Opcional, 18,
                c => c.LineaActual!.RecargoMonto is > 0 ? EcfXmlFormat.Money(c.LineaActual.RecargoMonto.Value) : null),
            EcfXmlFormat.Campo("TablaSubRecargo", EcfCampoPresence.Opcional, 19,
                c => EcfXmlFormat.TablaSubRecargoItem(c), complejo: true,
                nota: "Obligatoria si hay RecargoMonto"),
            EcfXmlFormat.Campo("TablaImpuestoAdicional", EcfCampoPresence.Opcional, 20,
                c => EcfXmlFormat.TablaImpuestoAdicionalItem(c), complejo: true),
            EcfXmlFormat.Campo("MontoItem", EcfCampoPresence.Obligatorio, 21,
                c => EcfXmlFormat.Money(c.LineaActual!.MontoItem)),
        };

        protected static IReadOnlyList<EcfCampoDef> BuildReferenciaComun() => new[]
        {
            EcfXmlFormat.Campo("NCFModificado", EcfCampoPresence.Obligatorio, 1,
                c => c.Documento.Referencia!.NcfModificado.Trim().ToUpperInvariant()),
            EcfXmlFormat.Campo("RNCOtroContribuyente", EcfCampoPresence.Opcional, 2,
                c => string.IsNullOrWhiteSpace(c.Documento.Referencia?.RncOtroContribuyente)
                    ? null
                    : EcfXmlFormat.NormalizarRnc(c.Documento.Referencia!.RncOtroContribuyente)),
            EcfXmlFormat.Campo("FechaNCFModificado", EcfCampoPresence.Obligatorio, 3,
                c => EcfXmlFormat.Date(c.Documento.Referencia?.FechaNcfModificado ?? c.Enc.FechaEmision)),
            EcfXmlFormat.Campo("CodigoModificacion", EcfCampoPresence.Obligatorio, 4,
                c => c.Documento.Referencia!.CodigoModificacion > 0 ? c.Documento.Referencia.CodigoModificacion : 1,
                nota: "1=Anula 2=Texto 3=Montos 4=Contingencia 5=Ref consumo"),
            EcfXmlFormat.Campo("RazonModificacion", EcfCampoPresence.Opcional, 5,
                c => string.IsNullOrWhiteSpace(c.Documento.Referencia?.RazonModificacion)
                    ? null
                    : EcfXmlFormat.Esc(c.Documento.Referencia!.RazonModificacion, 90)),
        };

        /// <summary>IdDoc común: TipoeCF + eNCF + campos variables por tipo.</summary>
        protected static List<EcfCampoDef> IdDocInicio(int tipoeCF)
            => new()
            {
                EcfXmlFormat.Campo("TipoeCF", EcfCampoPresence.Obligatorio, 1, _ => tipoeCF),
                EcfXmlFormat.Campo("eNCF", EcfCampoPresence.Obligatorio, 2,
                    c => c.Enc.Encf.Trim().ToUpperInvariant()),
            };

        protected static EcfCampoDef CampoFechaVencimiento(EcfCampoPresence presence, int orden)
            => EcfXmlFormat.Campo("FechaVencimientoSecuencia", presence, orden,
                c => presence == EcfCampoPresence.Prohibido
                    ? null
                    : EcfXmlFormat.Date(EcfXmlFormat.FechaVencimientoPrecert(c.Enc.FechaVencimientoSecuencia)),
                nota: presence == EcfCampoPresence.Prohibido
                    ? "No aplica en este tipo (XSD)"
                    : "Obligatoria; precert testecf suele exigir 31-12-2028");

        protected static EcfCampoDef CampoIndicadorMontoGravado(int orden)
            => EcfXmlFormat.Campo("IndicadorMontoGravado", EcfCampoPresence.Opcional, orden,
                c => c.Enc.IndicadorMontoGravado);

        protected static EcfCampoDef CampoTipoIngresos(EcfCampoPresence presence, int orden)
            => EcfXmlFormat.Campo("TipoIngresos", presence, orden, c =>
            {
                if (EcfXmlFormat.TryCeldaExcel(c, "TipoIngresos", out var excel, 0)
                    && int.TryParse(excel.Trim(), out var desdeSet)
                    && desdeSet > 0)
                    return EcfXmlFormat.TipoIngreso(desdeSet);

                // El set marca #e: no inventar 01. Solo se rellena si el XSD lo exige.
                if (EcfXmlFormat.DebeRespetarExcel(c) && presence != EcfCampoPresence.Obligatorio)
                    return null;

                return EcfXmlFormat.TipoIngreso(c.Enc.TipoIngreso > 0 ? c.Enc.TipoIngreso : 1);
            });

        protected static EcfCampoDef CampoTipoPago(int orden)
            => CampoTipoPago(EcfCampoPresence.Obligatorio, orden);

        protected static EcfCampoDef CampoTipoPago(EcfCampoPresence presence, int orden)
            => EcfXmlFormat.Campo("TipoPago", presence, orden,
                c =>
                {
                    if (c.Enc.TipoPago > 0) return c.Enc.TipoPago;
                    return presence == EcfCampoPresence.Obligatorio ? 1 : null;
                });

        /// <summary>Solo si el set trae la celda. Vacío (#e) no se inventa.</summary>
        protected static EcfCampoDef CampoFechaLimitePago(int orden)
            => EcfXmlFormat.Campo("FechaLimitePago", EcfCampoPresence.Opcional, orden,
                c => EcfXmlFormat.TryCeldaExcel(c, "FechaLimitePago", out var excel, 0) ? excel.Trim() : null);

        protected static EcfCampoDef CampoTerminoPago(int orden)
            => EcfXmlFormat.Campo("TerminoPago", EcfCampoPresence.Opcional, orden,
                c => EcfXmlFormat.TryCeldaExcel(c, "TerminoPago", out var excel, 0)
                    ? EcfXmlFormat.Esc(excel.Trim(), 15)
                    : null);

        protected static EcfCampoDef CampoTipoCuentaPago(int orden)
            => EcfXmlFormat.Campo("TipoCuentaPago", EcfCampoPresence.Opcional, orden,
                c => EcfXmlFormat.TryCeldaExcel(c, "TipoCuentaPago", out var excel, 0) ? excel.Trim() : null);

        protected static EcfCampoDef CampoNumeroCuentaPago(int orden)
            => EcfXmlFormat.Campo("NumeroCuentaPago", EcfCampoPresence.Opcional, orden,
                c => EcfXmlFormat.TryCeldaExcel(c, "NumeroCuentaPago", out var excel, 0) ? excel.Trim() : null);

        protected static EcfCampoDef CampoBancoPago(int orden)
            => EcfXmlFormat.Campo("BancoPago", EcfCampoPresence.Opcional, orden,
                c => EcfXmlFormat.TryCeldaExcel(c, "BancoPago", out var excel, 0) ? excel.Trim() : null);

        protected static EcfCampoDef CampoTablaFormasPago(EcfCampoPresence presence, int orden)
            => EcfXmlFormat.Campo("TablaFormasPago", presence, orden,
                c => presence == EcfCampoPresence.Prohibido ? null : EcfXmlFormat.TablaFormasPago(c),
                complejo: true,
                nota: presence == EcfCampoPresence.Prohibido
                    ? "XSD de este tipo NO incluye TablaFormasPago en IdDoc"
                    : "Opcional; máx. 7 formas",
                prohibidoModo: EcfProhibidoModo.Omitir);

        private static object? Pos(decimal v) => v > 0 ? EcfXmlFormat.Money(v) : null;
    }
}
