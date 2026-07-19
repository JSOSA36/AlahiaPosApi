using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using System.Linq;

namespace AlahiaPos.DataAccess.Servicios
{
    public class NotasCreditoServices : INotasCredito
    {
        private const int IdTipoDocumentoNotaCredito = 8;

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
            IFiscalWorkEnqueueService fiscalEnqueue)
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
                IdCliente = factura.IDCliente,
                NombreCliente = factura.NombreEmpresa ?? "",
                RNC = factura.RNC ?? "",
                SubTotal = subTotal,
                TotalItbis = totalItbis,
                Total = total,
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

            return new NotasCreditoResultadoDto
            {
                IdNotaCredito = nota.IdNotaCredito,
                NumeroDocumento = nota.NumeroDocumento ?? "",
                NCF = nota.NCF ?? "",
                Total = nota.Total,
                Mensaje = "Nota de crédito generada correctamente."
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
                    empresa?.NombreComercial ?? "",
                TelefonoEmpresa = empresa?.Telefono ?? "",
                DireccionEmpresa = empresa?.Direccion ?? "",
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
                        !string.IsNullOrWhiteSpace(nota.NCF)
                });
            }

            return lista;
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
                    nota.IdEmpresa);

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
    }
}
