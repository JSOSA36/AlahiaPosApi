using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Events;
using AlahiaPos.Entities.Interfaces;

namespace AlahiaPos.DataAccess.Servicios
{
    /// <summary>
    /// Construye asientos automáticos desde eventos operativos usando ContabilidadCuentaMapeo.
    /// </summary>
    internal static class ContabilidadAsientoBuilders
    {
        private static readonly HashSet<string> CategoriasTesoreriaYaContabilizadas = new(StringComparer.OrdinalIgnoreCase)
        {
            "VENTA", "GASTO", "COMPRA", "COBRO", "PAGO", "PAGO_PROVEEDOR", "PAGO_CLIENTE",
            "INGRESO", "INGRESO_EXTRA", "TRANSFERENCIA"
        };

        public static async Task<List<ContabilidadIntegracionRequest>> DesdeGastoAsync(
            GastoRegistradoEvent e,
            IContabilidadCuentaMapeoService mapeo)
        {
            if (e.Monto <= 0)
                return new List<ContabilidadIntegracionRequest>();

            var idGasto = e.IdCuentaContableGasto is > 0
                ? e.IdCuentaContableGasto.Value
                : await mapeo.ResolverRequeridoAsync(e.IdEmpresa, ContabilidadConceptosMapeo.GastoOperativo);
            var idTesoreria = await mapeo.ResolverTesoreriaAsync(e.IdEmpresa, e.FormaPago, e.TipoCuentaFinanciera);

            return new List<ContabilidadIntegracionRequest>
            {
                CrearRequest(
                    e,
                    ContabilidadConstantes.OrigenGastos,
                    ContabilidadTipoOperacion.Alta,
                    $"Gasto: {e.TipoGasto ?? "Operativo"} — {e.Detalle}".Trim(' ', '—'),
                    new ContabilidadIntegracionLinea { IdCuentaContable = idGasto, Debito = e.Monto },
                    new ContabilidadIntegracionLinea { IdCuentaContable = idTesoreria, Credito = e.Monto })
            };
        }

        public static async Task<List<ContabilidadIntegracionRequest>> DesdeIngresoAsync(
            IngresoExtraRegistradoEvent e,
            IContabilidadCuentaMapeoService mapeo)
        {
            if (e.Monto <= 0)
                return new List<ContabilidadIntegracionRequest>();

            var idTesoreria = await mapeo.ResolverTesoreriaAsync(e.IdEmpresa, e.FormaPago, e.TipoCuentaFinanciera);
            var idIngreso = await mapeo.ResolverRequeridoAsync(e.IdEmpresa, ContabilidadConceptosMapeo.OtrosIngresos);

            return new List<ContabilidadIntegracionRequest>
            {
                CrearRequest(
                    e,
                    ContabilidadConstantes.OrigenIngresos,
                    ContabilidadTipoOperacion.Alta,
                    $"Ingreso: {e.Categoria ?? "Extra"} — {e.Descripcion}".Trim(' ', '—'),
                    new ContabilidadIntegracionLinea { IdCuentaContable = idTesoreria, Debito = e.Monto },
                    new ContabilidadIntegracionLinea { IdCuentaContable = idIngreso, Credito = e.Monto })
            };
        }

        public static async Task<List<ContabilidadIntegracionRequest>> DesdeVentaAsync(
            VentaConfirmadaEvent e,
            ContabilidadConfiguracion config,
            IContabilidadCuentaMapeoService mapeo)
        {
            var resultado = new List<ContabilidadIntegracionRequest>();
            if (e.Total <= 0)
                return resultado;

            var idVentas = await mapeo.ResolverRequeridoAsync(e.IdEmpresa, ContabilidadConceptosMapeo.Ventas);
            var idItbis = await mapeo.ResolverRequeridoAsync(e.IdEmpresa, ContabilidadConceptosMapeo.ItbisPagar);

            var lineas = new List<ContabilidadIntegracionLinea>();

            var cobrado = e.MontoCobrado > 0 ? e.MontoCobrado : (e.MontoCredito > 0 ? 0 : e.Total);
            var credito = e.MontoCredito;
            if (cobrado <= 0 && credito <= 0)
                cobrado = e.Total;

            if (cobrado > 0)
            {
                var pagos = e.Pagos?.Where(p => p.Monto > 0).ToList() ?? new List<VentaPagoParte>();
                if (pagos.Count > 0)
                {
                    foreach (var pago in pagos)
                    {
                        var idTesoreria = await mapeo.ResolverTesoreriaPorCuentaAsync(
                            e.IdEmpresa, pago.IdCuentaFinanciera, pago.Metodo, pago.TipoCuentaFinanciera);
                        lineas.Add(new ContabilidadIntegracionLinea { IdCuentaContable = idTesoreria, Debito = pago.Monto });
                    }
                }
                else
                {
                    var idTesoreria = await mapeo.ResolverTesoreriaPorCuentaAsync(
                        e.IdEmpresa, e.IdCuentaFinanciera, e.MetodoPago, e.TipoCuentaFinanciera);
                    lineas.Add(new ContabilidadIntegracionLinea { IdCuentaContable = idTesoreria, Debito = cobrado });
                }
            }

            if (credito > 0)
            {
                var idCxc = await mapeo.ResolverRequeridoAsync(e.IdEmpresa, ContabilidadConceptosMapeo.Cxc);
                lineas.Add(new ContabilidadIntegracionLinea { IdCuentaContable = idCxc, Debito = credito });
            }

            if (e.Subtotal > 0)
                lineas.Add(new ContabilidadIntegracionLinea { IdCuentaContable = idVentas, Credito = e.Subtotal });

            if (e.Itbis > 0)
                lineas.Add(new ContabilidadIntegracionLinea { IdCuentaContable = idItbis, Credito = e.Itbis });

            var debito = lineas.Sum(l => l.Debito);
            var creditoSum = lineas.Sum(l => l.Credito);
            var diff = debito - creditoSum;
            if (Math.Abs(diff) > 0.009m && Math.Abs(diff) < 1m)
            {
                var ventaLinea = lineas.First(l => l.IdCuentaContable == idVentas);
                if (diff > 0)
                    ventaLinea.Credito += diff;
                else
                    ventaLinea.Credito = Math.Max(0, ventaLinea.Credito + diff);
            }

            var lineasCogs = new List<ContabilidadIntegracionLinea>();
            if (config.GenerarCOGSAutomatico && e.CostoInventario > 0)
            {
                var idCogs = await mapeo.ResolverRequeridoAsync(e.IdEmpresa, ContabilidadConceptosMapeo.CostoVentas);
                var idInv = await mapeo.ResolverRequeridoAsync(e.IdEmpresa, ContabilidadConceptosMapeo.Inventario);
                lineasCogs.Add(new ContabilidadIntegracionLinea { IdCuentaContable = idCogs, Debito = e.CostoInventario });
                lineasCogs.Add(new ContabilidadIntegracionLinea { IdCuentaContable = idInv, Credito = e.CostoInventario });
            }

            if (lineasCogs.Count > 0 && !config.SepararAsientoCOGS)
            {
                lineas.AddRange(lineasCogs);
                resultado.Add(CrearRequest(
                    e,
                    ContabilidadConstantes.OrigenVentas,
                    ContabilidadTipoOperacion.Alta,
                    $"Venta {e.NumeroFactura} ({e.TipoFactura})",
                    lineas.ToArray()));
            }
            else
            {
                resultado.Add(CrearRequest(
                    e,
                    ContabilidadConstantes.OrigenVentas,
                    ContabilidadTipoOperacion.Alta,
                    $"Venta {e.NumeroFactura} ({e.TipoFactura})",
                    lineas.ToArray()));

                if (lineasCogs.Count > 0)
                {
                    resultado.Add(CrearRequest(
                        e,
                        ContabilidadConstantes.OrigenVentas,
                        ContabilidadTipoOperacion.Cogs,
                        $"Costo venta {e.NumeroFactura}",
                        lineasCogs.ToArray()));
                }
            }

            return resultado;
        }

        public static async Task<List<ContabilidadIntegracionRequest>> DesdeNotaCreditoAsync(
            NotaCreditoCreadaEvent e,
            ContabilidadConfiguracion config,
            IContabilidadCuentaMapeoService mapeo)
        {
            var resultado = new List<ContabilidadIntegracionRequest>();
            if (e.Total <= 0)
                return resultado;

            var idVentas = await mapeo.ResolverRequeridoAsync(e.IdEmpresa, ContabilidadConceptosMapeo.Ventas);
            var idItbis = await mapeo.ResolverRequeridoAsync(e.IdEmpresa, ContabilidadConceptosMapeo.ItbisPagar);

            var lineas = new List<ContabilidadIntegracionLinea>();

            if (e.Subtotal > 0)
                lineas.Add(new ContabilidadIntegracionLinea { IdCuentaContable = idVentas, Debito = e.Subtotal });

            if (e.Itbis > 0)
                lineas.Add(new ContabilidadIntegracionLinea { IdCuentaContable = idItbis, Debito = e.Itbis });

            var montoCxc = e.MontoCxc;
            var montoTesoreria = e.MontoTesoreria;
            if (montoCxc <= 0 && montoTesoreria <= 0)
                montoCxc = e.Total;

            if (montoCxc > 0)
            {
                var idCxc = await mapeo.ResolverRequeridoAsync(e.IdEmpresa, ContabilidadConceptosMapeo.Cxc);
                lineas.Add(new ContabilidadIntegracionLinea { IdCuentaContable = idCxc, Credito = montoCxc });
            }

            if (montoTesoreria > 0)
            {
                var idTesoreria = await mapeo.ResolverTesoreriaAsync(e.IdEmpresa, e.MetodoPago, e.TipoCuentaFinanciera);
                lineas.Add(new ContabilidadIntegracionLinea { IdCuentaContable = idTesoreria, Credito = montoTesoreria });
            }

            CuadrarEnPrimeraLineaDebito(lineas);

            resultado.Add(CrearRequest(
                e,
                ContabilidadConstantes.OrigenNotasCredito,
                ContabilidadTipoOperacion.Alta,
                $"Nota crédito {e.NumeroDocumento} (Fact #{e.IdFacturaHeader})",
                lineas.ToArray()));

            if (config.GenerarCOGSAutomatico && e.CostoInventario > 0)
            {
                var idCogs = await mapeo.ResolverRequeridoAsync(e.IdEmpresa, ContabilidadConceptosMapeo.CostoVentas);
                var idInv = await mapeo.ResolverRequeridoAsync(e.IdEmpresa, ContabilidadConceptosMapeo.Inventario);

                var lineasCogs = new[]
                {
                    new ContabilidadIntegracionLinea { IdCuentaContable = idInv, Debito = e.CostoInventario },
                    new ContabilidadIntegracionLinea { IdCuentaContable = idCogs, Credito = e.CostoInventario }
                };

                if (config.SepararAsientoCOGS)
                {
                    resultado.Add(CrearRequest(
                        e,
                        ContabilidadConstantes.OrigenNotasCredito,
                        ContabilidadTipoOperacion.Cogs,
                        $"Reverso costo NC {e.NumeroDocumento}",
                        lineasCogs));
                }
                else
                {
                    var alta = resultado[0];
                    alta.Lineas.AddRange(lineasCogs);
                }
            }

            return resultado;
        }

        public static async Task<List<ContabilidadIntegracionRequest>> DesdeCompraAsync(
            CompraConfirmadaEvent e,
            IContabilidadCuentaMapeoService mapeo)
        {
            if (e.Total <= 0)
                return new List<ContabilidadIntegracionRequest>();

            var lineas = new List<ContabilidadIntegracionLinea>();

            if (e.MontoInventario > 0)
            {
                var idInv = await mapeo.ResolverRequeridoAsync(e.IdEmpresa, ContabilidadConceptosMapeo.Inventario);
                lineas.Add(new ContabilidadIntegracionLinea { IdCuentaContable = idInv, Debito = e.MontoInventario });
            }

            if (e.MontoGasto > 0)
            {
                var idGasto = await mapeo.ResolverRequeridoAsync(e.IdEmpresa, ContabilidadConceptosMapeo.GastoOperativo);
                lineas.Add(new ContabilidadIntegracionLinea { IdCuentaContable = idGasto, Debito = e.MontoGasto });
            }

            if (e.MontoActivoFijo > 0)
            {
                var idActivoFijo = await mapeo.ResolverRequeridoAsync(e.IdEmpresa, ContabilidadConceptosMapeo.ActivoFijo);
                lineas.Add(new ContabilidadIntegracionLinea { IdCuentaContable = idActivoFijo, Debito = e.MontoActivoFijo });
            }

            if (e.TotalItbis > 0)
            {
                var idItbis = await mapeo.ResolverRequeridoAsync(e.IdEmpresa, ContabilidadConceptosMapeo.ItbisCobrar);
                lineas.Add(new ContabilidadIntegracionLinea { IdCuentaContable = idItbis, Debito = e.TotalItbis });
            }

            var neto = e.Total - e.TotalItbis;
            if (e.MontoInventario <= 0 && e.MontoGasto <= 0 && e.MontoActivoFijo <= 0 && neto > 0)
            {
                var idInv = await mapeo.ResolverRequeridoAsync(e.IdEmpresa, ContabilidadConceptosMapeo.Inventario);
                lineas.Add(new ContabilidadIntegracionLinea { IdCuentaContable = idInv, Debito = neto });
            }

            if (e.EsContado)
            {
                var idTesoreria = await mapeo.ResolverTesoreriaAsync(e.IdEmpresa, e.FormaPago, e.TipoCuentaFinanciera);
                lineas.Add(new ContabilidadIntegracionLinea { IdCuentaContable = idTesoreria, Credito = e.Total });
            }
            else
            {
                var idCxp = await mapeo.ResolverRequeridoAsync(e.IdEmpresa, ContabilidadConceptosMapeo.Cxp);
                lineas.Add(new ContabilidadIntegracionLinea { IdCuentaContable = idCxp, Credito = e.Total });
            }

            return new List<ContabilidadIntegracionRequest>
            {
                CrearRequest(
                    e,
                    ContabilidadConstantes.OrigenCompras,
                    ContabilidadTipoOperacion.Alta,
                    $"Compra {e.NumeroDocumento}",
                    lineas.ToArray())
            };
        }

        public static async Task<List<ContabilidadIntegracionRequest>> DesdeCobroClienteAsync(
            CobroClienteRegistradoEvent e,
            IContabilidadCuentaMapeoService mapeo)
        {
            if (e.Monto <= 0)
                return new List<ContabilidadIntegracionRequest>();

            var idTesoreria = await mapeo.ResolverTesoreriaAsync(e.IdEmpresa, e.FormaPago, e.TipoCuentaFinanciera);
            var idCxc = await mapeo.ResolverRequeridoAsync(e.IdEmpresa, ContabilidadConceptosMapeo.Cxc);

            return new List<ContabilidadIntegracionRequest>
            {
                CrearRequest(
                    e,
                    ContabilidadConstantes.OrigenVentas,
                    ContabilidadTipoOperacion.Cobro,
                    $"Cobro cliente factura #{e.IdFacturaHeader}",
                    new ContabilidadIntegracionLinea { IdCuentaContable = idTesoreria, Debito = e.Monto },
                    new ContabilidadIntegracionLinea { IdCuentaContable = idCxc, Credito = e.Monto })
            };
        }

        public static async Task<List<ContabilidadIntegracionRequest>> DesdePagoProveedorAsync(
            PagoProveedorRegistradoEvent e,
            IContabilidadCuentaMapeoService mapeo)
        {
            if (e.Monto <= 0)
                return new List<ContabilidadIntegracionRequest>();

            var idCxp = await mapeo.ResolverRequeridoAsync(e.IdEmpresa, ContabilidadConceptosMapeo.Cxp);
            var idTesoreria = await mapeo.ResolverTesoreriaAsync(e.IdEmpresa, e.FormaPago, e.TipoCuentaFinanciera);

            return new List<ContabilidadIntegracionRequest>
            {
                CrearRequest(
                    e,
                    ContabilidadConstantes.OrigenCompras,
                    ContabilidadTipoOperacion.Pago,
                    $"Pago proveedor compra #{e.IdOrdenCompraHeader}",
                    new ContabilidadIntegracionLinea { IdCuentaContable = idCxp, Debito = e.Monto },
                    new ContabilidadIntegracionLinea { IdCuentaContable = idTesoreria, Credito = e.Monto })
            };
        }

        public static async Task<List<ContabilidadIntegracionRequest>> DesdeMovimientoBancarioAsync(
            MovimientoBancarioRegistradoEvent e,
            IContabilidadCuentaMapeoService mapeo)
        {
            if (e.Monto <= 0)
                return new List<ContabilidadIntegracionRequest>();

            var tipo = (e.TipoMovimiento ?? string.Empty).Trim().ToUpperInvariant();
            var categoria = (e.Categoria ?? string.Empty).Trim().ToUpperInvariant();

            if (tipo == "TRANSFERENCIA")
            {
                var idDestino = await mapeo.ResolverTesoreriaPorCuentaAsync(e.IdEmpresa, e.IdCuentaDestino, null, e.TipoCuentaDestino);
                var idOrigen = await mapeo.ResolverTesoreriaPorCuentaAsync(e.IdEmpresa, e.IdCuentaOrigen, null, e.TipoCuentaOrigen);
                var esReclas = string.Equals(categoria, "RECLASIFICAR_PAGO", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(e.ReferenciaTipo, "RECLASIFICAR_PAGO", StringComparison.OrdinalIgnoreCase);
                var tipoOp = esReclas
                    ? ContabilidadTipoOperacion.ReclasificarPago
                    : ContabilidadTipoOperacion.Alta;
                var concepto = esReclas
                    ? $"Reclasificación de pago: {e.Motivo}"
                    : $"Transferencia: {e.Motivo}";

                return new List<ContabilidadIntegracionRequest>
                {
                    CrearRequest(
                        e,
                        ContabilidadConstantes.OrigenBanco,
                        tipoOp,
                        concepto,
                        new ContabilidadIntegracionLinea { IdCuentaContable = idDestino, Debito = e.Monto },
                        new ContabilidadIntegracionLinea { IdCuentaContable = idOrigen, Credito = e.Monto })
                };
            }

            // Entradas/salidas operativas (venta/gasto/cobro/pago) ya van por su módulo.
            if (CategoriasTesoreriaYaContabilizadas.Contains(categoria))
                return new List<ContabilidadIntegracionRequest>();

            // Ajustes manuales y diferencia de cierre de caja.
            if (tipo is "ENTRADA" or "CIERRE_SOBRANTE")
            {
                var idTesoreria = await mapeo.ResolverTesoreriaAsync(
                    e.IdEmpresa, null, e.TipoCuentaDestino ?? e.TipoCuentaOrigen);
                var idIngreso = await mapeo.ResolverRequeridoAsync(e.IdEmpresa, ContabilidadConceptosMapeo.OtrosIngresos);
                var concepto = tipo == "CIERRE_SOBRANTE"
                    ? $"Sobrante cierre caja: {e.Motivo}"
                    : $"Entrada tesorería: {e.Motivo}";

                return new List<ContabilidadIntegracionRequest>
                {
                    CrearRequest(
                        e,
                        ContabilidadConstantes.OrigenBanco,
                        ContabilidadTipoOperacion.Alta,
                        concepto,
                        new ContabilidadIntegracionLinea { IdCuentaContable = idTesoreria, Debito = e.Monto },
                        new ContabilidadIntegracionLinea { IdCuentaContable = idIngreso, Credito = e.Monto })
                };
            }

            if (tipo is "SALIDA" or "CIERRE_FALTANTE")
            {
                var idTesoreria = await mapeo.ResolverTesoreriaAsync(
                    e.IdEmpresa, null, e.TipoCuentaOrigen ?? e.TipoCuentaDestino);
                var idGasto = await mapeo.ResolverRequeridoAsync(e.IdEmpresa, ContabilidadConceptosMapeo.GastoOperativo);
                var concepto = tipo == "CIERRE_FALTANTE"
                    ? $"Faltante cierre caja: {e.Motivo}"
                    : $"Salida tesorería: {e.Motivo}";

                return new List<ContabilidadIntegracionRequest>
                {
                    CrearRequest(
                        e,
                        ContabilidadConstantes.OrigenBanco,
                        ContabilidadTipoOperacion.Alta,
                        concepto,
                        new ContabilidadIntegracionLinea { IdCuentaContable = idGasto, Debito = e.Monto },
                        new ContabilidadIntegracionLinea { IdCuentaContable = idTesoreria, Credito = e.Monto })
                };
            }

            return new List<ContabilidadIntegracionRequest>();
        }

        public static async Task<List<ContabilidadIntegracionRequest>> DesdeInventarioAsync(
            InventarioMovimientoRegistradoEvent e,
            IContabilidadCuentaMapeoService mapeo)
        {
            if (e.Monto <= 0)
                return new List<ContabilidadIntegracionRequest>();

            var motivo = (e.Motivo ?? string.Empty).Trim().ToUpperInvariant();
            // Evitar doble contabilización: compra, venta y NC ya generan asiento en su origen.
            if (motivo is "COMPRA" or "VENTA" or "TRANSFERENCIA" or "DEVOLUCION"
                or "ANULACION_VENTA" or "ANULACION_NC")
                return new List<ContabilidadIntegracionRequest>();

            if (string.Equals(e.TipoMovimiento, "TRANSFERENCIA", StringComparison.OrdinalIgnoreCase))
                return new List<ContabilidadIntegracionRequest>();

            var idInv = await mapeo.ResolverRequeridoAsync(e.IdEmpresa, ContabilidadConceptosMapeo.Inventario);

            if (string.Equals(e.TipoMovimiento, "ENTRADA", StringComparison.OrdinalIgnoreCase))
            {
                var idContra = await mapeo.ResolverRequeridoAsync(e.IdEmpresa, ContabilidadConceptosMapeo.OtrosIngresos);
                return new List<ContabilidadIntegracionRequest>
                {
                    CrearRequest(
                        e,
                        ContabilidadConstantes.OrigenInventario,
                        ContabilidadTipoOperacion.Alta,
                        $"Ajuste inventario entrada: {e.Motivo}",
                        new ContabilidadIntegracionLinea { IdCuentaContable = idInv, Debito = e.Monto },
                        new ContabilidadIntegracionLinea { IdCuentaContable = idContra, Credito = e.Monto })
                };
            }

            if (string.Equals(e.TipoMovimiento, "SALIDA", StringComparison.OrdinalIgnoreCase))
            {
                var idGasto = await mapeo.ResolverRequeridoAsync(e.IdEmpresa, ContabilidadConceptosMapeo.GastoOperativo);
                return new List<ContabilidadIntegracionRequest>
                {
                    CrearRequest(
                        e,
                        ContabilidadConstantes.OrigenInventario,
                        ContabilidadTipoOperacion.Alta,
                        $"Ajuste inventario salida: {e.Motivo}",
                        new ContabilidadIntegracionLinea { IdCuentaContable = idGasto, Debito = e.Monto },
                        new ContabilidadIntegracionLinea { IdCuentaContable = idInv, Credito = e.Monto })
                };
            }

            return new List<ContabilidadIntegracionRequest>();
        }

        private static void CuadrarEnPrimeraLineaDebito(List<ContabilidadIntegracionLinea> lineas)
        {
            var debito = lineas.Sum(l => l.Debito);
            var credito = lineas.Sum(l => l.Credito);
            var diff = debito - credito;
            if (Math.Abs(diff) <= 0.009m || Math.Abs(diff) >= 1m)
                return;

            var primeraDebito = lineas.FirstOrDefault(l => l.Debito > 0);
            if (primeraDebito == null)
                return;

            if (diff < 0)
                primeraDebito.Debito += Math.Abs(diff);
            else
                primeraDebito.Debito = Math.Max(0, primeraDebito.Debito - diff);
        }

        private static ContabilidadIntegracionRequest CrearRequest(
            DomainEventBase e,
            string origenModulo,
            string tipoOperacion,
            string concepto,
            params ContabilidadIntegracionLinea[] lineas)
        {
            return new ContabilidadIntegracionRequest
            {
                IdEmpresa = e.IdEmpresa,
                IdUsuario = e.IdUsuario > 0 ? e.IdUsuario : 1,
                Fecha = e.Fecha == default ? DateTime.Now : e.Fecha,
                Concepto = string.IsNullOrWhiteSpace(concepto) ? origenModulo : concepto.Trim(),
                OrigenModulo = origenModulo,
                OrigenReferenciaId = e.ReferenciaId,
                TipoOperacion = tipoOperacion,
                Lineas = lineas.ToList()
            };
        }
    }
}
