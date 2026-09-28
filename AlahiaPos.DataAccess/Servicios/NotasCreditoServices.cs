using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Dto.Fiscal;
using AlahiaPos.Entities.Events;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace AlahiaPos.DataAccess.Servicios
{
    public class NotasCreditoServices : INotasCredito
    {
        private const int IdTipoDocumentoNotaCredito = 7;

        private readonly IRepository<NotasCredito> _notasCredito;

        private readonly IRepository<NotasCreditoDetalle> _notasCreditoDetalle;

        private readonly IFacturaHeader _facturaHeader;

        private readonly IFacturaDetalle _facturaDetalle;

        private readonly IProductos _productos;

        private readonly INCF_Secuencias _ncfSecuencias;

        private readonly IMovimientosInventarioService _movimientosInventario;

        private readonly IAlmacenes _almacenes;

        private readonly IEmpresas _empresas;

        private readonly ISecuenciaDocumentoService _secuenciaDocumentoService;
        private readonly IFiscalWorkEnqueueService _fiscalEnqueue;
        private readonly IContabilidadEventPublisher _contabilidadEvents;
        private readonly AlahiaPosContext _ctx;
        private readonly IFacturacionElectronicaService _facturacionElectronica;
        private readonly IFiscalFeatureService _fiscalFeatures;

        public NotasCreditoServices(
            IRepository<NotasCredito> notasCredito,
            IRepository<NotasCreditoDetalle> notasCreditoDetalle,
            IFacturaHeader facturaHeader,
            IFacturaDetalle facturaDetalle,
            IProductos productos,
            INCF_Secuencias ncfSecuencias,
            IMovimientosInventarioService movimientosInventario,
            IAlmacenes almacenes,
            IEmpresas empresas,
            ISecuenciaDocumentoService secuenciaDocumentoService,
            IFiscalWorkEnqueueService fiscalEnqueue,
            IContabilidadEventPublisher contabilidadEvents,
            AlahiaPosContext ctx,
            IFacturacionElectronicaService facturacionElectronica,
            IFiscalFeatureService fiscalFeatures)
        {
            _notasCredito = notasCredito;
            _notasCreditoDetalle = notasCreditoDetalle;
            _facturaHeader = facturaHeader;
            _facturaDetalle = facturaDetalle;
            _productos = productos;
            _ncfSecuencias = ncfSecuencias;
            _movimientosInventario = movimientosInventario;
            _almacenes = almacenes;
            _empresas = empresas;
            _secuenciaDocumentoService = secuenciaDocumentoService;
            _fiscalEnqueue = fiscalEnqueue;
            _contabilidadEvents = contabilidadEvents;
            _ctx = ctx;
            _facturacionElectronica = facturacionElectronica;
            _fiscalFeatures = fiscalFeatures;
        }

        public async Task<NotasCreditoResultadoDto> CrearNotaCredito(
            CrearNotaCreditoDto dto)
        {
            if (dto == null)
            {
                throw new Exception("Datos inválidos.");
            }

            if (dto.IdFacturaHeader <= 0)
            {
                throw new Exception("Factura inválida.");
            }

            if (dto.Lineas == null || !dto.Lineas.Any())
            {
                throw new Exception(
                    "Debe seleccionar al menos un producto.");
            }

            var factura =
                await _facturaHeader.GetFacturaHeaderById(
                    dto.IdFacturaHeader,
                    dto.IdEmpresa);

            if (factura == null)
            {
                throw new Exception("Factura no encontrada.");
            }

            if (factura.EstaCancelada)
            {
                throw new Exception(
                    "No se puede devolver una factura anulada.");
            }

            var detallesFactura =
                _facturaDetalle
                    .GetDetalleByIdHeader(dto.IdFacturaHeader)
                    .ToList();

            if (!detallesFactura.Any())
            {
                throw new Exception(
                    "La factura no tiene detalles.");
            }

            var lineasNc = new List<NotasCreditoDetalle>();

            decimal subTotal = 0;
            decimal totalItbis = 0;
            decimal total = 0;

            foreach (var linea in dto.Lineas)
            {
                if (linea.Cantidad <= 0)
                {
                    continue;
                }

                var detalle =
                    detallesFactura.FirstOrDefault(d =>
                        d.IdFacturaDetalle
                            == linea.IdFacturaDetalle);

                if (detalle == null)
                {
                    throw new Exception(
                        "Detalle de factura no encontrado.");
                }

                var disponible =
                    detalle.Cantidad
                    - detalle.CantidadDevuelta;

                if (linea.Cantidad > disponible)
                {
                    throw new Exception(
                        $"Cantidad a devolver mayor a la disponible para el producto.");
                }

                var factor =
                    detalle.Cantidad > 0
                        ? linea.Cantidad / detalle.Cantidad
                        : 0;

                var lineaItbis =
                    Math.Round(detalle.Itbis * factor, 2);

                var lineaSubTotal =
                    Math.Round(detalle.SubTotal * factor, 2);

                var lineaNeto =
                    lineaSubTotal - lineaItbis;

                var precioUnitario =
                    linea.Cantidad > 0
                        ? Math.Round(
                            lineaNeto / linea.Cantidad,
                            2)
                        : 0;

                var producto =
                    _productos.GetProductoById(detalle.IdProducto);

                lineasNc.Add(new NotasCreditoDetalle
                {
                    IdFacturaDetalle = detalle.IdFacturaDetalle,
                    IdProducto = detalle.IdProducto,
                    NombreProducto =
                        producto?.Nombre ?? "Producto",
                    Cantidad = linea.Cantidad,
                    PrecioUnitario = precioUnitario,
                    Itbis = lineaItbis,
                    SubTotal = lineaSubTotal
                });

                detalle.CantidadDevuelta +=
                    linea.Cantidad;

                _facturaDetalle.UpdateFacturaDetalle(
                    detalle.IdFacturaDetalle,
                    detalle);

                subTotal += lineaNeto;
                totalItbis += lineaItbis;
                total += lineaSubTotal;
            }

            if (!lineasNc.Any())
            {
                throw new Exception(
                    "Debe indicar cantidades válidas.");
            }

            var nota = new NotasCredito
            {
                IdFacturaHeader = factura.IdFacturaHeader,
                IdEmpresa = dto.IdEmpresa,
                NCFModificado = factura.NCF ?? "",
                TipoDocumentoOrigen = ExtraerTipoDocumento(factura.NCF),
                IdCliente = factura.IDCliente,
                NombreCliente = factura.NombreEmpresa ?? "",
                RNC = factura.RNC ?? "",
                SubTotal = subTotal,
                TotalItbis = totalItbis,
                Total = total,
                MontoOriginal = total,
                SaldoDisponible = 0,
                Estado = NotaCreditoEstado.Activa,
                Observacion = dto.Observacion ?? "",
                IdUsuario = dto.IdUsuario,
                FechaInseccion = DateTime.Now,
                FechaFacturaOrigen = factura.FechaInseccion
            };

            // Facturación electrónica: el e-NCF (E34) se reserva en emitir-enviar.
            // No generar B04/NCF clásico cuando la factura origen ya es e-CF (E31/E32/…).
            var esDocumentoElectronico = EsEncfElectronico(factura.NCF);
            if (!string.IsNullOrWhiteSpace(factura.NCF) && !esDocumentoElectronico)
            {
                nota.NCF =
                    await GenerarNcfNotaCreditoAsync(
                        dto.IdEmpresa);
            }

            await _notasCredito.Save(nota);

            nota.NumeroDocumento =
                await _secuenciaDocumentoService.GenerarDocumentoAsync(
                    dto.IdEmpresa,
                    IdTipoDocumentoNotaCredito);

            _notasCredito.Update(
                nota.IdNotaCredito,
                nota);

            foreach (var det in lineasNc)
            {
                det.IdNotaCredito = nota.IdNotaCredito;
                await _notasCreditoDetalle.Save(det);
            }

            nota.Detalles = lineasNc;

            factura.MontoNotaCredito += total;
            factura.Clientes = null;
            factura.Empleados = null;
            _facturaHeader.UpdateFacturaHeader(
                factura.IdFacturaHeader,
                factura);

            await CrearMovimientoInventarioDevolucion(
                nota,
                dto.IdUsuario);

            // Contabilidad automática (no-op si Contabilidad apagada)
            try
            {
                decimal costoInventario = 0;
                foreach (var det in lineasNc)
                {
                    var producto = _productos.GetProductoById(det.IdProducto);
                    if (producto == null || producto.EsServicio || producto.PrecioCompra <= 0)
                        continue;
                    costoInventario += producto.PrecioCompra * det.Cantidad;
                }

                // MontoNotaCredito ya incluye esta NC; reconstruir saldo CxC previo.
                var montoNcPrevio = factura.MontoNotaCredito - total;
                var saldoPorCobrarAntes = Math.Max(0m, factura.Total - factura.Pagado - montoNcPrevio);
                var montoCxc = Math.Min(total, saldoPorCobrarAntes);
                var montoTesoreria = Math.Max(0m, total - montoCxc);

                await _contabilidadEvents.TryPublishAsync(new NotaCreditoCreadaEvent
                {
                    IdEmpresa = dto.IdEmpresa,
                    IdUsuario = dto.IdUsuario,
                    Fecha = nota.FechaInseccion == default ? DateTime.Now : nota.FechaInseccion,
                    ReferenciaId = nota.IdNotaCredito,
                    ReferenciaTipo = "NotaCredito",
                    IdFacturaHeader = nota.IdFacturaHeader,
                    NumeroDocumento = nota.NumeroDocumento,
                    Subtotal = nota.SubTotal,
                    Itbis = nota.TotalItbis,
                    Total = nota.Total,
                    MontoCxc = montoCxc,
                    MontoTesoreria = montoTesoreria,
                    CostoInventario = costoInventario
                });
            }
            catch
            {
                // Nunca tumbar NC comercial
            }

            try
            {
                await _fiscalEnqueue.EnqueueFotografiaSiActivoAsync(new FiscalDocumentoRequest
                {
                    IdEmpresa = dto.IdEmpresa,
                    IdUsuario = dto.IdUsuario,
                    ReferenciaId = nota.IdNotaCredito,
                    TipoDocumento = "NotaCredito"
                });
            }
            catch
            {
                // NC comercial ya persistida
            }

            var impacto = await AplicarImpactoFinancieroAsync(
                nota,
                factura,
                dto.IdUsuario);

            await IntentarEmitirE34SiActivoAsync(
                nota,
                dto.IdEmpresa,
                dto.IdUsuario);

            // Recargar para devolver estado fiscal actualizado
            var notaFinal = await _notasCredito.GetByIdAsync(nota.IdNotaCredito)
                ?? nota;

            return MapResultado(
                notaFinal,
                impacto.MontoAplicadoCxc,
                impacto.IdSaldoAFavor,
                "Nota de crédito generada correctamente.");
        }

        public async Task<NotasCreditoResultadoDto> CrearNotaCreditoComercialAsync(
            CrearNotaCreditoComercialDto dto)
        {
            if (dto == null)
                throw new Exception("Datos inválidos.");

            var concepto = (dto.Concepto ?? "").Trim();
            if (string.IsNullOrWhiteSpace(concepto))
                throw new Exception("Debe indicar el concepto / motivo de la nota de crédito.");

            if (dto.Monto <= 0)
                throw new Exception("El monto de la nota de crédito debe ser mayor que cero.");

            FacturaHeaders? factura = null;
            if (dto.IdFacturaHeader.HasValue && dto.IdFacturaHeader.Value > 0)
            {
                factura = await _facturaHeader.GetFacturaHeaderById(
                    dto.IdFacturaHeader.Value,
                    dto.IdEmpresa);

                if (factura == null)
                    throw new Exception("Factura origen no encontrada.");

                if (factura.EstaCancelada)
                    throw new Exception("No se puede asociar una nota de crédito a una factura anulada.");

                if (string.IsNullOrWhiteSpace(factura.NCF))
                    throw new Exception(
                        "La factura no tiene NCF / comprobante fiscal. No se puede generar nota de crédito.");

                var yaTieneNc = await _ctx.NotasCredito
                    .AsNoTracking()
                    .AnyAsync(n =>
                        n.IdEmpresa == dto.IdEmpresa
                        && n.IdFacturaHeader == factura.IdFacturaHeader
                        && n.Estado == NotaCreditoEstado.Activa);

                if (yaTieneNc)
                    throw new Exception(
                        "Esta factura ya tiene una nota de crédito activa. No se puede generar otra.");
            }

            // Con factura e-CF: RNC + nombre comercial vienen del documento (no se exige elegir cliente).
            // Si la factura es a crédito, ya trae IdCliente; se usa para CXC / saldo a favor.
            // Contado e-CF sin cliente de catálogo: solo receptor (RNC + nombre) basta.
            int? idCliente = dto.IdCliente.HasValue && dto.IdCliente.Value > 0
                ? dto.IdCliente.Value
                : null;

            if (factura != null)
            {
                if (!idCliente.HasValue
                    && factura.IDCliente.HasValue
                    && factura.IDCliente.Value > 0)
                {
                    idCliente = factura.IDCliente.Value;
                }
            }
            else if (!idCliente.HasValue)
            {
                throw new Exception("Debe seleccionar el cliente.");
            }

            string nombreCliente;
            string rnc;

            if (factura != null)
            {
                nombreCliente = (factura.NombreEmpresa ?? "").Trim();
                rnc = (factura.RNC ?? "").Trim();
                if (string.IsNullOrWhiteSpace(nombreCliente))
                    throw new Exception("La factura origen no tiene nombre comercial del receptor.");
                if (string.IsNullOrWhiteSpace(rnc))
                    throw new Exception("La factura origen no tiene RNC del receptor.");
            }
            else
            {
                var cliente = await _ctx.Clientes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c =>
                        c.IDCliente == idCliente!.Value
                        && c.IdEmpresa == dto.IdEmpresa)
                    ?? throw new Exception("Cliente no encontrado.");

                nombreCliente = (cliente.NombreComercial ?? "").Trim();
                rnc = (cliente.CedulaRNC ?? "").Trim();
                if (string.IsNullOrWhiteSpace(nombreCliente))
                    throw new Exception("El cliente no tiene nombre comercial.");
            }

            var montoTotal = Math.Round(dto.Monto, 2);
            decimal montoItbis;
            if (dto.MontoItbis.HasValue)
            {
                montoItbis = Math.Round(Math.Max(0, dto.MontoItbis.Value), 2);
                if (montoItbis > montoTotal)
                    throw new Exception("El ITBIS no puede ser mayor que el monto total.");
            }
            else if (factura != null && factura.Total > 0 && factura.TotalItbis > 0)
            {
                var ratio = factura.TotalItbis / factura.Total;
                montoItbis = Math.Round(montoTotal * ratio, 2);
            }
            else
            {
                montoItbis = 0;
            }

            var subTotalNeto = Math.Round(montoTotal - montoItbis, 2);
            var tasa = montoItbis > 0 && subTotalNeto > 0
                ? Math.Round(montoItbis / subTotalNeto * 100m, 2)
                : (decimal?)null;

            var nota = new NotasCredito
            {
                IdFacturaHeader = factura?.IdFacturaHeader ?? 0,
                IdEmpresa = dto.IdEmpresa,
                NCFModificado = factura?.NCF ?? "",
                TipoDocumentoOrigen = ExtraerTipoDocumento(factura?.NCF),
                IdCliente = idCliente,
                NombreCliente = nombreCliente,
                RNC = rnc,
                SubTotal = subTotalNeto,
                TotalItbis = montoItbis,
                Total = montoTotal,
                MontoOriginal = montoTotal,
                SaldoDisponible = 0,
                Estado = NotaCreditoEstado.Activa,
                Observacion = concepto,
                IdUsuario = dto.IdUsuario,
                FechaInseccion = DateTime.Now,
                FechaFacturaOrigen = factura?.FechaInseccion
            };

            var esDocumentoElectronico = EsEncfElectronico(factura?.NCF);
            if (factura != null
                && !string.IsNullOrWhiteSpace(factura.NCF)
                && !esDocumentoElectronico)
            {
                nota.NCF = await GenerarNcfNotaCreditoAsync(dto.IdEmpresa);
            }

            await _notasCredito.Save(nota);

            nota.NumeroDocumento =
                await _secuenciaDocumentoService.GenerarDocumentoAsync(
                    dto.IdEmpresa,
                    IdTipoDocumentoNotaCredito);
            _notasCredito.Update(nota.IdNotaCredito, nota);

            var detalle = new NotasCreditoDetalle
            {
                IdNotaCredito = nota.IdNotaCredito,
                IdFacturaDetalle = 0,
                IdProducto = 0,
                NombreProducto = concepto.Length > 200 ? concepto[..200] : concepto,
                Cantidad = 1,
                PrecioUnitario = subTotalNeto,
                Itbis = montoItbis,
                SubTotal = montoTotal,
                TasaItbis = tasa,
                ItbisCalculado = montoItbis,
                MontoGravadoLinea = montoItbis > 0 ? subTotalNeto : 0,
                MontoExentoLinea = montoItbis > 0 ? 0 : subTotalNeto
            };
            await _notasCreditoDetalle.Save(detalle);
            nota.Detalles = new List<NotasCreditoDetalle> { detalle };

            // Contabilidad (sin costo de inventario: no hay devolución física)
            try
            {
                decimal montoCxcContab = 0;
                decimal montoTesoreriaContab = montoTotal;
                if (factura != null)
                {
                    var saldoPorCobrarAntes = Math.Max(0m, factura.Total - factura.Pagado - factura.MontoNotaCredito);
                    montoCxcContab = Math.Min(montoTotal, saldoPorCobrarAntes);
                    montoTesoreriaContab = Math.Max(0m, montoTotal - montoCxcContab);
                }

                await _contabilidadEvents.TryPublishAsync(new NotaCreditoCreadaEvent
                {
                    IdEmpresa = dto.IdEmpresa,
                    IdUsuario = dto.IdUsuario,
                    Fecha = nota.FechaInseccion == default ? DateTime.Now : nota.FechaInseccion,
                    ReferenciaId = nota.IdNotaCredito,
                    ReferenciaTipo = "NotaCredito",
                    IdFacturaHeader = nota.IdFacturaHeader,
                    NumeroDocumento = nota.NumeroDocumento,
                    Subtotal = nota.SubTotal,
                    Itbis = nota.TotalItbis,
                    Total = nota.Total,
                    MontoCxc = montoCxcContab,
                    MontoTesoreria = montoTesoreriaContab,
                    CostoInventario = 0
                });
            }
            catch
            {
                // Nunca tumbar NC comercial
            }

            decimal montoAplicadoCxc = 0;
            int? idSaldo = null;

            if (factura != null)
            {
                var impacto = await AplicarImpactoFinancieroAsync(
                    nota,
                    factura,
                    dto.IdUsuario);
                montoAplicadoCxc = impacto.MontoAplicadoCxc;
                idSaldo = impacto.IdSaldoAFavor;

                // Cierra la factura origen: anulada por NC (sin inventario; el crédito queda en el recibo).
                var facturaCierre = await _facturaHeader.GetFacturaHeaderById(
                    factura.IdFacturaHeader,
                    dto.IdEmpresa)
                    ?? factura;

                facturaCierre.MontoNotaCredito =
                    Math.Round(facturaCierre.MontoNotaCredito + montoTotal, 2);
                facturaCierre.Pendiente = 0;
                if (!string.Equals(facturaCierre.Estado, "Pagada", StringComparison.OrdinalIgnoreCase))
                    facturaCierre.Estado = "Pagada";
                facturaCierre.EstaCancelada = true;
                facturaCierre.MotivoAnulacion =
                    $"Anulada por nota de crédito {nota.NumeroDocumento}".Trim();
                facturaCierre.Clientes = null;
                facturaCierre.Empleados = null;
                _facturaHeader.UpdateFacturaHeader(
                    facturaCierre.IdFacturaHeader,
                    facturaCierre);
            }
            else
            {
                // Sin factura: todo el monto es saldo a favor (requiere cliente catálogo)
                if (!idCliente.HasValue || idCliente.Value <= 0)
                    throw new Exception("Debe seleccionar el cliente para generar saldo a favor.");

                var saldo = new ClienteSaldoAFavor
                {
                    IdEmpresa = dto.IdEmpresa,
                    IdCliente = idCliente.Value,
                    IdNotaCredito = nota.IdNotaCredito,
                    MontoOriginal = montoTotal,
                    SaldoDisponible = montoTotal,
                    Estado = ClienteSaldoAFavorEstado.Disponible,
                    Fecha = DateTime.Now,
                    IdUsuario = dto.IdUsuario,
                    Observacion = $"Saldo a favor por NC comercial {nota.NumeroDocumento}"
                };
                _ctx.ClienteSaldoAFavor.Add(saldo);
                await _ctx.SaveChangesAsync();
                idSaldo = saldo.IdSaldoAFavor;

                _ctx.NotasCreditoAplicaciones.Add(new NotasCreditoAplicacion
                {
                    IdNotaCredito = nota.IdNotaCredito,
                    IdFacturaHeader = null,
                    IdSaldoAFavor = idSaldo,
                    IdEmpresa = dto.IdEmpresa,
                    MontoAplicado = montoTotal,
                    TipoAplicacion = NotaCreditoTipoAplicacion.SaldoFavor,
                    FechaAplicacion = DateTime.Now,
                    IdUsuario = dto.IdUsuario
                });

                nota.MontoOriginal = montoTotal;
                nota.SaldoDisponible = montoTotal;
                _notasCredito.Update(nota.IdNotaCredito, nota);
                await _ctx.SaveChangesAsync();
            }

            // E34 solo si hay factura origen electrónica (requiere referencia DGII)
            if (factura != null && EsEncfElectronico(factura.NCF))
            {
                await IntentarEmitirE34SiActivoAsync(
                    nota,
                    dto.IdEmpresa,
                    dto.IdUsuario,
                    forzar: false,
                    codigoModificacion: 3);
            }

            var notaFinal = await _notasCredito.GetByIdAsync(nota.IdNotaCredito) ?? nota;

            return MapResultado(
                notaFinal,
                montoAplicadoCxc,
                idSaldo,
                "Nota de crédito comercial generada correctamente.");
        }

        public async Task<bool> ExisteAnticipoPorCitaAsync(int idEmpresa, int idCita)
        {
            var marca = $"cita #{idCita}";
            return await _ctx.NotasCredito.AsNoTracking().AnyAsync(n =>
                n.IdEmpresa == idEmpresa
                && n.Estado == NotaCreditoEstado.Activa
                && n.Observacion != null
                && n.Observacion.Contains(marca));
        }

        public async Task<NotasCreditoResultadoDto> AnularNotaCredito(
            int idNotaCredito,
            int idEmpresa,
            int idUsuario,
            string? motivo)
        {
            var nota = await _notasCredito.GetByIdAsync(idNotaCredito);

            if (nota == null || nota.IdEmpresa != idEmpresa)
            {
                throw new Exception("Nota de crédito no encontrada.");
            }

            if (EsEstadoDgiiAceptado(nota.EstadoDgii))
            {
                throw new Exception(
                    "No se puede anular una nota de crédito con e-CF 34 ya aceptado por DGII.");
            }

            var consumosVenta = await _ctx.NotasCreditoAplicaciones
                .AsNoTracking()
                .Where(a =>
                    a.IdNotaCredito == idNotaCredito
                    && a.TipoAplicacion == NotaCreditoTipoAplicacion.AplicacionVenta)
                .ToListAsync();
            if (consumosVenta.Count > 0)
            {
                throw new Exception(
                    "No se puede anular: el saldo a favor de esta NC ya se usó para pagar una o más facturas.");
            }

            var saldoUsado = await _ctx.ClienteSaldoAFavor
                .AsNoTracking()
                .AnyAsync(s =>
                    s.IdNotaCredito == idNotaCredito
                    && s.IdEmpresa == idEmpresa
                    && s.SaldoDisponible < s.MontoOriginal);
            if (saldoUsado)
            {
                throw new Exception(
                    "No se puede anular: el saldo a favor de esta NC ya fue parcialmente consumido.");
            }

            var detalles =
                (await _notasCreditoDetalle
                    .GetAllByExpresionAsync(d =>
                        d.IdNotaCredito == idNotaCredito))
                .ToList();

            // Restaurar saldo devuelto en la factura origen (CxC).
            var factura =
                await _facturaHeader.GetFacturaHeaderById(
                    nota.IdFacturaHeader,
                    idEmpresa);

            if (factura != null)
            {
                factura.MontoNotaCredito =
                    Math.Max(0m, factura.MontoNotaCredito - nota.Total);

                var aplicacionesCxc = await _ctx.NotasCreditoAplicaciones
                    .AsNoTracking()
                    .Where(a =>
                        a.IdNotaCredito == idNotaCredito
                        && a.TipoAplicacion == NotaCreditoTipoAplicacion.CxcOrigen)
                    .ToListAsync();

                var montoCxc = aplicacionesCxc.Sum(a => a.MontoAplicado);
                if (montoCxc > 0)
                {
                    factura.Pendiente = Math.Round(factura.Pendiente + montoCxc, 2);
                    if (factura.Pendiente > 0
                        && string.Equals(factura.Estado, "Pagada", StringComparison.OrdinalIgnoreCase))
                    {
                        factura.Estado = "Pendiente";
                    }
                }

                factura.Clientes = null;
                factura.Empleados = null;
                _facturaHeader.UpdateFacturaHeader(
                    factura.IdFacturaHeader,
                    factura);

                var detallesFactura =
                    _facturaDetalle
                        .GetDetalleByIdHeader(nota.IdFacturaHeader)
                        .ToList();

                foreach (var det in detalles)
                {
                    var facturaDet = detallesFactura.FirstOrDefault(d =>
                        d.IdFacturaDetalle == det.IdFacturaDetalle);

                    if (facturaDet != null)
                    {
                        facturaDet.CantidadDevuelta =
                            Math.Max(0m, facturaDet.CantidadDevuelta - det.Cantidad);
                        _facturaDetalle.UpdateFacturaDetalle(
                            facturaDet.IdFacturaDetalle,
                            facturaDet);
                    }
                }
            }

            // Anular saldos a favor derivados de esta NC.
            var saldos = await _ctx.ClienteSaldoAFavor
                .Where(s => s.IdNotaCredito == idNotaCredito && s.IdEmpresa == idEmpresa)
                .ToListAsync();
            if (saldos.Count > 0)
                _ctx.ClienteSaldoAFavor.RemoveRange(saldos);

            var aplicaciones = await _ctx.NotasCreditoAplicaciones
                .Where(a => a.IdNotaCredito == idNotaCredito)
                .ToListAsync();
            if (aplicaciones.Count > 0)
                _ctx.NotasCreditoAplicaciones.RemoveRange(aplicaciones);

            await _ctx.SaveChangesAsync();

            // Reverso de inventario: la NC reingresó stock (ENTRADA/DEVOLUCION) → sacarlo (SALIDA).
            try
            {
                await RevertirMovimientoInventarioDevolucion(nota, detalles, idUsuario);
            }
            catch
            {
                // No bloquear anulación por stock insuficiente.
            }

            // Contabilidad: reverso del asiento de la NC (no-op si Contabilidad apagada).
            try
            {
                await _contabilidadEvents.TryPublishAsync(new NotaCreditoAnuladaEvent
                {
                    IdEmpresa = idEmpresa,
                    IdUsuario = idUsuario,
                    Fecha = DateTime.Now,
                    ReferenciaId = nota.IdNotaCredito,
                    ReferenciaTipo = "NotaCredito",
                    Motivo = motivo,
                    NumeroDocumento = nota.NumeroDocumento
                });
            }
            catch
            {
                // Nunca tumbar anulación comercial.
            }

            // El modelo no tiene estado de anulación; se elimina la NC y su detalle.
            foreach (var det in detalles)
            {
                _notasCreditoDetalle.Delete(det.IdNotaCreditoDetalle);
            }

            _notasCredito.Delete(nota.IdNotaCredito);

            return new NotasCreditoResultadoDto
            {
                IdNotaCredito = nota.IdNotaCredito,
                NumeroDocumento = nota.NumeroDocumento ?? "",
                NCF = nota.NCF ?? "",
                Total = nota.Total,
                Mensaje = "Nota de crédito anulada correctamente."
            };
        }

        public async Task<TicketNotaCreditoDto?> GetTicketNotaCredito(
            int idNotaCredito,
            int idEmpresa)
        {
            var nota =
                await _notasCredito.GetByIdAsync(idNotaCredito);

            if (nota == null || nota.IdEmpresa != idEmpresa)
            {
                return null;
            }

            var detalles =
                (await _notasCreditoDetalle
                    .GetAllByExpresionAsync(d =>
                        d.IdNotaCredito == idNotaCredito))
                .ToList();

            var factura =
                await _facturaHeader.GetFacturaHeaderById(
                    nota.IdFacturaHeader,
                    idEmpresa);

            var empresa =
                await _empresas.GetEmpresaById(idEmpresa);

            string? securityCode = null;
            string? urlQr = null;
            string? rncEmisor = null;
            string? razonEmisor = null;

            if (!string.IsNullOrWhiteSpace(nota.TrackId)
                || !string.IsNullOrWhiteSpace(nota.NCF))
            {
                var ecf = await _ctx.ECFEncabezados
                    .AsNoTracking()
                    .Where(e => e.IdEmpresa == idEmpresa
                        && (
                            (!string.IsNullOrWhiteSpace(nota.TrackId) && e.TrackId == nota.TrackId)
                            || (!string.IsNullOrWhiteSpace(nota.NCF) && e.ENCF == nota.NCF)
                        ))
                    .OrderByDescending(e => e.IdECF)
                    .FirstOrDefaultAsync();

                if (ecf != null)
                {
                    securityCode = ecf.SecurityCode;
                    urlQr = ecf.UrlQR;
                    rncEmisor = ecf.RncEmisor;
                    razonEmisor = empresa?.NombreComercial;
                    if (string.IsNullOrWhiteSpace(nota.EstadoDgii) && !string.IsNullOrWhiteSpace(ecf.EstadoDGII))
                        nota.EstadoDgii = ecf.EstadoDGII;
                }
            }

            return new TicketNotaCreditoDto
            {
                IdNotaCredito = nota.IdNotaCredito,
                NumeroDocumento = nota.NumeroDocumento ?? "",
                NCF = nota.NCF ?? "",
                NCFModificado = nota.NCFModificado ?? "",
                NumeroFactura =
                    factura?.NumeroDocumento
                    ?? $"#{nota.IdFacturaHeader}",
                Fecha = nota.FechaInseccion,
                Cliente = nota.NombreCliente ?? "Cliente",
                RNC = nota.RNC ?? "",
                SubTotal = nota.SubTotal,
                TotalItbis = nota.TotalItbis,
                Total = nota.Total,
                NombreEmpresa =
                    razonEmisor
                    ?? empresa?.NombreComercial ?? "",
                TelefonoEmpresa = empresa?.Telefono ?? "",
                DireccionEmpresa = empresa?.Direccion ?? "",
                TrackId = nota.TrackId,
                EstadoDgii = nota.EstadoDgii,
                TipoDocumentoOrigen = nota.TipoDocumentoOrigen,
                SaldoDisponible = nota.SaldoDisponible,
                EmisionPendiente = EsEmisionPendiente(nota),
                MensajeEmision = nota.MensajeEmision,
                FechaEmisionEcf = nota.FechaEmisionEcf,
                SecurityCode = securityCode,
                UrlQR = urlQr,
                RncEmisor = rncEmisor,
                Detalles = detalles.Select(d =>
                    new TicketNotaCreditoDetalleDto
                    {
                        Cantidad = d.Cantidad,
                        Descripcion = d.NombreProducto ?? "",
                        Precio = d.PrecioUnitario,
                        SubTotal = d.SubTotal
                    }).ToList()
            };
        }

        public async Task<IEnumerable<NotaCreditoListadoDto>> ListarNotasCredito(
            int idEmpresa,
            DateTime? desde,
            DateTime? hasta,
            bool soloConComprobante)
        {
            var notas =
                (await _notasCredito.GetAllByExpresionAsync(n =>
                    n.IdEmpresa == idEmpresa))
                .AsEnumerable();

            if (desde.HasValue)
            {
                var desdeDate = desde.Value.Date;
                notas = notas.Where(n =>
                    n.FechaInseccion.Date >= desdeDate);
            }

            if (hasta.HasValue)
            {
                var hastaDate = hasta.Value.Date;
                notas = notas.Where(n =>
                    n.FechaInseccion.Date <= hastaDate);
            }

            if (soloConComprobante)
            {
                notas = notas.Where(n =>
                    !string.IsNullOrWhiteSpace(n.NCF));
            }

            var lista = new List<NotaCreditoListadoDto>();

            foreach (var nota in notas.OrderByDescending(n => n.IdNotaCredito))
            {
                var factura =
                    await _facturaHeader.GetFacturaHeaderById(
                        nota.IdFacturaHeader,
                        idEmpresa);

                var detalles =
                    (await _notasCreditoDetalle
                        .GetAllByExpresionAsync(d =>
                            d.IdNotaCredito == nota.IdNotaCredito))
                    .ToList();

                lista.Add(new NotaCreditoListadoDto
                {
                    IdNotaCredito = nota.IdNotaCredito,
                    NumeroDocumento = nota.NumeroDocumento ?? "",
                    NCF = nota.NCF ?? "",
                    NCFModificado = nota.NCFModificado ?? "",
                    IdFacturaHeader = nota.IdFacturaHeader,
                    NumeroFactura =
                        factura?.NumeroDocumento
                        ?? $"Fact-{nota.IdFacturaHeader}",
                    NombreCliente = nota.NombreCliente ?? "",
                    RNC = nota.RNC ?? "",
                    SubTotal = nota.SubTotal,
                    TotalItbis = nota.TotalItbis,
                    Total = nota.Total,
                    Observacion = nota.Observacion ?? "",
                    FechaInseccion = nota.FechaInseccion,
                    CantidadProductos = detalles.Count,
                    ProductosDevueltos = string.Join(
                        ", ",
                        detalles.Select(d =>
                            $"{d.NombreProducto} (x{d.Cantidad:0.##})")),
                    TieneComprobante =
                        !string.IsNullOrWhiteSpace(nota.NCF),
                    TrackId = nota.TrackId,
                    EstadoDgii = nota.EstadoDgii,
                    TipoDocumentoOrigen = nota.TipoDocumentoOrigen,
                    SaldoDisponible = nota.SaldoDisponible,
                    Estado = nota.Estado ?? NotaCreditoEstado.Activa,
                    EmisionPendiente = EsEmisionPendiente(nota),
                    FechaEmisionEcf = nota.FechaEmisionEcf
                });
            }

            return lista;
        }

        public async Task<NotasCreditoResultadoDto> ReintentarEmisionAsync(
            int idNotaCredito,
            int idEmpresa,
            int idUsuario)
        {
            var nota = await _notasCredito.GetByIdAsync(idNotaCredito);
            if (nota == null || nota.IdEmpresa != idEmpresa)
                throw new Exception("Nota de crédito no encontrada.");

            if (!EsEmisionPendiente(nota) && !string.IsNullOrWhiteSpace(nota.NCF))
            {
                throw new Exception("La nota de crédito ya tiene e-CF emitido.");
            }

            var flags = await _fiscalFeatures.GetFeaturesAsync(idEmpresa);
            if (!flags.FacturacionElectronicaActiva)
                throw new Exception("Facturación electrónica no está activa para esta empresa.");

            await IntentarEmitirE34SiActivoAsync(nota, idEmpresa, idUsuario, forzar: true);

            var notaFinal = await _notasCredito.GetByIdAsync(idNotaCredito) ?? nota;
            var saldoId = await _ctx.ClienteSaldoAFavor
                .AsNoTracking()
                .Where(s => s.IdNotaCredito == idNotaCredito)
                .Select(s => (int?)s.IdSaldoAFavor)
                .FirstOrDefaultAsync();

            var montoCxc = await _ctx.NotasCreditoAplicaciones
                .AsNoTracking()
                .Where(a =>
                    a.IdNotaCredito == idNotaCredito
                    && a.TipoAplicacion == NotaCreditoTipoAplicacion.CxcOrigen)
                .SumAsync(a => (decimal?)a.MontoAplicado) ?? 0m;

            return MapResultado(
                notaFinal,
                montoCxc,
                saldoId,
                EsEmisionPendiente(notaFinal)
                    ? "Reintento registrado; emisión aún pendiente."
                    : "Emisión e-CF 34 reintentada correctamente.");
        }

        public async Task<IEnumerable<ClienteSaldoAFavorListadoDto>> ListarSaldosAFavorAsync(
            int idEmpresa,
            int? idCliente = null)
        {
            await AsegurarSaldosFaltantesAsync(idEmpresa);

            var query =
                from s in _ctx.ClienteSaldoAFavor.AsNoTracking()
                join n in _ctx.NotasCredito.AsNoTracking() on s.IdNotaCredito equals n.IdNotaCredito
                join f in _ctx.FacturaHeaders.AsNoTracking() on n.IdFacturaHeader equals f.IdFacturaHeader into fj
                from f in fj.DefaultIfEmpty()
                where s.IdEmpresa == idEmpresa
                select new { s, n, f };

            if (idCliente.HasValue && idCliente.Value > 0)
                query = query.Where(x => x.s.IdCliente == idCliente.Value);

            var rows = await query
                .OrderByDescending(x => x.s.IdSaldoAFavor)
                .ToListAsync();

            return rows.Select(x => MapSaldoListado(x.s, x.n, x.f)).ToList();
        }

        public async Task<ClienteSaldoAFavorListadoDto?> ObtenerSaldoAFavorPorNumeroAsync(
            int idEmpresa,
            int idCliente,
            string numero)
        {
            var key = NormalizarNumeroNc(numero);
            if (string.IsNullOrWhiteSpace(key))
                throw new Exception("Indique el e-NCF o el número de la nota de crédito.");

            await AsegurarSaldosFaltantesAsync(idEmpresa);

            var notasQuery = _ctx.NotasCredito
                .AsNoTracking()
                .Where(n =>
                    n.IdEmpresa == idEmpresa
                    && n.Estado == NotaCreditoEstado.Activa);
            if (idCliente > 0)
                notasQuery = notasQuery.Where(n => n.IdCliente == idCliente);

            var notas = await notasQuery.ToListAsync();

            var nota = notas.FirstOrDefault(n =>
                NormalizarNumeroNc(n.NCF) == key
                || NormalizarNumeroNc(n.NumeroDocumento) == key
                || NormalizarNumeroNc(n.NCFModificado) == key);

            if (nota == null)
                throw new Exception("No se encontró una nota de crédito activa con ese número.");

            var saldoQuery = _ctx.ClienteSaldoAFavor
                .AsNoTracking()
                .Where(s =>
                    s.IdEmpresa == idEmpresa
                    && s.IdNotaCredito == nota.IdNotaCredito
                    && s.Estado == ClienteSaldoAFavorEstado.Disponible
                    && s.SaldoDisponible > 0);
            if (idCliente > 0)
                saldoQuery = saldoQuery.Where(s => s.IdCliente == idCliente);

            var saldo = await saldoQuery
                .OrderByDescending(s => s.IdSaldoAFavor)
                .FirstOrDefaultAsync();

            if (saldo == null)
            {
                throw new Exception(
                    "Esa nota de crédito no tiene saldo a favor disponible " +
                    "(pudo haberse aplicado a CxC o ya consumirse).");
            }

            FacturaHeaders? factura = null;
            if (nota.IdFacturaHeader > 0)
                factura = await _ctx.FacturaHeaders.AsNoTracking()
                    .FirstOrDefaultAsync(f => f.IdFacturaHeader == nota.IdFacturaHeader);

            return MapSaldoListado(saldo, nota, factura);
        }

        public async Task ConsumirSaldoAFavorEnVentaAsync(
            int idEmpresa,
            int idFacturaHeader,
            int idCliente,
            decimal monto,
            int? idSaldoAFavor,
            int? idNotaCredito,
            string? ncfONumero,
            int? idUsuario)
        {
            monto = Math.Round(monto, 2);
            if (monto <= 0)
                throw new Exception("El monto de la nota de crédito debe ser mayor que cero.");

            await AsegurarSaldosFaltantesAsync(idEmpresa);

            // Idempotencia: misma factura + mismo saldo ya aplicado
            if (idSaldoAFavor.HasValue && idSaldoAFavor.Value > 0)
            {
                var ya = await _ctx.NotasCreditoAplicaciones
                    .AsNoTracking()
                    .AnyAsync(a =>
                        a.IdEmpresa == idEmpresa
                        && a.IdFacturaHeader == idFacturaHeader
                        && a.IdSaldoAFavor == idSaldoAFavor.Value
                        && a.TipoAplicacion == NotaCreditoTipoAplicacion.AplicacionVenta);
                if (ya)
                    return;
            }

            ClienteSaldoAFavor? saldo = null;

            if (idSaldoAFavor.HasValue && idSaldoAFavor.Value > 0)
            {
                saldo = await _ctx.ClienteSaldoAFavor
                    .FirstOrDefaultAsync(s =>
                        s.IdSaldoAFavor == idSaldoAFavor.Value
                        && s.IdEmpresa == idEmpresa);
            }
            else if (idNotaCredito.HasValue && idNotaCredito.Value > 0)
            {
                saldo = await _ctx.ClienteSaldoAFavor
                    .Where(s =>
                        s.IdEmpresa == idEmpresa
                        && s.IdNotaCredito == idNotaCredito.Value)
                    .OrderByDescending(s => s.IdSaldoAFavor)
                    .FirstOrDefaultAsync();
            }
            else if (!string.IsNullOrWhiteSpace(ncfONumero))
            {
                var dto = await ObtenerSaldoAFavorPorNumeroAsync(idEmpresa, idCliente, ncfONumero);
                saldo = await _ctx.ClienteSaldoAFavor
                    .FirstOrDefaultAsync(s => s.IdSaldoAFavor == dto!.IdSaldoAFavor);
            }

            if (saldo == null)
                throw new Exception("No se encontró el saldo a favor de la nota de crédito.");

            if (!string.Equals(saldo.Estado, ClienteSaldoAFavorEstado.Disponible, StringComparison.OrdinalIgnoreCase)
                || saldo.SaldoDisponible <= 0)
            {
                throw new Exception("El saldo a favor de esa nota de crédito no está disponible.");
            }

            if (monto > saldo.SaldoDisponible)
            {
                throw new Exception(
                    $"El monto ({monto:N2}) supera el saldo disponible ({saldo.SaldoDisponible:N2}) de la NC.");
            }

            var nota = await _notasCredito.GetByIdAsync(saldo.IdNotaCredito);
            if (nota == null || nota.IdEmpresa != idEmpresa)
                throw new Exception("Nota de crédito no encontrada.");

            if (!string.Equals(nota.Estado, NotaCreditoEstado.Activa, StringComparison.OrdinalIgnoreCase))
                throw new Exception("La nota de crédito no está activa.");

            saldo.SaldoDisponible = Math.Round(saldo.SaldoDisponible - monto, 2);
            if (saldo.SaldoDisponible <= 0)
            {
                saldo.SaldoDisponible = 0;
                saldo.Estado = ClienteSaldoAFavorEstado.Agotado;
            }

            nota.SaldoDisponible = Math.Round(Math.Max(0, nota.SaldoDisponible - monto), 2);

            _ctx.NotasCreditoAplicaciones.Add(new NotasCreditoAplicacion
            {
                IdNotaCredito = nota.IdNotaCredito,
                IdFacturaHeader = idFacturaHeader,
                IdSaldoAFavor = saldo.IdSaldoAFavor,
                IdEmpresa = idEmpresa,
                MontoAplicado = monto,
                TipoAplicacion = NotaCreditoTipoAplicacion.AplicacionVenta,
                FechaAplicacion = DateTime.Now,
                IdUsuario = idUsuario
            });

            await _ctx.SaveChangesAsync();
        }

        public async Task RevertirConsumosSaldoPorFacturaAsync(
            int idEmpresa,
            int idFacturaHeader,
            int? idUsuario)
        {
            var apps = await _ctx.NotasCreditoAplicaciones
                .Where(a =>
                    a.IdEmpresa == idEmpresa
                    && a.IdFacturaHeader == idFacturaHeader
                    && a.TipoAplicacion == NotaCreditoTipoAplicacion.AplicacionVenta)
                .ToListAsync();

            if (apps.Count == 0)
                return;

            foreach (var app in apps)
            {
                if (app.IdSaldoAFavor.HasValue)
                {
                    var saldo = await _ctx.ClienteSaldoAFavor
                        .FirstOrDefaultAsync(s =>
                            s.IdSaldoAFavor == app.IdSaldoAFavor.Value
                            && s.IdEmpresa == idEmpresa);

                    if (saldo != null
                        && !string.Equals(saldo.Estado, ClienteSaldoAFavorEstado.Anulado, StringComparison.OrdinalIgnoreCase))
                    {
                        saldo.SaldoDisponible = Math.Round(saldo.SaldoDisponible + app.MontoAplicado, 2);
                        if (saldo.SaldoDisponible > 0)
                            saldo.Estado = ClienteSaldoAFavorEstado.Disponible;
                    }
                }

                var nota = await _notasCredito.GetByIdAsync(app.IdNotaCredito);
                if (nota != null && nota.IdEmpresa == idEmpresa)
                {
                    nota.SaldoDisponible = Math.Round(nota.SaldoDisponible + app.MontoAplicado, 2);
                }
            }

            _ctx.NotasCreditoAplicaciones.RemoveRange(apps);
            await _ctx.SaveChangesAsync();
        }

        private static string NormalizarNumeroNc(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return "";
            return new string(valor.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
        }

        private async Task AsegurarSaldosFaltantesAsync(int idEmpresa)
        {
            var ncSinSaldo = await (
                from n in _ctx.NotasCredito
                where n.IdEmpresa == idEmpresa
                    && n.Estado == NotaCreditoEstado.Activa
                    && n.SaldoDisponible > 0
                    && !_ctx.ClienteSaldoAFavor.Any(s => s.IdNotaCredito == n.IdNotaCredito)
                select n
            ).ToListAsync();

            foreach (var n in ncSinSaldo)
            {
                _ctx.ClienteSaldoAFavor.Add(new ClienteSaldoAFavor
                {
                    IdEmpresa = idEmpresa,
                    IdCliente = n.IdCliente ?? 0,
                    IdNotaCredito = n.IdNotaCredito,
                    MontoOriginal = n.SaldoDisponible,
                    SaldoDisponible = n.SaldoDisponible,
                    Estado = ClienteSaldoAFavorEstado.Disponible,
                    Fecha = DateTime.Now,
                    Observacion = $"Saldo a favor por NC {n.NumeroDocumento}"
                });
            }

            if (ncSinSaldo.Count > 0)
                await _ctx.SaveChangesAsync();
        }

        private static ClienteSaldoAFavorListadoDto MapSaldoListado(
            ClienteSaldoAFavor saldo,
            NotasCredito nota,
            FacturaHeaders? factura)
        {
            var ncfFactura = !string.IsNullOrWhiteSpace(nota.NCFModificado)
                ? nota.NCFModificado
                : factura?.NCF;
            var numeroFactura = !string.IsNullOrWhiteSpace(factura?.NumeroDocumento)
                ? factura!.NumeroDocumento
                : (nota.IdFacturaHeader > 0 ? $"#{nota.IdFacturaHeader}" : null);

            return new ClienteSaldoAFavorListadoDto
            {
                IdSaldoAFavor = saldo.IdSaldoAFavor,
                IdCliente = saldo.IdCliente,
                NombreCliente = string.IsNullOrWhiteSpace(nota.NombreCliente)
                    ? "Consumo / portador"
                    : nota.NombreCliente,
                IdNotaCredito = saldo.IdNotaCredito,
                IdFacturaHeader = nota.IdFacturaHeader,
                NcfFacturaOrigen = ncfFactura,
                NumeroFactura = numeroFactura,
                NcfNotaCredito = nota.NCF,
                NumeroDocumentoNotaCredito = nota.NumeroDocumento,
                MontoOriginal = saldo.MontoOriginal,
                SaldoDisponible = saldo.SaldoDisponible,
                Estado = saldo.Estado,
                Fecha = saldo.Fecha,
                Observacion = saldo.Observacion
            };
        }

        private async Task<(decimal MontoAplicadoCxc, int? IdSaldoAFavor)> AplicarImpactoFinancieroAsync(
            NotasCredito nota,
            FacturaHeaders factura,
            int idUsuario)
        {
            decimal montoCxc = 0m;
            int? idSaldo = null;
            var esCredito = string.Equals(
                factura.TipoFactura,
                "Credito",
                StringComparison.OrdinalIgnoreCase);

            if (esCredito && factura.Pendiente > 0)
            {
                montoCxc = Math.Min(nota.Total, factura.Pendiente);
                factura.Pendiente = Math.Round(factura.Pendiente - montoCxc, 2);
                if (factura.Pendiente <= 0)
                {
                    factura.Pendiente = 0;
                    factura.Estado = "Pagada";
                }

                factura.Clientes = null;
                factura.Empleados = null;
                _facturaHeader.UpdateFacturaHeader(factura.IdFacturaHeader, factura);

                _ctx.NotasCreditoAplicaciones.Add(new NotasCreditoAplicacion
                {
                    IdNotaCredito = nota.IdNotaCredito,
                    IdFacturaHeader = factura.IdFacturaHeader,
                    IdEmpresa = nota.IdEmpresa,
                    MontoAplicado = montoCxc,
                    TipoAplicacion = NotaCreditoTipoAplicacion.CxcOrigen,
                    FechaAplicacion = DateTime.Now,
                    IdUsuario = idUsuario
                });
            }

            var remanente = Math.Round(nota.Total - montoCxc, 2);
            if (remanente > 0)
            {
                var saldo = new ClienteSaldoAFavor
                {
                    IdEmpresa = nota.IdEmpresa,
                    IdCliente = nota.IdCliente ?? factura.IDCliente ?? 0,
                    IdNotaCredito = nota.IdNotaCredito,
                    MontoOriginal = remanente,
                    SaldoDisponible = remanente,
                    Estado = ClienteSaldoAFavorEstado.Disponible,
                    Fecha = DateTime.Now,
                    IdUsuario = idUsuario,
                    Observacion = $"Saldo a favor por NC {nota.NumeroDocumento}"
                };
                _ctx.ClienteSaldoAFavor.Add(saldo);
                await _ctx.SaveChangesAsync();
                idSaldo = saldo.IdSaldoAFavor;

                _ctx.NotasCreditoAplicaciones.Add(new NotasCreditoAplicacion
                {
                    IdNotaCredito = nota.IdNotaCredito,
                    IdFacturaHeader = factura.IdFacturaHeader,
                    IdSaldoAFavor = idSaldo,
                    IdEmpresa = nota.IdEmpresa,
                    MontoAplicado = remanente,
                    TipoAplicacion = NotaCreditoTipoAplicacion.SaldoFavor,
                    FechaAplicacion = DateTime.Now,
                    IdUsuario = idUsuario
                });
            }

            nota.MontoOriginal = nota.Total;
            nota.SaldoDisponible = remanente;
            _notasCredito.Update(nota.IdNotaCredito, nota);
            await _ctx.SaveChangesAsync();

            return (montoCxc, idSaldo);
        }

        private async Task IntentarEmitirE34SiActivoAsync(
            NotasCredito nota,
            int idEmpresa,
            int idUsuario,
            bool forzar = false,
            int codigoModificacion = 1)
        {
            var flags = await _fiscalFeatures.GetFeaturesAsync(idEmpresa);
            if (!flags.FacturacionElectronicaActiva)
                return;

            try
            {
                var resultado = await _facturacionElectronica.EmitirYEnviarAsync(
                    new EmisionEcfRequest
                    {
                        IdEmpresa = idEmpresa,
                        TipoEcfDgii = 34,
                        OrigenDocumento = OrigenDocumento.NotaCredito,
                        IdOrigen = nota.IdNotaCredito,
                        IdUsuario = idUsuario,
                        IdSucursal = nota.IdSucursal,
                        NcfModificado = nota.NCFModificado,
                        FechaNcfModificado = nota.FechaFacturaOrigen,
                        CodigoModificacion = codigoModificacion,
                        RazonModificacion = string.IsNullOrWhiteSpace(nota.Observacion)
                            ? (codigoModificacion == 3 ? "Correccion de montos" : "Devolucion")
                            : nota.Observacion
                    });

                await AplicarResultadoEmisionAsync(nota.IdNotaCredito, resultado);
            }
            catch (Exception ex)
            {
                await MarcarEmisionPendienteAsync(
                    nota.IdNotaCredito,
                    forzar ? $"Reintento fallido: {ex.Message}" : ex.Message);
            }
        }

        private async Task AplicarResultadoEmisionAsync(
            int idNotaCredito,
            EmisionEcfResultadoCompleto resultado)
        {
            var tracked = await _ctx.NotasCredito
                .AsTracking()
                .FirstOrDefaultAsync(n => n.IdNotaCredito == idNotaCredito);
            if (tracked == null)
                return;

            if (!string.IsNullOrWhiteSpace(resultado.Encf))
                tracked.NCF = resultado.Encf;

            tracked.IdEcf = resultado.IdEcf;
            tracked.TrackId = resultado.TrackId;
            tracked.EstadoDgii = resultado.EstadoDgii
                ?? (resultado.Exitoso ? "Enviado" : "Error");
            tracked.FechaEmisionEcf = DateTime.Now;
            tracked.CodigoTipoComprobanteDgii = "34";
            tracked.MensajeEmision = (resultado.MensajesDgii != null && resultado.MensajesDgii.Count > 0)
                ? string.Join("; ", resultado.MensajesDgii)
                : (resultado.Exitoso ? null : resultado.MensajeError);

            if (!resultado.Exitoso && string.IsNullOrWhiteSpace(tracked.EstadoDgii))
                tracked.EstadoDgii = "Pendiente";

            if (!resultado.Exitoso && string.IsNullOrWhiteSpace(tracked.NCF))
                tracked.EstadoDgii = "Pendiente";

            await _ctx.SaveChangesAsync();
        }

        private static NotasCreditoResultadoDto MapResultado(
            NotasCredito nota,
            decimal montoCxc,
            int? idSaldoAFavor,
            string mensajeBase)
        {
            var pendiente = EsEmisionPendiente(nota);
            var mensaje = mensajeBase;
            if (pendiente)
            {
                mensaje = string.IsNullOrWhiteSpace(nota.MensajeEmision)
                    ? $"{mensajeBase} Emisión e-CF 34 pendiente de reintento."
                    : $"{mensajeBase} Emisión e-CF pendiente: {nota.MensajeEmision}";
            }

            return new NotasCreditoResultadoDto
            {
                IdNotaCredito = nota.IdNotaCredito,
                NumeroDocumento = nota.NumeroDocumento ?? "",
                NCF = nota.NCF ?? "",
                Total = nota.Total,
                Mensaje = mensaje,
                TrackId = nota.TrackId,
                EstadoDgii = nota.EstadoDgii,
                EmisionPendiente = pendiente,
                MensajeEmision = nota.MensajeEmision,
                SaldoDisponible = nota.SaldoDisponible,
                IdSaldoAFavor = idSaldoAFavor,
                MontoAplicadoCxc = montoCxc
            };
        }

        private static bool EsEmisionPendiente(NotasCredito nota)
        {
            var estado = (nota.EstadoDgii ?? "").Trim();
            if (estado.Equals("Pendiente", StringComparison.OrdinalIgnoreCase)
                || estado.Equals("Error", StringComparison.OrdinalIgnoreCase)
                || estado.Equals("Rechazado", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return string.IsNullOrWhiteSpace(nota.NCF)
                && !string.IsNullOrWhiteSpace(nota.MensajeEmision);
        }

        private async Task MarcarEmisionPendienteAsync(int idNotaCredito, string mensaje)
        {
            var tracked = await _ctx.NotasCredito
                .AsTracking()
                .FirstOrDefaultAsync(n => n.IdNotaCredito == idNotaCredito);
            if (tracked == null)
                return;

            tracked.EstadoDgii = "Pendiente";
            tracked.MensajeEmision = Truncate(mensaje, 1000);
            tracked.CodigoTipoComprobanteDgii = "34";
            await _ctx.SaveChangesAsync();
        }

        private static bool EsEstadoDgiiAceptado(string? estado)
        {
            if (string.IsNullOrWhiteSpace(estado))
                return false;
            estado = estado.Trim();
            return estado.Equals("Aceptado", StringComparison.OrdinalIgnoreCase)
                || estado.Equals("AceptadoCondicional", StringComparison.OrdinalIgnoreCase)
                || estado.Contains("Aceptado", StringComparison.OrdinalIgnoreCase);
        }

        private static string? ExtraerTipoDocumento(string? ncf)
        {
            if (string.IsNullOrWhiteSpace(ncf) || ncf.Length < 3)
                return null;
            return ncf.Trim()[..3].ToUpperInvariant();
        }

        private static string Truncate(string? value, int max)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return value.Length <= max ? value : value[..max];
        }

        /// <summary>
        /// e-NCF electrónicos DGII: E31, E32, E33, E34, …
        /// </summary>
        private static bool EsEncfElectronico(string? ncf)
        {
            if (string.IsNullOrWhiteSpace(ncf)) return false;
            ncf = ncf.Trim();
            return ncf.Length >= 3
                && ncf.StartsWith("E", StringComparison.OrdinalIgnoreCase)
                && char.IsDigit(ncf[1])
                && char.IsDigit(ncf[2]);
        }

        private async Task<string> GenerarNcfNotaCreditoAsync(
            int idEmpresa)
        {
            var tiposIntento = new[]
            {
                "Nota de Crédito",
                "B04"
            };

            Exception? ultimoError = null;

            foreach (var tipo in tiposIntento)
            {
                try
                {
                    return await _ncfSecuencias.GenerarNCF(
                        idEmpresa,
                        tipo);
                }
                catch (Exception ex)
                {
                    ultimoError = ex;
                }
            }

            throw ultimoError
                ?? new Exception(
                    "No se pudo generar el comprobante de nota de crédito.");
        }

        private async Task CrearMovimientoInventarioDevolucion(
            NotasCredito nota,
            int idUsuario)
        {
            var almacen =
                await _almacenes.GetAlmacenPrincipal(
                    nota.IdEmpresa,
                    nota.IdSucursal);

            if (almacen == null)
            {
                return;
            }

            var movimiento = new MovimientosInventario
            {
                TipoMovimiento = "ENTRADA",
                Motivo = "DEVOLUCION",
                Referencia =
                    $"NC {nota.NumeroDocumento}",
                Observacion =
                    $"Devolución factura #{nota.IdFacturaHeader}",
                Fecha = DateTime.Now,
                IdEmpresa = nota.IdEmpresa,
                IdSucursal = nota.IdSucursal,
                IdUsuario = idUsuario,
                IdAlmacen = almacen.IdAlmacen,
                Activo = true,
                Detalles = new List<MovimientosInventarioDetalle>()
            };

            foreach (var det in nota.Detalles)
            {
                var producto =
                    _productos.GetProductoById(det.IdProducto);

                if (producto == null || producto.EsServicio)
                {
                    continue;
                }

                if (!producto.ControlarStock)
                {
                    continue;
                }

                movimiento.Detalles.Add(
                    new MovimientosInventarioDetalle
                    {
                        IdProducto = det.IdProducto,
                        Cantidad = det.Cantidad,
                        Precio = det.PrecioUnitario,
                        SubTotal = det.SubTotal,
                        Observacion =
                            $"Devolución NC {nota.NumeroDocumento}"
                    });
            }

            if (movimiento.Detalles.Any())
            {
                await _movimientosInventario
                    .GuardarMovimiento(movimiento);
            }
        }

        private async Task RevertirMovimientoInventarioDevolucion(
            NotasCredito nota,
            List<NotasCreditoDetalle> detalles,
            int idUsuario)
        {
            var almacen =
                await _almacenes.GetAlmacenPrincipal(
                    nota.IdEmpresa,
                    nota.IdSucursal);

            if (almacen == null)
            {
                return;
            }

            var movimiento = new MovimientosInventario
            {
                TipoMovimiento = "SALIDA",
                Motivo = "ANULACION_NC",
                Referencia =
                    $"Anulación NC {nota.NumeroDocumento}",
                Observacion =
                    $"Anulación devolución factura #{nota.IdFacturaHeader}",
                Fecha = DateTime.Now,
                IdEmpresa = nota.IdEmpresa,
                IdSucursal = nota.IdSucursal,
                IdUsuario = idUsuario,
                IdAlmacen = almacen.IdAlmacen,
                Activo = true,
                Detalles = new List<MovimientosInventarioDetalle>()
            };

            foreach (var det in detalles)
            {
                var producto =
                    _productos.GetProductoById(det.IdProducto);

                if (producto == null || producto.EsServicio)
                {
                    continue;
                }

                if (!producto.ControlarStock)
                {
                    continue;
                }

                movimiento.Detalles.Add(
                    new MovimientosInventarioDetalle
                    {
                        IdProducto = det.IdProducto,
                        Cantidad = det.Cantidad,
                        Precio = det.PrecioUnitario,
                        SubTotal = det.SubTotal,
                        Observacion =
                            $"Anulación NC {nota.NumeroDocumento}"
                    });
            }

            if (movimiento.Detalles.Any())
            {
                await _movimientosInventario
                    .GuardarMovimiento(movimiento);
            }
        }
    }
}
