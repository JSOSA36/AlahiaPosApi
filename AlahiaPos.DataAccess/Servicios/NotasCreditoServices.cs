using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;

namespace AlahiaPos.DataAccess.Servicios
{
    public class NotasCreditoServices : INotasCredito
    {
        private readonly IRepository<NotasCredito> _notasCredito;

        private readonly IRepository<NotasCreditoDetalle> _notasCreditoDetalle;

        private readonly IFacturaHeader _facturaHeader;

        private readonly IFacturaDetalle _facturaDetalle;

        private readonly IProductos _productos;

        private readonly INCF_Secuencias _ncfSecuencias;

        private readonly IMovimientosInventarioService _movimientosInventario;

        private readonly IAlmacenes _almacenes;

        private readonly IEmpresas _empresas;

        public NotasCreditoServices(
            IRepository<NotasCredito> notasCredito,
            IRepository<NotasCreditoDetalle> notasCreditoDetalle,
            IFacturaHeader facturaHeader,
            IFacturaDetalle facturaDetalle,
            IProductos productos,
            INCF_Secuencias ncfSecuencias,
            IMovimientosInventarioService movimientosInventario,
            IAlmacenes almacenes,
            IEmpresas empresas)
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
                FechaInseccion = DateTime.Now
            };

            if (!string.IsNullOrWhiteSpace(factura.NCF))
            {
                try
                {
                    nota.NCF =
                        await _ncfSecuencias.GenerarNCF(
                            dto.IdEmpresa,
                            "B04");
                }
                catch
                {
                    nota.NCF = "";
                }
            }

            await _notasCredito.Save(nota);

            nota.NumeroDocumento =
                $"NC-{nota.IdNotaCredito:D6}";

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
