using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    public class ComprasService : IComprasService
    {
        private const int TipoDocumentoFacturaCompra = 11;
        private const int TipoDocumentoOrdenCompra = 5;

        private readonly AlahiaPosContext _context;
        private readonly IRepository<OrdenCompraHeader> _headerRepository;
        private readonly IRepository<OrdenCompraDetalle> _detalleRepository;
        private readonly IRepository<PagosProveedor> _pagosRepository;
        private readonly IRepository<Productos> _productosRepository;
        private readonly IRepository<Proveedores> _proveedoresRepository;
        private readonly IRepository<Gastos> _gastosRepository;
        private readonly ISecuenciaDocumentoService _secuenciaService;
        private readonly IMovimientosInventarioService _movimientosInventario;
        private readonly IMovimientoFinancieroService _movimientoFinancieroService;
        private readonly IMetodoPagoCuentaService _metodoPagoCuentaService;
        private readonly IFiscalWorkEnqueueService _fiscalEnqueue;

        public ComprasService(
            AlahiaPosContext context,
            IRepository<OrdenCompraHeader> headerRepository,
            IRepository<OrdenCompraDetalle> detalleRepository,
            IRepository<PagosProveedor> pagosRepository,
            IRepository<Productos> productosRepository,
            IRepository<Proveedores> proveedoresRepository,
            IRepository<Gastos> gastosRepository,
            ISecuenciaDocumentoService secuenciaService,
            IMovimientosInventarioService movimientosInventario,
            IMovimientoFinancieroService movimientoFinancieroService,
            IMetodoPagoCuentaService metodoPagoCuentaService,
            IFiscalWorkEnqueueService fiscalEnqueue)
        {
            _context = context;
            _headerRepository = headerRepository;
            _detalleRepository = detalleRepository;
            _pagosRepository = pagosRepository;
            _productosRepository = productosRepository;
            _proveedoresRepository = proveedoresRepository;
            _gastosRepository = gastosRepository;
            _secuenciaService = secuenciaService;
            _movimientosInventario = movimientosInventario;
            _movimientoFinancieroService = movimientoFinancieroService;
            _metodoPagoCuentaService = metodoPagoCuentaService;
            _fiscalEnqueue = fiscalEnqueue;
        }

        public async Task<FacturaCompraDto> GuardarBorradorAsync(GuardarFacturaCompraRequest request)
        {
            ValidarRequestBase(request);

            var proveedor = await _proveedoresRepository.GetByExpresionAsync(p =>
                p.IdProveedor == request.IdProveedor
                && p.IdEmpresa == request.IdEmpresa
                && p.IsActivo);

            if (proveedor == null)
                throw new ArgumentException("Proveedor no válido o inactivo.");

            var totales = await CalcularTotalesAsync(request.Detalles);

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                OrdenCompraHeader header;

                if (request.IdOrdenCompraHeader > 0)
                {
                    header = await _headerRepository.GetByExpresionAsync(h =>
                        h.IdOrdenCompraHeader == request.IdOrdenCompraHeader
                        && h.IdEmpresa == request.IdEmpresa)
                        ?? throw new KeyNotFoundException("Factura de compra no encontrada.");

                    if (!string.Equals(header.Estado, "BORRADOR", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Solo se pueden editar facturas en estado BORRADOR.");

                    ActualizarHeaderDesdeRequest(header, request, totales);
                    _headerRepository.Update(header.IdOrdenCompraHeader, header);
                    await EliminarDetallesAsync(header.IdOrdenCompraHeader, request.IdEmpresa);
                }
                else
                {
                    header = CrearHeaderDesdeRequest(request, totales);
                    header.Estado = "BORRADOR";
                    header.IdTipoDocumentos = TipoDocumentoFacturaCompra;
                    header.Pagado = 0;
                    header.Pendiente = totales.Total;
                    header.AjustadaInventario = false;
                    await _headerRepository.Save(header);
                }

                await GuardarDetallesAsync(header.IdOrdenCompraHeader, request.IdEmpresa, request.Detalles);
                await tx.CommitAsync();

                return await MapToDtoAsync(header.IdOrdenCompraHeader, request.IdEmpresa)
                    ?? throw new Exception("No se pudo cargar la factura guardada.");
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<FacturaCompraDto> ConfirmarAsync(
            int idOrdenCompraHeader,
            ConfirmarFacturaCompraRequest request)
        {
            var header = await _headerRepository.GetByExpresionAsync(h =>
                h.IdOrdenCompraHeader == idOrdenCompraHeader
                && h.IdEmpresa == request.IdEmpresa)
                ?? throw new KeyNotFoundException("Factura de compra no encontrada.");

            if (!string.Equals(header.Estado, "BORRADOR", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("La factura ya fue confirmada o no está en borrador.");

            if (header.IdTipoDocumentos != TipoDocumentoFacturaCompra)
                throw new InvalidOperationException("Solo se confirman facturas de compra (FACTC). Use Emitir para órdenes de compra.");

            var detalles = (await _detalleRepository.GetAllByExpresionAsync(d =>
                d.IdOrdenCompraHeader == idOrdenCompraHeader
                && d.IdEmpresa == request.IdEmpresa)).ToList();

            if (!detalles.Any())
                throw new ArgumentException("La factura no tiene líneas de detalle.");

            if (!header.IdTipoBienesServicios.HasValue || header.IdTipoBienesServicios < 1 || header.IdTipoBienesServicios > 11)
                throw new ArgumentException("Debe indicar el Tipo de Bienes y Servicios (DGII 606) antes de confirmar.");

            if (!header.FormaPagoDgii.HasValue || header.FormaPagoDgii < 1 || header.FormaPagoDgii > 7)
                throw new ArgumentException("Debe indicar la Forma de Pago DGII (606) antes de confirmar.");

            if ((header.ItbisRetenido > 0 || header.MontoRetencionRenta > 0)
                && !header.FechaPagoFiscal.HasValue)
            {
                throw new ArgumentException("Si hay retenciones (ITBIS o ISR), debe indicar la Fecha de pago fiscal.");
            }

            // IdAlmacen es sugerido; la recepción física lo exige al confirmar en Almacén.
            var esContado = EsContado(header.CondicionFactura);
            if (esContado && string.IsNullOrWhiteSpace(request.FormaPago))
                throw new ArgumentException("Debe indicar la forma de pago para compras de contado.");

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                header.NumeroDocumento = await _secuenciaService.GenerarDocumentoAsync(
                    request.IdEmpresa,
                    TipoDocumentoFacturaCompra);

                if (esContado)
                {
                    header.Pagado = header.Total;
                    header.Pendiente = 0;
                    header.Estado = "PAGADA";
                }
                else
                {
                    header.Pagado = 0;
                    header.Pendiente = header.Total;
                    header.Estado = "CONFIRMADA";
                }

                // Recepción física: diferida a MovimientoInventario (no aumentar stock aquí).
                header.EstadoRecepcion = await ResolverEstadoRecepcionInicialAsync(detalles);
                header.AjustadaInventario = header.EstadoRecepcion
                    == EstadoRecepcionCompraConstantes.NoAplica
                    || header.EstadoRecepcion == EstadoRecepcionCompraConstantes.Recibida;

                _headerRepository.Update(header.IdOrdenCompraHeader, header);

                // Solo gastos no inventariables al confirmar. Inventario/ActivoFijo → recepción.
                await ProcesarGastosAlConfirmarAsync(header, detalles, request.IdUsuario);

                if (esContado)
                {
                    await RegistrarSalidaTesoreriaAsync(
                        request.IdEmpresa,
                        request.IdUsuario,
                        header,
                        header.Total,
                        request.FormaPago!,
                        $"COMPRA-CONTADO-{header.IdOrdenCompraHeader}",
                        "PAGO_PROVEEDOR");

                    await RegistrarPagoProveedorInternoAsync(
                        header,
                        header.Total,
                        request.FormaPago!,
                        request.IdUsuario,
                        "Pago al confirmar compra de contado");
                }

                await transaction.CommitAsync();

                try
                {
                    await _fiscalEnqueue.EnqueueFotografiaSiActivoAsync(new FiscalDocumentoRequest
                    {
                        IdEmpresa = request.IdEmpresa,
                        IdUsuario = request.IdUsuario,
                        ReferenciaId = header.IdOrdenCompraHeader,
                        TipoDocumento = "Compra"
                    });
                }
                catch
                {
                    // Confirmación comercial ya commit
                }

                return await MapToDtoAsync(idOrdenCompraHeader, request.IdEmpresa)
                    ?? throw new Exception("No se pudo cargar la factura confirmada.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<FacturaCompraDto> RegistrarPagoAsync(
            int idOrdenCompraHeader,
            RegistrarPagoProveedorRequest request)
        {
            if (request.Monto <= 0)
                throw new ArgumentException("El monto del pago debe ser mayor a cero.");

            var header = await _headerRepository.GetByExpresionAsync(h =>
                h.IdOrdenCompraHeader == idOrdenCompraHeader
                && h.IdEmpresa == request.IdEmpresa)
                ?? throw new KeyNotFoundException("Factura de compra no encontrada.");

            if (EsContado(header.CondicionFactura))
                throw new InvalidOperationException("Las compras de contado no admiten pagos adicionales.");

            if (string.Equals(header.Estado, "PAGADA", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("La factura ya está pagada.");

            if (string.Equals(header.Estado, "ANULADA", StringComparison.OrdinalIgnoreCase)
                || string.Equals(header.Estado, "BORRADOR", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("La factura no está confirmada para recibir pagos.");

            if (request.Monto > header.Pendiente)
                throw new ArgumentException("El monto excede el pendiente de la factura.");

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var pago = await RegistrarPagoProveedorInternoAsync(
                    header,
                    request.Monto,
                    request.FormaPago,
                    request.IdUsuario,
                    request.Nota,
                    request.IdCuentaFinanciera);

                header.Pagado += request.Monto;
                header.Pendiente = header.Total - header.Pagado;
                header.Estado = header.Pendiente <= 0 ? "PAGADA" : "PARCIALMENTE_PAGADA";

                _headerRepository.Update(header.IdOrdenCompraHeader, header);

                await RegistrarSalidaTesoreriaAsync(
                    request.IdEmpresa,
                    request.IdUsuario,
                    header,
                    request.Monto,
                    request.FormaPago,
                    $"PAGO-CXP-{header.IdOrdenCompraHeader}-{pago.IdPagoProveedor}",
                    "PAGO_CXP",
                    request.IdCuentaFinanciera);

                await transaction.CommitAsync();

                return await MapToDtoAsync(idOrdenCompraHeader, request.IdEmpresa)
                    ?? throw new Exception("No se pudo cargar la factura actualizada.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<FacturaCompraDto?> ObtenerPorIdAsync(int idOrdenCompraHeader, int idEmpresa)
        {
            return await MapToDtoAsync(idOrdenCompraHeader, idEmpresa);
        }

        public async Task<IEnumerable<FacturaCompraDto>> ListarAsync(int idEmpresa, string? estado = null)
        {
            var headers = await _headerRepository.GetAllByExpresionAsync(h =>
                h.IdEmpresa == idEmpresa
                && h.IdTipoDocumentos == TipoDocumentoFacturaCompra
                && (estado == null || h.Estado == estado));

            var result = new List<FacturaCompraDto>();
            foreach (var header in headers.OrderByDescending(h => h.FechaInseccion))
            {
                var dto = await MapToDtoAsync(header.IdOrdenCompraHeader, idEmpresa);
                if (dto != null)
                    result.Add(dto);
            }

            return result;
        }

        public async Task<IEnumerable<FacturaCompraDto>> ListarPendientesAsync(int idEmpresa, int? idProveedor = null)
        {
            var headers = await _headerRepository.GetAllByExpresionAsync(h =>
                h.IdEmpresa == idEmpresa
                && h.IdTipoDocumentos == TipoDocumentoFacturaCompra
                && h.Pendiente > 0
                && h.Estado != "BORRADOR"
                && h.Estado != "ANULADA"
                && h.Estado != "PAGADA"
                && (!idProveedor.HasValue || idProveedor.Value == 0 || h.IdProveedor == idProveedor.Value));

            var result = new List<FacturaCompraDto>();
            foreach (var header in headers.OrderBy(h => h.FechaBencimiento))
            {
                var dto = await MapToDtoAsync(header.IdOrdenCompraHeader, idEmpresa);
                if (dto != null)
                    result.Add(dto);
            }

            return result;
        }

        public async Task<IEnumerable<PagosProveedor>> ObtenerPagosAsync(int idOrdenCompraHeader, int idEmpresa)
        {
            var header = await _headerRepository.GetByExpresionAsync(h =>
                h.IdOrdenCompraHeader == idOrdenCompraHeader
                && h.IdEmpresa == idEmpresa);

            if (header == null)
                return Enumerable.Empty<PagosProveedor>();

            return (await _pagosRepository.GetAllByExpresionAsync(p =>
                p.IdOrdenCompraHeader == idOrdenCompraHeader
                && p.IdEmpresa == idEmpresa))
                .OrderByDescending(p => p.FechaInseccion)
                .ThenByDescending(p => p.IdPagoProveedor)
                .ToList();
        }

        public async Task AnularAsync(int idOrdenCompraHeader, int idEmpresa)
        {
            var header = await _headerRepository.GetByExpresionAsync(h =>
                h.IdOrdenCompraHeader == idOrdenCompraHeader
                && h.IdEmpresa == idEmpresa)
                ?? throw new KeyNotFoundException("Factura de compra no encontrada.");

            if (!string.Equals(header.Estado, "BORRADOR", StringComparison.OrdinalIgnoreCase)
                && !(header.IdTipoDocumentos == TipoDocumentoOrdenCompra
                     && (string.Equals(header.Estado, "EMITIDA", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(header.Estado, "ENVIADA", StringComparison.OrdinalIgnoreCase))))
            {
                throw new InvalidOperationException(
                    "Solo se pueden anular borradores, u órdenes emitidas/enviadas aún no facturadas.");
            }

            if (header.IdTipoDocumentos == TipoDocumentoOrdenCompra
                && !string.Equals(header.Estado, "BORRADOR", StringComparison.OrdinalIgnoreCase))
            {
                var tieneFactura = await _headerRepository.GetByExpresionAsync(h =>
                    h.IdEmpresa == idEmpresa
                    && h.IdDocumentoOrigen == idOrdenCompraHeader
                    && h.IdTipoDocumentos == TipoDocumentoFacturaCompra
                    && h.Estado != "ANULADA");
                if (tieneFactura != null)
                    throw new InvalidOperationException("No se puede anular: ya existe una factura generada desde esta orden.");
            }

            header.Estado = "ANULADA";
            _headerRepository.Update(header.IdOrdenCompraHeader, header);
            await Task.CompletedTask;
        }

        // ============================================================
        // ORDEN DE COMPRA (tipo 5)
        // ============================================================

        public async Task<FacturaCompraDto> GuardarBorradorOrdenAsync(GuardarFacturaCompraRequest request)
        {
            ValidarRequestBase(request);

            var proveedor = await _proveedoresRepository.GetByExpresionAsync(p =>
                p.IdProveedor == request.IdProveedor
                && p.IdEmpresa == request.IdEmpresa
                && p.IsActivo);

            if (proveedor == null)
                throw new ArgumentException("Proveedor no válido o inactivo.");

            var totales = await CalcularTotalesAsync(request.Detalles);

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                OrdenCompraHeader header;

                if (request.IdOrdenCompraHeader > 0)
                {
                    header = await _headerRepository.GetByExpresionAsync(h =>
                        h.IdOrdenCompraHeader == request.IdOrdenCompraHeader
                        && h.IdEmpresa == request.IdEmpresa)
                        ?? throw new KeyNotFoundException("Orden de compra no encontrada.");

                    if (header.IdTipoDocumentos != TipoDocumentoOrdenCompra)
                        throw new InvalidOperationException("El documento no es una orden de compra.");

                    if (!string.Equals(header.Estado, "BORRADOR", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Solo se pueden editar órdenes en estado BORRADOR.");

                    ActualizarHeaderDesdeRequest(header, request, totales);
                    header.Pendiente = 0;
                    header.Pagado = 0;
                    _headerRepository.Update(header.IdOrdenCompraHeader, header);
                    await EliminarDetallesAsync(header.IdOrdenCompraHeader, request.IdEmpresa);
                }
                else
                {
                    header = CrearHeaderDesdeRequest(request, totales);
                    header.Estado = "BORRADOR";
                    header.IdTipoDocumentos = TipoDocumentoOrdenCompra;
                    header.Pagado = 0;
                    header.Pendiente = 0;
                    header.AjustadaInventario = false;
                    header.EstadoRecepcion = EstadoRecepcionCompraConstantes.NoAplica;
                    await _headerRepository.Save(header);
                }

                await GuardarDetallesAsync(header.IdOrdenCompraHeader, request.IdEmpresa, request.Detalles);
                await tx.CommitAsync();

                return await MapToDtoAsync(header.IdOrdenCompraHeader, request.IdEmpresa)
                    ?? throw new Exception("No se pudo cargar la orden guardada.");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<FacturaCompraDto> EmitirOrdenAsync(
            int idOrdenCompraHeader,
            EmitirOrdenCompraRequest request)
        {
            var header = await _headerRepository.GetByExpresionAsync(h =>
                h.IdOrdenCompraHeader == idOrdenCompraHeader
                && h.IdEmpresa == request.IdEmpresa)
                ?? throw new KeyNotFoundException("Orden de compra no encontrada.");

            if (header.IdTipoDocumentos != TipoDocumentoOrdenCompra)
                throw new InvalidOperationException("El documento no es una orden de compra.");

            if (!string.Equals(header.Estado, "BORRADOR", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Solo se emiten órdenes en borrador.");

            var detalles = (await _detalleRepository.GetAllByExpresionAsync(d =>
                d.IdOrdenCompraHeader == idOrdenCompraHeader
                && d.IdEmpresa == request.IdEmpresa)).ToList();

            if (!detalles.Any())
                throw new ArgumentException("La orden no tiene líneas de detalle.");

            header.NumeroDocumento = await _secuenciaService.GenerarDocumentoAsync(
                request.IdEmpresa,
                TipoDocumentoOrdenCompra);

            header.Estado = "EMITIDA";
            header.Pendiente = 0;
            header.Pagado = 0;
            header.EstadoRecepcion = EstadoRecepcionCompraConstantes.NoAplica;
            header.AjustadaInventario = false;
            _headerRepository.Update(header.IdOrdenCompraHeader, header);

            return await MapToDtoAsync(idOrdenCompraHeader, request.IdEmpresa)
                ?? throw new Exception("No se pudo cargar la orden emitida.");
        }

        public async Task<IEnumerable<FacturaCompraDto>> ListarOrdenesAsync(int idEmpresa, string? estado = null)
        {
            var headers = await _headerRepository.GetAllByExpresionAsync(h =>
                h.IdEmpresa == idEmpresa
                && h.IdTipoDocumentos == TipoDocumentoOrdenCompra
                && (estado == null || h.Estado == estado));

            var result = new List<FacturaCompraDto>();
            foreach (var header in headers.OrderByDescending(h => h.FechaInseccion))
            {
                var dto = await MapToDtoAsync(header.IdOrdenCompraHeader, idEmpresa);
                if (dto != null)
                    result.Add(dto);
            }

            return result;
        }

        public async Task<FacturaCompraDto> GenerarFacturaDesdeOrdenAsync(
            int idOrdenCompraHeader,
            int idEmpresa,
            int idUsuario)
        {
            var orden = await _headerRepository.GetByExpresionAsync(h =>
                h.IdOrdenCompraHeader == idOrdenCompraHeader
                && h.IdEmpresa == idEmpresa)
                ?? throw new KeyNotFoundException("Orden de compra no encontrada.");

            if (orden.IdTipoDocumentos != TipoDocumentoOrdenCompra)
                throw new InvalidOperationException("El documento no es una orden de compra.");

            if (!string.Equals(orden.Estado, "EMITIDA", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(orden.Estado, "ENVIADA", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(orden.Estado, "PARCIALMENTE_FACTURADA", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("La orden debe estar emitida o enviada para generar factura.");
            }

            var detalles = (await _detalleRepository.GetAllByExpresionAsync(d =>
                d.IdOrdenCompraHeader == idOrdenCompraHeader
                && d.IdEmpresa == idEmpresa)).ToList();

            if (!detalles.Any())
                throw new ArgumentException("La orden no tiene líneas.");

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var factura = new OrdenCompraHeader
                {
                    IdEmpresa = orden.IdEmpresa,
                    IdProveedor = orden.IdProveedor,
                    IdTipoDocumentos = TipoDocumentoFacturaCompra,
                    IdDocumentoOrigen = orden.IdOrdenCompraHeader,
                    CondicionFactura = orden.CondicionFactura ?? "Credito",
                    FechaBencimiento = orden.FechaBencimiento,
                    Comentario = string.IsNullOrWhiteSpace(orden.Comentario)
                        ? $"Desde {orden.NumeroDocumento}"
                        : $"{orden.Comentario} (Desde {orden.NumeroDocumento})",
                    IdAlmacen = orden.IdAlmacen,
                    FechaInseccion = DateTime.Now,
                    TotalDescuento = orden.TotalDescuento,
                    TotalItbis = orden.TotalItbis,
                    Total = orden.Total,
                    Pagado = 0,
                    Pendiente = orden.Total,
                    Estado = "BORRADOR",
                    AjustadaInventario = false,
                    EstadoRecepcion = EstadoRecepcionCompraConstantes.NoAplica,
                    Seleccione = false,
                    NCF = null
                };

                await _headerRepository.Save(factura);

                foreach (var linea in detalles)
                {
                    await _detalleRepository.Save(new OrdenCompraDetalle
                    {
                        IdOrdenCompraHeader = factura.IdOrdenCompraHeader,
                        IdEmpresa = idEmpresa,
                        IdProducto = linea.IdProducto,
                        TipoComportamientoLinea = linea.TipoComportamientoLinea,
                        Cantidad = linea.Cantidad,
                        CantidadRecibida = 0,
                        Descuento = linea.Descuento,
                        Itbis = linea.Itbis,
                        SubTotal = linea.SubTotal
                    });
                }

                orden.Estado = "FACTURADA";
                _headerRepository.Update(orden.IdOrdenCompraHeader, orden);

                await tx.CommitAsync();

                return await MapToDtoAsync(factura.IdOrdenCompraHeader, idEmpresa)
                    ?? throw new Exception("No se pudo cargar la factura generada.");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<FacturaCompraDto> MarcarOrdenEnviadaAsync(
            int idOrdenCompraHeader,
            EnviarOrdenCompraRequest request)
        {
            var header = await _headerRepository.GetByExpresionAsync(h =>
                h.IdOrdenCompraHeader == idOrdenCompraHeader
                && h.IdEmpresa == request.IdEmpresa)
                ?? throw new KeyNotFoundException("Orden de compra no encontrada.");

            if (header.IdTipoDocumentos != TipoDocumentoOrdenCompra)
                throw new InvalidOperationException("El documento no es una orden de compra.");

            if (!string.Equals(header.Estado, "EMITIDA", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(header.Estado, "ENVIADA", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("La orden debe estar emitida para enviarla al proveedor.");
            }

            var proveedor = await _proveedoresRepository.GetByIdAsync(header.IdProveedor);
            var email = !string.IsNullOrWhiteSpace(request.EmailDestino)
                ? request.EmailDestino.Trim()
                : (proveedor?.Email ?? string.Empty);

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException(
                    "Indique un correo destino o registre el email del proveedor.");

            // Envío real SMTP: fase siguiente. Por ahora marca ENVIADA y deja trazabilidad.
            header.Estado = "ENVIADA";
            header.FechaEnvioProveedor = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(request.Mensaje))
            {
                header.Comentario = string.IsNullOrWhiteSpace(header.Comentario)
                    ? $"Envío: {request.Mensaje}"
                    : $"{header.Comentario}\nEnvío: {request.Mensaje}";
            }

            _headerRepository.Update(header.IdOrdenCompraHeader, header);

            return await MapToDtoAsync(idOrdenCompraHeader, request.IdEmpresa)
                ?? throw new Exception("No se pudo cargar la orden.");
        }

        public async Task<EstadoCuentaProveedorDto> ObtenerEstadoCuentaProveedorAsync(
            int idEmpresa,
            int idProveedor,
            DateTime desde,
            DateTime hasta)
        {
            if (idEmpresa <= 0)
                throw new ArgumentException("IdEmpresa es requerido.");
            if (idProveedor <= 0)
                throw new ArgumentException("Debe seleccionar un proveedor.");

            var desdeDia = desde.Date;
            var hastaDia = hasta.Date;
            if (hastaDia < desdeDia)
                throw new ArgumentException("La fecha hasta no puede ser menor que desde.");

            var proveedor = await _proveedoresRepository.GetByExpresionAsync(p =>
                p.IdProveedor == idProveedor && p.IdEmpresa == idEmpresa)
                ?? throw new KeyNotFoundException("Proveedor no encontrado.");

            var facturas = (await _headerRepository.GetAllByExpresionAsync(h =>
                h.IdEmpresa == idEmpresa
                && h.IdProveedor == idProveedor
                && h.IdTipoDocumentos == TipoDocumentoFacturaCompra
                && h.Estado != "BORRADOR"
                && h.Estado != "ANULADA")).ToList();

            var pagos = (await _pagosRepository.GetAllByExpresionAsync(p =>
                p.IdEmpresa == idEmpresa
                && p.IdProveedor == idProveedor)).ToList();

            decimal SumDebitosAntes(DateTime corte) =>
                facturas
                    .Where(h => h.FechaInseccion.Date < corte)
                    .Sum(h => h.Total);

            decimal SumCreditosAntes(DateTime corte) =>
                pagos
                    .Where(p => p.FechaInseccion.Date < corte)
                    .Sum(p => p.Monto);

            var saldoInicial = SumDebitosAntes(desdeDia) - SumCreditosAntes(desdeDia);

            var movimientos = new List<EstadoCuentaMovimientoDto>();
            var balance = saldoInicial;

            movimientos.Add(new EstadoCuentaMovimientoDto
            {
                Fecha = desdeDia,
                Tipo = "SALDO_INICIAL",
                NumeroDocumento = "—",
                Concepto = "Saldo inicial",
                Debito = 0,
                Credito = 0,
                Balance = balance
            });

            var facturasPeriodo = facturas
                .Where(h => h.FechaInseccion.Date >= desdeDia && h.FechaInseccion.Date <= hastaDia)
                .Select(h => new
                {
                    Fecha = h.FechaInseccion.Date,
                    Orden = 1,
                    Id = h.IdOrdenCompraHeader,
                    Tipo = "FACTURA",
                    Numero = h.NumeroDocumento ?? $"FACTC-{h.IdOrdenCompraHeader}",
                    Concepto = $"Factura de compra {(h.NCF != null ? $"NCF {h.NCF}" : "")}".Trim(),
                    Debito = h.Total,
                    Credito = 0m,
                    FormaPago = (string?)null,
                    IdOc = (int?)h.IdOrdenCompraHeader
                });

            var pagosPeriodo = pagos
                .Where(p => p.FechaInseccion.Date >= desdeDia && p.FechaInseccion.Date <= hastaDia)
                .Select(p => new
                {
                    Fecha = p.FechaInseccion.Date,
                    Orden = 2,
                    Id = p.IdPagoProveedor,
                    Tipo = "PAGO",
                    Numero = p.NumeroDocumento ?? $"PAGO-{p.IdPagoProveedor}",
                    Concepto = string.IsNullOrWhiteSpace(p.Nota)
                        ? $"Pago {(p.FormaPago ?? "").Trim()}".Trim()
                        : $"Pago {(p.FormaPago ?? "").Trim()} — {p.Nota}".Trim(),
                    Debito = 0m,
                    Credito = p.Monto,
                    FormaPago = (string?)p.FormaPago,
                    IdOc = (int?)p.IdOrdenCompraHeader
                });

            foreach (var m in facturasPeriodo.Concat(pagosPeriodo)
                         .OrderBy(x => x.Fecha)
                         .ThenBy(x => x.Orden)
                         .ThenBy(x => x.Id))
            {
                balance += m.Debito - m.Credito;
                movimientos.Add(new EstadoCuentaMovimientoDto
                {
                    Fecha = m.Fecha,
                    Tipo = m.Tipo,
                    IdDocumento = m.Id,
                    NumeroDocumento = m.Numero,
                    Concepto = m.Concepto,
                    Debito = m.Debito,
                    Credito = m.Credito,
                    Balance = balance,
                    FormaPago = m.FormaPago,
                    IdOrdenCompraHeader = m.IdOc
                });
            }

            var totalCompradoPeriodo = facturasPeriodo.Sum(x => x.Debito);
            var totalPagadoPeriodo = pagosPeriodo.Sum(x => x.Credito);

            // Balance pendiente actual (no solo del período): débitos − créditos históricos
            var balancePendiente = facturas.Sum(h => h.Total) - pagos.Sum(p => p.Monto);

            var hoy = DateTime.Today;
            var pendientes = facturas
                .Where(h => h.Pendiente > 0 && h.Estado != "PAGADA")
                .OrderBy(h => h.FechaBencimiento)
                .Select(h =>
                {
                    int? dias = null;
                    if (string.Equals(h.CondicionFactura, "Credito", StringComparison.OrdinalIgnoreCase)
                        && h.FechaBencimiento != default)
                    {
                        dias = (hoy - h.FechaBencimiento.Date).Days;
                    }

                    return new EstadoCuentaFacturaPendienteDto
                    {
                        IdOrdenCompraHeader = h.IdOrdenCompraHeader,
                        Fecha = h.FechaInseccion,
                        NumeroDocumento = h.NumeroDocumento,
                        FechaVencimiento = h.FechaBencimiento == default ? null : h.FechaBencimiento,
                        MontoOriginal = h.Total,
                        Pagado = h.Pagado,
                        Pendiente = h.Pendiente,
                        DiasVencimiento = dias,
                        Estado = h.Estado ?? ""
                    };
                })
                .ToList();

            return new EstadoCuentaProveedorDto
            {
                IdEmpresa = idEmpresa,
                IdProveedor = idProveedor,
                ProveedorNombre = proveedor.NombreComercial,
                ProveedorRnc = proveedor.RNC,
                Desde = desdeDia,
                Hasta = hastaDia,
                SaldoInicial = saldoInicial,
                TotalComprado = totalCompradoPeriodo,
                TotalPagado = totalPagadoPeriodo,
                BalancePendiente = balancePendiente,
                Movimientos = movimientos,
                FacturasPendientes = pendientes
            };
        }

        public async Task<AnalisisProductoProveedorDto> ObtenerAnalisisProductoProveedorAsync(
            AnalisisCompraFiltroRequest filtro)
        {
            if (filtro.IdEmpresa <= 0)
                throw new ArgumentException("IdEmpresa es requerido.");

            var desde = filtro.Desde?.Date;
            var hasta = filtro.Hasta?.Date;

            var headers = (await _headerRepository.GetAllByExpresionAsync(h =>
                h.IdEmpresa == filtro.IdEmpresa
                && h.IdTipoDocumentos == TipoDocumentoFacturaCompra
                && h.Estado != "BORRADOR"
                && h.Estado != "ANULADA"
                && (!filtro.IdProveedor.HasValue || filtro.IdProveedor.Value == 0
                    || h.IdProveedor == filtro.IdProveedor.Value)
                && (!filtro.IdAlmacen.HasValue || filtro.IdAlmacen.Value == 0
                    || h.IdAlmacen == filtro.IdAlmacen.Value))).ToList();

            if (desde.HasValue)
                headers = headers.Where(h => h.FechaInseccion.Date >= desde.Value).ToList();
            if (hasta.HasValue)
                headers = headers.Where(h => h.FechaInseccion.Date <= hasta.Value).ToList();

            var headerIds = headers.Select(h => h.IdOrdenCompraHeader).ToHashSet();
            if (!headerIds.Any())
            {
                string? nombreProductoVacio = null;
                if (filtro.IdProducto.HasValue && filtro.IdProducto.Value > 0)
                {
                    var p0 = await _productosRepository.GetByIdAsync(filtro.IdProducto.Value);
                    nombreProductoVacio = p0?.Nombre;
                }

                return new AnalisisProductoProveedorDto
                {
                    IdEmpresa = filtro.IdEmpresa,
                    IdProducto = filtro.IdProducto,
                    ProductoNombre = nombreProductoVacio,
                    Desde = desde,
                    Hasta = hasta
                };
            }

            var detalles = (await _detalleRepository.GetAllByExpresionAsync(d =>
                d.IdEmpresa == filtro.IdEmpresa
                && headerIds.Contains(d.IdOrdenCompraHeader)
                && (!filtro.IdProducto.HasValue || filtro.IdProducto.Value == 0
                    || d.IdProducto == filtro.IdProducto.Value))).ToList();

            var headersMap = headers.ToDictionary(h => h.IdOrdenCompraHeader);
            var proveedorIds = headers.Select(h => h.IdProveedor).Distinct().ToList();
            var proveedores = (await _proveedoresRepository.GetAllByExpresionAsync(p =>
                proveedorIds.Contains(p.IdProveedor))).ToDictionary(p => p.IdProveedor, p => p.NombreComercial);

            var productoIds = detalles.Select(d => d.IdProducto).Distinct().ToList();
            var productosNombres = new Dictionary<int, string?>();
            foreach (var idProd in productoIds)
            {
                var prod = await _productosRepository.GetByIdAsync(idProd);
                productosNombres[idProd] = prod?.Nombre;
            }

            string? productoNombre = null;
            if (filtro.IdProducto.HasValue && filtro.IdProducto.Value > 0)
                productosNombres.TryGetValue(filtro.IdProducto.Value, out productoNombre);

            static decimal PrecioNeto(OrdenCompraDetalle d) =>
                d.Cantidad == 0 ? 0 : (d.SubTotal - d.Itbis) / d.Cantidad;

            var lineas = new List<AnalisisCompraLineaDto>();
            foreach (var d in detalles)
            {
                if (!headersMap.TryGetValue(d.IdOrdenCompraHeader, out var h))
                    continue;

                proveedores.TryGetValue(h.IdProveedor, out var nomProv);
                productosNombres.TryGetValue(d.IdProducto, out var nomProd);

                lineas.Add(new AnalisisCompraLineaDto
                {
                    Fecha = h.FechaInseccion,
                    IdProveedor = h.IdProveedor,
                    ProveedorNombre = nomProv,
                    IdOrdenCompraHeader = h.IdOrdenCompraHeader,
                    NumeroDocumento = h.NumeroDocumento,
                    IdProducto = d.IdProducto,
                    ProductoNombre = nomProd,
                    IdAlmacen = h.IdAlmacen,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = PrecioNeto(d),
                    Itbis = d.Itbis,
                    Descuento = d.Descuento,
                    Total = d.SubTotal
                });
            }

            lineas = lineas
                .OrderByDescending(l => l.Fecha)
                .ThenByDescending(l => l.IdOrdenCompraHeader)
                .ToList();

            var resumen = lineas
                .GroupBy(l => new { l.IdProveedor, l.ProveedorNombre })
                .Select(g =>
                {
                    var ordenados = g.OrderByDescending(x => x.Fecha)
                        .ThenByDescending(x => x.IdOrdenCompraHeader)
                        .ToList();
                    return new AnalisisCompraProveedorResumenDto
                    {
                        IdProveedor = g.Key.IdProveedor,
                        ProveedorNombre = g.Key.ProveedorNombre,
                        UltimoPrecio = ordenados.First().PrecioUnitario,
                        PrecioPromedio = g.Average(x => x.PrecioUnitario),
                        MejorPrecio = g.Min(x => x.PrecioUnitario),
                        PeorPrecio = g.Max(x => x.PrecioUnitario),
                        VecesComprado = g.Select(x => x.IdOrdenCompraHeader).Distinct().Count(),
                        CantidadTotal = g.Sum(x => x.Cantidad),
                        UltimaFecha = ordenados.First().Fecha
                    };
                })
                .OrderBy(r => r.MejorPrecio)
                .ToList();

            var indicadores = new AnalisisCompraIndicadoresDto();
            if (lineas.Any())
            {
                var ultima = lineas.First();
                indicadores.UltimoPrecio = ultima.PrecioUnitario;
                indicadores.UltimoProveedor = ultima.ProveedorNombre;
                indicadores.UltimaFecha = ultima.Fecha;
                indicadores.PrecioPromedio = lineas.Average(l => l.PrecioUnitario);
                indicadores.MejorPrecio = lineas.Min(l => l.PrecioUnitario);
                indicadores.PeorPrecio = lineas.Max(l => l.PrecioUnitario);
                indicadores.VecesComprado = lineas.Select(l => l.IdOrdenCompraHeader).Distinct().Count();
                indicadores.CantidadTotal = lineas.Sum(l => l.Cantidad);
                indicadores.MayorCantidad = lineas.Max(l => l.Cantidad);
                indicadores.ProveedorMasBarato = resumen.OrderBy(r => r.MejorPrecio).FirstOrDefault()?.ProveedorNombre;
                indicadores.ProveedorMasFrecuente = resumen.OrderByDescending(r => r.VecesComprado)
                    .FirstOrDefault()?.ProveedorNombre;
            }

            var evolucion = lineas
                .OrderBy(l => l.Fecha)
                .ThenBy(l => l.IdOrdenCompraHeader)
                .Select(l => new AnalisisCompraEvolucionDto
                {
                    Fecha = l.Fecha,
                    PrecioUnitario = l.PrecioUnitario,
                    IdProveedor = l.IdProveedor,
                    ProveedorNombre = l.ProveedorNombre,
                    NumeroDocumento = l.NumeroDocumento
                })
                .ToList();

            return new AnalisisProductoProveedorDto
            {
                IdEmpresa = filtro.IdEmpresa,
                IdProducto = filtro.IdProducto,
                ProductoNombre = productoNombre,
                Desde = desde,
                Hasta = hasta,
                Indicadores = indicadores,
                ResumenProveedores = resumen,
                Historial = lineas,
                EvolucionPrecio = evolucion
            };
        }

        private async Task ProcesarGastosAlConfirmarAsync(
            OrdenCompraHeader header,
            List<OrdenCompraDetalle> detalles,
            int idUsuario)
        {
            foreach (var linea in detalles)
            {
                var tipo = TipoComportamientoConstantes.Normalizar(linea.TipoComportamientoLinea);
                if (tipo == TipoComportamientoConstantes.Gasto)
                    await RegistrarGastoDesdeLineaAsync(header, linea, idUsuario);
            }
        }

        private async Task<string> ResolverEstadoRecepcionInicialAsync(List<OrdenCompraDetalle> detalles)
        {
            var hayRecepcion = false;
            foreach (var linea in detalles)
            {
                var tipo = TipoComportamientoConstantes.Normalizar(linea.TipoComportamientoLinea);
                if (string.IsNullOrWhiteSpace(linea.TipoComportamientoLinea))
                {
                    var producto = _productosRepository.GetById(linea.IdProducto);
                    tipo = producto != null
                        ? TipoComportamientoConstantes.ResolverComportamientoCompra(producto)
                        : TipoComportamientoConstantes.Inventario;
                }

                if (TipoComportamientoConstantes.RequiereRecepcionFisica(tipo))
                {
                    hayRecepcion = true;
                    break;
                }
            }

            await Task.CompletedTask;
            return hayRecepcion
                ? EstadoRecepcionCompraConstantes.PendienteRecepcion
                : EstadoRecepcionCompraConstantes.NoAplica;
        }

        public async Task<IEnumerable<FacturaCompraDto>> BuscarPendientesRecepcionAsync(
            int idEmpresa,
            string? texto = null,
            int? idProveedor = null)
        {
            var headers = await _headerRepository.GetAllByExpresionAsync(h =>
                h.IdEmpresa == idEmpresa
                && h.IdTipoDocumentos == TipoDocumentoFacturaCompra
                && h.Estado != "BORRADOR"
                && h.Estado != "ANULADA"
                && (h.EstadoRecepcion == EstadoRecepcionCompraConstantes.PendienteRecepcion
                    || h.EstadoRecepcion == EstadoRecepcionCompraConstantes.ParcialmenteRecibida
                    || h.EstadoRecepcion == null));

            if (idProveedor.HasValue && idProveedor.Value > 0)
                headers = headers.Where(h => h.IdProveedor == idProveedor.Value);

            if (!string.IsNullOrWhiteSpace(texto))
            {
                var q = texto.Trim().ToLowerInvariant();
                var proveedores = await _proveedoresRepository.GetAllByExpresionAsync(p =>
                    p.IdEmpresa == idEmpresa
                    && p.NombreComercial != null
                    && p.NombreComercial.ToLower().Contains(q));
                var idsProv = proveedores.Select(p => p.IdProveedor).ToHashSet();

                headers = headers.Where(h =>
                    (h.NumeroDocumento != null && h.NumeroDocumento.ToLower().Contains(q))
                    || (h.NCF != null && h.NCF.ToLower().Contains(q))
                    || idsProv.Contains(h.IdProveedor));
            }

            var result = new List<FacturaCompraDto>();
            foreach (var header in headers.OrderByDescending(h => h.FechaInseccion))
            {
                var dto = await MapToDtoAsync(header.IdOrdenCompraHeader, idEmpresa);
                if (dto == null)
                    continue;

                if (!dto.Detalles.Any(d => d.RequiereRecepcionFisica && d.CantidadPendienteRecepcion > 0))
                    continue;

                result.Add(dto);
            }

            return result;
        }

        public async Task<FacturaCompraDto?> ObtenerParaRecepcionAsync(int idOrdenCompraHeader, int idEmpresa)
        {
            var dto = await MapToDtoAsync(idOrdenCompraHeader, idEmpresa);
            if (dto == null)
                return null;

            if (!EstadoRecepcionCompraConstantes.EstaAbierta(dto.EstadoRecepcion)
                && dto.EstadoRecepcion != EstadoRecepcionCompraConstantes.NoAplica)
            {
                // Permitir ver documentos ya recibidos, pero el POST validará.
            }

            return dto;
        }

        public async Task<FacturaCompraDto> ConfirmarRecepcionAsync(
            int idOrdenCompraHeader,
            ConfirmarRecepcionCompraRequest request)
        {
            if (request.Lineas == null || !request.Lineas.Any(l => l.CantidadRecibir > 0))
                throw new ArgumentException("Debe indicar al menos una cantidad a recibir.");

            var header = await _headerRepository.GetByExpresionAsync(h =>
                h.IdOrdenCompraHeader == idOrdenCompraHeader
                && h.IdEmpresa == request.IdEmpresa)
                ?? throw new KeyNotFoundException("Factura de compra no encontrada.");

            if (string.Equals(header.Estado, "BORRADOR", StringComparison.OrdinalIgnoreCase)
                || string.Equals(header.Estado, "ANULADA", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("La factura no está disponible para recepción.");

            var estadoRec = string.IsNullOrWhiteSpace(header.EstadoRecepcion)
                ? EstadoRecepcionCompraConstantes.PendienteRecepcion
                : header.EstadoRecepcion;

            if (string.Equals(estadoRec, EstadoRecepcionCompraConstantes.Recibida, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("La factura ya fue recibida por completo.");

            if (string.Equals(estadoRec, EstadoRecepcionCompraConstantes.NoAplica, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Esta factura no tiene líneas que requieran recepción física.");

            if (!EstadoRecepcionCompraConstantes.EstaAbierta(estadoRec))
                throw new InvalidOperationException("La factura no está disponible para recepción.");

            var detalles = (await _detalleRepository.GetAllByExpresionAsync(d =>
                d.IdOrdenCompraHeader == idOrdenCompraHeader
                && d.IdEmpresa == request.IdEmpresa)).ToList();

            // Pre-validar tipos para exigir almacén solo si hay Inventario
            var tiposReq = request.Lineas
                .Where(l => l.CantidadRecibir > 0)
                .Select(l =>
                {
                    var det = detalles.FirstOrDefault(d => d.IdOrdenCompraDetalle == l.IdOrdenCompraDetalle);
                    return det == null
                        ? null
                        : TipoComportamientoConstantes.Normalizar(det.TipoComportamientoLinea);
                })
                .Where(t => t != null)
                .ToList();

            var tieneInventario = tiposReq.Any(t => t == TipoComportamientoConstantes.Inventario);
            if (tieneInventario && request.IdAlmacen <= 0)
                throw new ArgumentException("Debe seleccionar el almacén de recepción para líneas de Inventario.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var lineasInventario = new List<MovimientosInventarioDetalle>();

                foreach (var reqLinea in request.Lineas.Where(l => l.CantidadRecibir > 0))
                {
                    var detalle = detalles.FirstOrDefault(d =>
                        d.IdOrdenCompraDetalle == reqLinea.IdOrdenCompraDetalle)
                        ?? throw new ArgumentException($"Línea no encontrada: {reqLinea.IdOrdenCompraDetalle}");

                    var tipo = TipoComportamientoConstantes.Normalizar(detalle.TipoComportamientoLinea);
                    if (!TipoComportamientoConstantes.RequiereRecepcionFisica(tipo))
                        throw new ArgumentException(
                            $"La línea {detalle.IdOrdenCompraDetalle} no requiere recepción física.");

                    var pendiente = detalle.Cantidad - detalle.CantidadRecibida;
                    if (reqLinea.CantidadRecibir > pendiente + 0.0001m)
                        throw new ArgumentException(
                            $"Cantidad a recibir excede el pendiente de la línea {detalle.IdOrdenCompraDetalle}.");

                    detalle.CantidadRecibida += reqLinea.CantidadRecibir;
                    _detalleRepository.Update(detalle.IdOrdenCompraDetalle, detalle);

                    var precioUnitario = detalle.Cantidad > 0
                        ? (detalle.SubTotal - detalle.Itbis) / detalle.Cantidad
                        : 0;

                    if (tipo == TipoComportamientoConstantes.Inventario)
                    {
                        // Solo Inventario afecta AlmacenExistencia vía MovimientoInventario.
                        lineasInventario.Add(new MovimientosInventarioDetalle
                        {
                            IdProducto = detalle.IdProducto,
                            Cantidad = reqLinea.CantidadRecibir,
                            Precio = precioUnitario,
                            Fecha = DateTime.Now,
                            Observacion = $"OC-{header.IdOrdenCompraHeader}-DET-{detalle.IdOrdenCompraDetalle}"
                        });
                    }
                    else if (tipo == TipoComportamientoConstantes.ActivoFijo)
                    {
                        // Activo Fijo: no stock. Alta individual por unidad recibida.
                        await CrearActivosFijosDesdeRecepcionAsync(
                            header,
                            detalle,
                            reqLinea.CantidadRecibir,
                            precioUnitario,
                            request.IdAlmacen > 0 ? request.IdAlmacen : null,
                            request.IdUsuario,
                            request.Observacion);
                    }
                }

                if (lineasInventario.Any())
                {
                    var stamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    var movimiento = new MovimientosInventario
                    {
                        TipoMovimiento = "ENTRADA",
                        Motivo = "COMPRA",
                        Referencia = $"OC-{header.IdOrdenCompraHeader}-REC-{stamp}",
                        Observacion = string.IsNullOrWhiteSpace(request.Observacion)
                            ? $"Recepción factura compra {header.NumeroDocumento}"
                            : request.Observacion,
                        Fecha = DateTime.Now,
                        IdUsuario = request.IdUsuario > 0 ? request.IdUsuario : null,
                        IdEmpresa = header.IdEmpresa,
                        IdAlmacen = request.IdAlmacen,
                        Activo = true,
                        Detalles = lineasInventario
                    };

                    await _movimientosInventario.GuardarMovimiento(movimiento);
                }

                if (request.IdAlmacen > 0)
                    header.IdAlmacen = request.IdAlmacen;

                header.FechaUltimaRecepcion = DateTime.Now;
                header.EstadoRecepcion = CalcularEstadoRecepcion(detalles);
                header.AjustadaInventario = header.EstadoRecepcion
                    == EstadoRecepcionCompraConstantes.Recibida;
                _headerRepository.Update(header.IdOrdenCompraHeader, header);

                await transaction.CommitAsync();

                return await MapToDtoAsync(idOrdenCompraHeader, request.IdEmpresa)
                    ?? throw new Exception("No se pudo cargar la factura tras la recepción.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Genera un registro ActivoFijo por cada unidad física recibida.
        /// No toca AlmacenExistencia.
        /// </summary>
        private async Task CrearActivosFijosDesdeRecepcionAsync(
            OrdenCompraHeader header,
            OrdenCompraDetalle detalle,
            decimal cantidadRecibir,
            decimal valorUnitario,
            int? idAlmacenRecepcion,
            int idUsuario,
            string? observacion)
        {
            var producto = await _productosRepository.GetByIdAsync(detalle.IdProducto);
            var nombre = producto?.Nombre ?? $"Producto {detalle.IdProducto}";

            var enteros = (int)Math.Floor(cantidadRecibir);
            var fraccion = cantidadRecibir - enteros;
            var unidades = new List<decimal>();
            for (var i = 0; i < enteros; i++)
                unidades.Add(1m);
            if (fraccion > 0.0001m)
                unidades.Add(fraccion);

            if (!unidades.Any())
                return;

            int? primerId = detalle.IdActivoFijoGenerado;
            var seqBase = await _context.ActivosFijos
                .CountAsync(a => a.IdEmpresa == header.IdEmpresa);

            var n = 0;
            foreach (var factor in unidades)
            {
                n++;
                seqBase++;
                var codigo = $"AF-{header.IdEmpresa}-{seqBase:D6}";

                var activo = new ActivoFijo
                {
                    IdEmpresa = header.IdEmpresa,
                    IdProducto = detalle.IdProducto,
                    IdOrdenCompraHeader = header.IdOrdenCompraHeader,
                    IdOrdenCompraDetalle = detalle.IdOrdenCompraDetalle,
                    CodigoActivo = codigo,
                    Descripcion = unidades.Count > 1 && factor == 1m
                        ? $"{nombre} ({n}/{enteros + (fraccion > 0.0001m ? 1 : 0)})"
                        : nombre,
                    FechaAdquisicion = header.FechaInseccion.Date,
                    FechaRecepcion = DateTime.Now,
                    ValorAdquisicion = Math.Round(valorUnitario * factor, 2),
                    ValorResidual = 0,
                    Estado = EstadoActivoFijoConstantes.PendienteDatos,
                    IdAlmacenRecepcion = idAlmacenRecepcion,
                    Observacion = string.IsNullOrWhiteSpace(observacion)
                        ? $"Alta por recepción FACTC {header.NumeroDocumento}"
                        : observacion,
                    FechaCreacion = DateTime.Now,
                    IdUsuarioCreacion = idUsuario > 0 ? idUsuario : null,
                    Activo = true,
                    FechaInseccion = DateTime.Now.Date
                };

                await _context.ActivosFijos.AddAsync(activo);
                await _context.SaveChangesAsync();

                if (!primerId.HasValue || primerId.Value <= 0)
                    primerId = activo.IdActivoFijo;
            }

            if (primerId.HasValue && (!detalle.IdActivoFijoGenerado.HasValue || detalle.IdActivoFijoGenerado <= 0))
            {
                detalle.IdActivoFijoGenerado = primerId;
                _detalleRepository.Update(detalle.IdOrdenCompraDetalle, detalle);
            }
        }

        private static string CalcularEstadoRecepcion(List<OrdenCompraDetalle> detalles)
        {
            var recibibles = detalles
                .Where(d => TipoComportamientoConstantes.RequiereRecepcionFisica(d.TipoComportamientoLinea))
                .ToList();

            if (!recibibles.Any())
                return EstadoRecepcionCompraConstantes.NoAplica;

            if (recibibles.All(d => d.CantidadRecibida >= d.Cantidad))
                return EstadoRecepcionCompraConstantes.Recibida;

            if (recibibles.Any(d => d.CantidadRecibida > 0))
                return EstadoRecepcionCompraConstantes.ParcialmenteRecibida;

            return EstadoRecepcionCompraConstantes.PendienteRecepcion;
        }

        private async Task RegistrarGastoDesdeLineaAsync(
            OrdenCompraHeader header,
            OrdenCompraDetalle linea,
            int idUsuario)
        {
            if (linea.IdGastoGenerado.HasValue && linea.IdGastoGenerado.Value > 0)
                return;

            var producto = _productosRepository.GetById(linea.IdProducto);
            var nombre = producto?.Nombre ?? $"Producto {linea.IdProducto}";

            var gasto = new Gastos
            {
                TipoGasto = "Compra proveedor",
                IdProveedor = header.IdProveedor,
                Monto = linea.SubTotal,
                Detalle = $"{nombre} — {header.NumeroDocumento}",
                FormaPago = header.CondicionFactura ?? "Credito",
                Orien = "COMPRAS",
                Referencia = $"OC-{header.IdOrdenCompraHeader}-DET-{linea.IdOrdenCompraDetalle}",
                OrigenModulo = "COMPRAS",
                IdOrdenCompraDetalle = linea.IdOrdenCompraDetalle,
                IdUsuario = idUsuario > 0 ? idUsuario : null,
                IdEmpresa = header.IdEmpresa,
                FechaInseccion = DateTime.Now,
                EstaCerrada = false,
                EstaAnulado = false
            };

            await _gastosRepository.Save(gasto);

            linea.IdGastoGenerado = gasto.IdGasto;
            _detalleRepository.Update(linea.IdOrdenCompraDetalle, linea);
        }

        private async Task RegistrarSalidaTesoreriaAsync(
            int idEmpresa,
            int idUsuario,
            OrdenCompraHeader header,
            decimal monto,
            string formaPago,
            string claveIdempotencia,
            string categoria,
            int? idCuentaFinanciera = null)
        {
            var idCuenta = idCuentaFinanciera;

            if (!idCuenta.HasValue || idCuenta.Value <= 0)
            {
                var metodo = await _metodoPagoCuentaService.GetByMetodoAsync(idEmpresa, formaPago)
                    ?? throw new ArgumentException(
                        "El método de pago seleccionado no tiene una cuenta financiera configurada.");

                idCuenta = metodo.IdCuentaFinanciera;
            }

            var cuenta = await _context.CuentaFinanciera
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.IdCuentaFinanciera == idCuenta.Value
                    && c.IdEmpresa == idEmpresa
                    && c.Activa)
                ?? throw new ArgumentException("La cuenta financiera origen no existe o está inactiva.");

            if (!cuenta.PermiteSaldoNegativo && cuenta.SaldoDisponible < monto)
                throw new ArgumentException(
                    $"Fondos insuficientes en {cuenta.Nombre}. Disponible: {cuenta.SaldoDisponible:N2}");

            await _movimientoFinancieroService.RegistrarSalidaAsync(
                idEmpresa,
                idUsuario,
                cuenta.IdCuentaFinanciera,
                monto,
                $"Pago proveedor - {header.NumeroDocumento}",
                header.Comentario ?? $"Pago factura compra #{header.IdOrdenCompraHeader}",
                categoria: categoria,
                referenciaId: header.IdOrdenCompraHeader,
                referenciaTipo: "ORDEN_COMPRA",
                claveIdempotencia: claveIdempotencia);
        }

        private async Task<PagosProveedor> RegistrarPagoProveedorInternoAsync(
            OrdenCompraHeader header,
            decimal monto,
            string formaPago,
            int idUsuario,
            string? nota,
            int? idCuentaFinanciera = null)
        {
            var pago = new PagosProveedor
            {
                IdOrdenCompraHeader = header.IdOrdenCompraHeader,
                NumeroDocumento = header.NumeroDocumento,
                IdProveedor = header.IdProveedor,
                FormaPago = formaPago,
                Monto = monto,
                Nota = nota,
                IdUsuario = idUsuario > 0 ? idUsuario : null,
                IdCuentaFinanciera = idCuentaFinanciera,
                IdEmpresa = header.IdEmpresa,
                FechaInseccion = DateTime.Now
            };

            await _pagosRepository.Save(pago);
            return pago;
        }

        private static bool EsContado(string? condicion)
        {
            return string.Equals(condicion, "Contado", StringComparison.OrdinalIgnoreCase);
        }

        private static void ValidarRequestBase(GuardarFacturaCompraRequest request)
        {
            if (request.IdEmpresa <= 0)
                throw new ArgumentException("IdEmpresa es requerido.");

            if (request.IdProveedor <= 0)
                throw new ArgumentException("Debe seleccionar un proveedor.");

            if (request.Detalles == null || !request.Detalles.Any())
                throw new ArgumentException("Debe agregar al menos una línea.");

            if (!EsContado(request.CondicionFactura)
                && !string.Equals(request.CondicionFactura, "Credito", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("CondicionFactura debe ser Contado o Credito.");
            }
        }

        private static OrdenCompraHeader CrearHeaderDesdeRequest(
            GuardarFacturaCompraRequest request,
            (decimal TotalDescuento, decimal TotalItbis, decimal Total) totales)
        {
            var montos = ResolverMontosDgii(request, totales);

            return new OrdenCompraHeader
            {
                IdEmpresa = request.IdEmpresa,
                IdProveedor = request.IdProveedor,
                IdTipoDocumentos = TipoDocumentoFacturaCompra,
                NCF = request.NumeroComprobanteProveedor,
                NcfModificado = request.NcfModificado,
                CondicionFactura = request.CondicionFactura,
                FechaBencimiento = request.FechaVencimiento ?? request.FechaDocumento,
                Comentario = request.Comentario,
                IdAlmacen = request.IdAlmacen,
                FechaInseccion = request.FechaDocumento,
                IdTipoBienesServicios = request.IdTipoBienesServicios,
                FormaPagoDgii = request.FormaPagoDgii,
                MontoFacturadoServicios = montos.Servicios,
                MontoFacturadoBienes = montos.Bienes,
                ItbisRetenido = request.ItbisRetenido,
                ItbisProporcionalidad = request.ItbisProporcionalidad,
                ItbisLlevadoAlCosto = request.ItbisLlevadoAlCosto,
                TipoRetencionIsr = request.TipoRetencionIsr,
                MontoRetencionRenta = request.MontoRetencionRenta,
                FechaPagoFiscal = request.FechaPagoFiscal,
                TotalDescuento = totales.TotalDescuento,
                TotalItbis = totales.TotalItbis,
                Total = totales.Total,
                Pagado = 0,
                Pendiente = totales.Total,
                Estado = "BORRADOR",
                AjustadaInventario = false,
                Seleccione = false
            };
        }

        private static void ActualizarHeaderDesdeRequest(
            OrdenCompraHeader header,
            GuardarFacturaCompraRequest request,
            (decimal TotalDescuento, decimal TotalItbis, decimal Total) totales)
        {
            var montos = ResolverMontosDgii(request, totales);

            header.IdProveedor = request.IdProveedor;
            header.NCF = request.NumeroComprobanteProveedor;
            header.NcfModificado = request.NcfModificado;
            header.CondicionFactura = request.CondicionFactura;
            header.FechaBencimiento = request.FechaVencimiento ?? request.FechaDocumento;
            header.Comentario = request.Comentario;
            header.IdAlmacen = request.IdAlmacen;
            header.FechaInseccion = request.FechaDocumento;
            header.IdTipoBienesServicios = request.IdTipoBienesServicios;
            header.FormaPagoDgii = request.FormaPagoDgii;
            header.MontoFacturadoServicios = montos.Servicios;
            header.MontoFacturadoBienes = montos.Bienes;
            header.ItbisRetenido = request.ItbisRetenido;
            header.ItbisProporcionalidad = request.ItbisProporcionalidad;
            header.ItbisLlevadoAlCosto = request.ItbisLlevadoAlCosto;
            header.TipoRetencionIsr = request.TipoRetencionIsr;
            header.MontoRetencionRenta = request.MontoRetencionRenta;
            header.FechaPagoFiscal = request.FechaPagoFiscal;
            header.TotalDescuento = totales.TotalDescuento;
            header.TotalItbis = totales.TotalItbis;
            header.Total = totales.Total;
            header.Pendiente = totales.Total - header.Pagado;
        }

        /// <summary>
        /// Si el cliente envía montos, se usan. Si no, se reparte el neto (Total - ITBIS)
        /// en bienes (todo) y servicios 0 — el front suele enviar el split correcto.
        /// </summary>
        private static (decimal Servicios, decimal Bienes) ResolverMontosDgii(
            GuardarFacturaCompraRequest request,
            (decimal TotalDescuento, decimal TotalItbis, decimal Total) totales)
        {
            var neto = totales.Total - totales.TotalItbis;
            if (neto < 0) neto = 0;

            if (request.MontoFacturadoServicios.HasValue || request.MontoFacturadoBienes.HasValue)
            {
                var servicios = request.MontoFacturadoServicios ?? 0;
                var bienes = request.MontoFacturadoBienes ?? Math.Max(0, neto - servicios);
                return (servicios, bienes);
            }

            return (0, neto);
        }

        private async Task GuardarDetallesAsync(
            int idHeader,
            int idEmpresa,
            IEnumerable<GuardarFacturaCompraDetalleRequest> detalles)
        {
            foreach (var linea in detalles)
            {
                var producto = await _context.Productos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdProducto == linea.IdProducto)
                    ?? throw new ArgumentException($"Producto no encontrado: {linea.IdProducto}");

                if (producto.IdEmpresa != idEmpresa)
                    throw new ArgumentException($"El producto {linea.IdProducto} no pertenece a la empresa.");

                var subtotal = (linea.Cantidad * linea.PrecioCompra) - linea.Descuento + linea.Itbis;

                var tipoLinea = TipoComportamientoConstantes.ResolverComportamientoCompra(producto);

                var detalle = new OrdenCompraDetalle
                {
                    IdOrdenCompraHeader = idHeader,
                    IdProducto = linea.IdProducto,
                    TipoComportamientoLinea = tipoLinea,
                    Cantidad = linea.Cantidad,
                    Descuento = linea.Descuento,
                    Itbis = linea.Itbis,
                    SubTotal = subtotal,
                    IdEmpresa = idEmpresa,
                    FechaInseccion = DateTime.Now,
                    Productos = null
                };

                await _detalleRepository.Save(detalle);
            }
        }

        private async Task EliminarDetallesAsync(int idHeader, int idEmpresa)
        {
            var existentes = await _detalleRepository.GetAllByExpresionAsync(d =>
                d.IdOrdenCompraHeader == idHeader && d.IdEmpresa == idEmpresa);

            foreach (var detalle in existentes)
                _detalleRepository.Delete(detalle.IdOrdenCompraDetalle);
        }

        private async Task<bool> RequiereAlmacenAsync(IEnumerable<OrdenCompraDetalle> detalles)
        {
            foreach (var linea in detalles)
            {
                var tipo = TipoComportamientoConstantes.Normalizar(linea.TipoComportamientoLinea);
                if (TipoComportamientoConstantes.RequiereAlmacen(tipo))
                    return true;

                if (string.IsNullOrWhiteSpace(linea.TipoComportamientoLinea))
                {
                    var producto = _productosRepository.GetById(linea.IdProducto);
                    if (producto != null
                        && TipoComportamientoConstantes.RequiereAlmacen(
                            TipoComportamientoConstantes.ResolverComportamientoCompra(producto)))
                    {
                        return true;
                    }
                }
            }

            await Task.CompletedTask;
            return false;
        }

        private async Task<(decimal TotalDescuento, decimal TotalItbis, decimal Total)> CalcularTotalesAsync(
            IEnumerable<GuardarFacturaCompraDetalleRequest> detalles)
        {
            decimal totalDescuento = 0;
            decimal totalItbis = 0;
            decimal total = 0;

            foreach (var linea in detalles)
            {
                if (linea.Cantidad <= 0)
                    throw new ArgumentException("La cantidad debe ser mayor a cero.");

                if (linea.PrecioCompra < 0)
                    throw new ArgumentException("El precio de compra no puede ser negativo.");

                totalDescuento += linea.Descuento;
                totalItbis += linea.Itbis;
                total += (linea.Cantidad * linea.PrecioCompra) - linea.Descuento + linea.Itbis;
            }

            await Task.CompletedTask;
            return (totalDescuento, totalItbis, total);
        }

        private async Task<FacturaCompraDto?> MapToDtoAsync(int idOrdenCompraHeader, int idEmpresa)
        {
            var header = await _headerRepository.GetByExpresionAsync(h =>
                h.IdOrdenCompraHeader == idOrdenCompraHeader
                && h.IdEmpresa == idEmpresa);

            if (header == null)
                return null;

            var proveedor = await _proveedoresRepository.GetByIdAsync(header.IdProveedor);
            var detalles = await _detalleRepository.GetAllByExpresionAsync(d =>
                d.IdOrdenCompraHeader == idOrdenCompraHeader
                && d.IdEmpresa == idEmpresa);

            var dto = new FacturaCompraDto
            {
                IdOrdenCompraHeader = header.IdOrdenCompraHeader,
                IdEmpresa = header.IdEmpresa,
                IdProveedor = header.IdProveedor,
                ProveedorNombre = proveedor?.NombreComercial,
                NumeroDocumento = header.NumeroDocumento,
                NumeroComprobanteProveedor = header.NCF,
                NcfModificado = header.NcfModificado,
                FechaDocumento = header.FechaInseccion,
                CondicionFactura = header.CondicionFactura ?? "Contado",
                FechaVencimiento = header.FechaBencimiento,
                IdAlmacen = header.IdAlmacen,
                Comentario = header.Comentario,
                IdTipoBienesServicios = header.IdTipoBienesServicios,
                FormaPagoDgii = header.FormaPagoDgii,
                MontoFacturadoServicios = header.MontoFacturadoServicios,
                MontoFacturadoBienes = header.MontoFacturadoBienes,
                ItbisRetenido = header.ItbisRetenido,
                ItbisProporcionalidad = header.ItbisProporcionalidad,
                ItbisLlevadoAlCosto = header.ItbisLlevadoAlCosto,
                TipoRetencionIsr = header.TipoRetencionIsr,
                MontoRetencionRenta = header.MontoRetencionRenta,
                FechaPagoFiscal = header.FechaPagoFiscal,
                TotalDescuento = header.TotalDescuento,
                TotalItbis = header.TotalItbis,
                Total = header.Total,
                Pagado = header.Pagado,
                Pendiente = header.Pendiente,
                Estado = header.Estado ?? "BORRADOR",
                EstadoRecepcion = string.IsNullOrWhiteSpace(header.EstadoRecepcion)
                    ? EstadoRecepcionCompraConstantes.NoAplica
                    : header.EstadoRecepcion,
                FechaUltimaRecepcion = header.FechaUltimaRecepcion,
                AjustadaInventario = header.AjustadaInventario,
                IdTipoDocumentos = header.IdTipoDocumentos,
                IdDocumentoOrigen = header.IdDocumentoOrigen,
                FechaEnvioProveedor = header.FechaEnvioProveedor
            };

            if (header.IdDocumentoOrigen.HasValue && header.IdDocumentoOrigen.Value > 0)
            {
                var origen = await _headerRepository.GetByIdAsync(header.IdDocumentoOrigen.Value);
                dto.NumeroDocumentoOrigen = origen?.NumeroDocumento
                    ?? $"OC-{header.IdDocumentoOrigen.Value}";
            }

            foreach (var linea in detalles)
            {
                var producto = await _productosRepository.GetByIdAsync(linea.IdProducto);
                var tipo = string.IsNullOrWhiteSpace(linea.TipoComportamientoLinea)
                    ? (producto != null
                        ? TipoComportamientoConstantes.ResolverComportamientoCompra(producto)
                        : TipoComportamientoConstantes.Inventario)
                    : linea.TipoComportamientoLinea;

                dto.Detalles.Add(new FacturaCompraDetalleDto
                {
                    IdOrdenCompraDetalle = linea.IdOrdenCompraDetalle,
                    IdProducto = linea.IdProducto,
                    Cantidad = linea.Cantidad,
                    CantidadRecibida = linea.CantidadRecibida,
                    CantidadPendienteRecepcion = linea.CantidadPendienteRecepcion,
                    Descuento = linea.Descuento,
                    Itbis = linea.Itbis,
                    SubTotal = linea.SubTotal,
                    PrecioCompra = linea.Cantidad > 0
                        ? ((linea.SubTotal - linea.Itbis) / linea.Cantidad)
                        : 0,
                    NombreProducto = producto?.Nombre,
                    TipoComportamientoLinea = tipo,
                    RequiereRecepcionFisica = TipoComportamientoConstantes.RequiereRecepcionFisica(tipo)
                });
            }

            return dto;
        }

        // ============================================================
        // Formato 606 (DGII) — compras desde FACTC confirmadas
        // ============================================================

        public async Task<Reporte606Dto> ObtenerReporte606Async(
            int idEmpresa,
            DateTime desde,
            DateTime hasta,
            string? periodo = null)
        {
            var d = desde.Date;
            var h = hasta.Date;
            if (h < d)
                throw new ArgumentException("La fecha hasta no puede ser menor que desde.");

            var empresa = await _context.Empresas
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa);

            var headers = (await _headerRepository.GetAllByExpresionAsync(x =>
                    x.IdEmpresa == idEmpresa
                    && x.IdTipoDocumentos == TipoDocumentoFacturaCompra
                    && x.Estado != "BORRADOR"
                    && x.Estado != "ANULADA"
                    && x.FechaInseccion.Date >= d
                    && x.FechaInseccion.Date <= h))
                .OrderBy(x => x.FechaInseccion)
                .ThenBy(x => x.IdOrdenCompraHeader)
                .ToList();

            var periodoTxt = string.IsNullOrWhiteSpace(periodo)
                ? h.ToString("yyyyMM")
                : periodo.Trim();

            var result = new Reporte606Dto
            {
                IdEmpresa = idEmpresa,
                RncEmpresa = LimpiarDocumento(empresa?.RNC),
                NombreEmpresa = empresa?.NombreComercial,
                Periodo = periodoTxt,
                Desde = d,
                Hasta = h
            };

            foreach (var header in headers)
            {
                var proveedor = await _proveedoresRepository.GetByIdAsync(header.IdProveedor);
                var linea = MapearLinea606(header, proveedor);
                result.Lineas.Add(linea);
            }

            result.CantidadRegistros = result.Lineas.Count;
            result.CantidadConAlertas = result.Lineas.Count(x => !x.EsValidaParaEnvio);
            result.TotalMontoFacturado = result.Lineas.Sum(x => x.TotalMontoFacturado);
            result.TotalItbisFacturado = result.Lineas.Sum(x => x.ItbisFacturado);
            result.TotalItbisRetenido = result.Lineas.Sum(x => x.ItbisRetenido);
            result.TotalRetencionRenta = result.Lineas.Sum(x => x.MontoRetencionRenta);
            result.ContenidoTxt = GenerarTxt606(result);

            return result;
        }

        private static Reporte606LineaDto MapearLinea606(
            OrdenCompraHeader header,
            Proveedores? proveedor)
        {
            var rnc = LimpiarDocumento(proveedor?.RNC);
            var tipoId = ResolverTipoIdDocumento(rnc);

            var neto = header.Total - header.TotalItbis;
            if (neto < 0) neto = 0;

            var servicios = header.MontoFacturadoServicios;
            var bienes = header.MontoFacturadoBienes;
            if (servicios == 0 && bienes == 0)
                bienes = neto;

            var totalFacturado = servicios + bienes;
            var itbisPorAdelantar = header.TotalItbis - header.ItbisLlevadoAlCosto;
            if (itbisPorAdelantar < 0) itbisPorAdelantar = 0;

            var formaPago = header.FormaPagoDgii ?? 0;
            if (formaPago < 1 || formaPago > 7)
            {
                formaPago = string.Equals(header.CondicionFactura, "Credito", StringComparison.OrdinalIgnoreCase)
                    ? 4
                    : 1;
            }

            var tipoBienes = header.IdTipoBienesServicios ?? 0;

            DateTime? fechaPago = header.FechaPagoFiscal;
            if (!fechaPago.HasValue
                && string.Equals(header.CondicionFactura, "Contado", StringComparison.OrdinalIgnoreCase))
            {
                fechaPago = header.FechaInseccion;
            }

            var linea = new Reporte606LineaDto
            {
                IdOrdenCompraHeader = header.IdOrdenCompraHeader,
                NumeroDocumento = header.NumeroDocumento,
                ProveedorNombre = proveedor?.NombreComercial,
                RncCedula = rnc,
                TipoId = tipoId,
                TipoBienesServicios = tipoBienes,
                Ncf = (header.NCF ?? "").Trim().ToUpperInvariant(),
                NcfModificado = string.IsNullOrWhiteSpace(header.NcfModificado)
                    ? null
                    : header.NcfModificado.Trim().ToUpperInvariant(),
                FechaComprobante = header.FechaInseccion.Date,
                FechaPago = fechaPago?.Date,
                MontoFacturadoServicios = servicios,
                MontoFacturadoBienes = bienes,
                TotalMontoFacturado = totalFacturado,
                ItbisFacturado = header.TotalItbis,
                ItbisRetenido = header.ItbisRetenido,
                ItbisProporcionalidad = header.ItbisProporcionalidad,
                ItbisLlevadoAlCosto = header.ItbisLlevadoAlCosto,
                ItbisPorAdelantar = itbisPorAdelantar,
                ItbisPercibido = 0,
                TipoRetencionIsr = header.TipoRetencionIsr,
                MontoRetencionRenta = header.MontoRetencionRenta,
                IsrPercibido = 0,
                ImpuestoSelectivo = 0,
                OtrosImpuestos = 0,
                MontoPropinaLegal = 0,
                FormaPagoDgii = formaPago,
                Estado = header.Estado ?? ""
            };

            if (string.IsNullOrWhiteSpace(linea.RncCedula))
                linea.Alertas.Add("Proveedor sin RNC/Cédula");
            if (linea.TipoId < 1 || linea.TipoId > 2)
                linea.Alertas.Add("Tipo Id inválido (RNC/Cédula)");
            if (linea.TipoBienesServicios < 1 || linea.TipoBienesServicios > 11)
                linea.Alertas.Add("Falta Tipo de Bienes y Servicios DGII (1-11)");
            if (string.IsNullOrWhiteSpace(linea.Ncf))
                linea.Alertas.Add("Falta NCF del proveedor");
            if (linea.FormaPagoDgii < 1 || linea.FormaPagoDgii > 7)
                linea.Alertas.Add("Falta Forma de Pago DGII (1-7)");
            if ((linea.ItbisRetenido > 0 || linea.MontoRetencionRenta > 0) && !linea.FechaPago.HasValue)
                linea.Alertas.Add("Retención sin Fecha de pago fiscal");
            if (Math.Abs(linea.TotalMontoFacturado - (linea.MontoFacturadoServicios + linea.MontoFacturadoBienes)) > 0.02m)
                linea.Alertas.Add("Bienes + Servicios no cuadra con el total facturado");

            return linea;
        }

        private static string GenerarTxt606(Reporte606Dto reporte)
        {
            var rncEmp = reporte.RncEmpresa ?? "";
            var sb = new System.Text.StringBuilder();
            // Encabezado oficial del archivo
            sb.AppendLine($"606|{rncEmp}|{reporte.Periodo}|{reporte.CantidadRegistros}");

            foreach (var l in reporte.Lineas)
            {
                var fechaComp = l.FechaComprobante.ToString("yyyyMMdd");
                var fechaPago = l.FechaPago.HasValue
                    ? l.FechaPago.Value.ToString("yyyyMMdd")
                    : "";

                sb.Append(l.RncCedula).Append('|')
                  .Append(l.TipoId).Append('|')
                  .Append(l.TipoBienesServicios).Append('|')
                  .Append(l.Ncf).Append('|')
                  .Append(l.NcfModificado ?? "").Append('|')
                  .Append(fechaComp).Append('|')
                  .Append(fechaPago).Append('|')
                  .Append(FmtDec(l.MontoFacturadoServicios)).Append('|')
                  .Append(FmtDec(l.MontoFacturadoBienes)).Append('|')
                  .Append(FmtDec(l.TotalMontoFacturado)).Append('|')
                  .Append(FmtDec(l.ItbisFacturado)).Append('|')
                  .Append(FmtDec(l.ItbisRetenido)).Append('|')
                  .Append(FmtDec(l.ItbisProporcionalidad)).Append('|')
                  .Append(FmtDec(l.ItbisLlevadoAlCosto)).Append('|')
                  .Append(FmtDec(l.ItbisPorAdelantar)).Append('|')
                  .Append(FmtDec(l.ItbisPercibido)).Append('|')
                  .Append(l.TipoRetencionIsr?.ToString() ?? "").Append('|')
                  .Append(FmtDec(l.MontoRetencionRenta)).Append('|')
                  .Append(FmtDec(l.IsrPercibido)).Append('|')
                  .Append(FmtDec(l.ImpuestoSelectivo)).Append('|')
                  .Append(FmtDec(l.OtrosImpuestos)).Append('|')
                  .Append(FmtDec(l.MontoPropinaLegal)).Append('|')
                  .Append(l.FormaPagoDgii)
                  .AppendLine();
            }

            return sb.ToString();
        }

        private static string FmtDec(decimal v) => v.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

        private static string LimpiarDocumento(string? doc)
        {
            if (string.IsNullOrWhiteSpace(doc))
                return "";
            return new string(doc.Where(char.IsDigit).ToArray());
        }

        private static int ResolverTipoIdDocumento(string digitos)
        {
            if (string.IsNullOrEmpty(digitos))
                return 0;
            // Cédula RD = 11 dígitos; RNC = 9 (a veces 11 con ceros)
            if (digitos.Length == 11)
                return 2;
            if (digitos.Length == 9)
                return 1;
            // Fallback: números largos como cédula
            return digitos.Length >= 11 ? 2 : 1;
        }
    }
}
