using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class ConducesService : IConducesService
    {
        private readonly AlahiaPosContext _context;
        private readonly IRepository<ConduceHeader> _headerRepo;
        private readonly IRepository<ConduceDetalle> _detalleRepo;
        private readonly IRepository<FacturaHeaders> _facturaRepo;
        private readonly IRepository<FacturaDetalles> _facturaDetalleRepo;
        private readonly IRepository<Productos> _productosRepo;
        private readonly IRepository<Clientes> _clientesRepo;
        private readonly IRepository<Almacen> _almacenRepo;

        public ConducesService(
            AlahiaPosContext context,
            IRepository<ConduceHeader> headerRepo,
            IRepository<ConduceDetalle> detalleRepo,
            IRepository<FacturaHeaders> facturaRepo,
            IRepository<FacturaDetalles> facturaDetalleRepo,
            IRepository<Productos> productosRepo,
            IRepository<Clientes> clientesRepo,
            IRepository<Almacen> almacenRepo)
        {
            _context = context;
            _headerRepo = headerRepo;
            _detalleRepo = detalleRepo;
            _facturaRepo = facturaRepo;
            _facturaDetalleRepo = facturaDetalleRepo;
            _productosRepo = productosRepo;
            _clientesRepo = clientesRepo;
            _almacenRepo = almacenRepo;
        }

        public async Task<IEnumerable<ConduceDto>> ListarAsync(
            int idEmpresa, DateTime? desde, DateTime? hasta, int? idFactura, string? q)
        {
            var d0 = (desde ?? DateTime.Today.AddDays(-30)).Date;
            var d1 = (hasta ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

            var headers = (await _headerRepo.GetAllByExpresionAsync(h =>
                h.IdEmpresa == idEmpresa
                && h.Activo
                && h.Fecha >= d0
                && h.Fecha <= d1
                && (!idFactura.HasValue || idFactura <= 0 || h.IdFacturaHeader == idFactura.Value)
            )).OrderByDescending(h => h.Fecha).ToList();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLowerInvariant();
                headers = headers.Where(h =>
                    (h.Numero ?? "").ToLowerInvariant().Contains(term)
                    || h.IdFacturaHeader.ToString().Contains(term)
                    || (h.QuienRecibe ?? "").ToLowerInvariant().Contains(term)
                    || (h.QuienEntrega ?? "").ToLowerInvariant().Contains(term)
                ).ToList();
            }

            var result = new List<ConduceDto>();
            foreach (var h in headers)
                result.Add(await MapHeaderAsync(h, incluirDetalles: false));
            return result;
        }

        public async Task<ConduceDto?> GetByIdAsync(int idConduce, int idEmpresa)
        {
            var h = await _headerRepo.GetByExpresionAsync(x =>
                x.IdConduceHeader == idConduce && x.IdEmpresa == idEmpresa && x.Activo);
            if (h == null) return null;
            return await MapHeaderAsync(h, incluirDetalles: true);
        }

        public async Task<IEnumerable<FacturaParaConduceDto>> FacturasDisponiblesAsync(
            int idEmpresa, DateTime? desde, DateTime? hasta, string? q)
        {
            var d0 = (desde ?? DateTime.Today.AddDays(-90)).Date;
            var d1 = (hasta ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

            var facturas = (await _facturaRepo.GetAllByExpresionAsync(f =>
                f.IdEmpresa == idEmpresa
                && f.IdTipoDocumentos == 1
                && f.EstaCancelada == false
                && f.FechaInseccion >= d0
                && f.FechaInseccion <= d1
            )).OrderByDescending(f => f.FechaInseccion).ToList();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLowerInvariant();
                facturas = facturas.Where(f =>
                    (f.NumeroDocumento ?? "").ToLowerInvariant().Contains(term)
                    || (f.NCF ?? "").ToLowerInvariant().Contains(term)
                    || f.IdFacturaHeader.ToString().Contains(term)
                ).ToList();
            }

            var result = new List<FacturaParaConduceDto>();
            foreach (var f in facturas.Take(200))
            {
                var lineas = await LineasPendientesAsync(f.IdFacturaHeader, idEmpresa);
                var pend = lineas.Where(l => l.CantidadPendiente > 0).ToList();
                if (!pend.Any()) continue;

                string? clienteNombre = null;
                if (f.IDCliente.HasValue && f.IDCliente > 0)
                {
                    var c = await _clientesRepo.GetByExpresionAsync(x =>
                        x.IDCliente == f.IDCliente && x.IdEmpresa == idEmpresa);
                    clienteNombre = c?.NombreComercial;
                }

                result.Add(new FacturaParaConduceDto
                {
                    IdFacturaHeader = f.IdFacturaHeader,
                    NumeroDocumento = string.IsNullOrWhiteSpace(f.NumeroDocumento)
                        ? $"FACT-{f.IdFacturaHeader}"
                        : f.NumeroDocumento!,
                    Ncf = f.NCF,
                    Fecha = f.FechaInseccion,
                    ClienteNombre = clienteNombre,
                    IdCliente = f.IDCliente,
                    Total = f.Total,
                    LineasPendientes = pend.Count,
                    CantidadPendienteTotal = pend.Sum(x => x.CantidadPendiente)
                });
            }

            return result;
        }

        public async Task<IEnumerable<LineaPendienteEntregaDto>> LineasPendientesAsync(int idFactura, int idEmpresa)
        {
            var factura = await _facturaRepo.GetByExpresionAsync(f =>
                f.IdFacturaHeader == idFactura && f.IdEmpresa == idEmpresa)
                ?? throw new KeyNotFoundException("Factura no encontrada.");

            var detalles = (await _facturaDetalleRepo.GetAllByExpresionAsync(d =>
                d.IdFacturaHeader == idFactura)).ToList();

            var conduces = (await _headerRepo.GetAllByExpresionAsync(h =>
                h.IdEmpresa == idEmpresa && h.IdFacturaHeader == idFactura && h.Activo)).ToList();
            var conduceIds = conduces.Select(h => h.IdConduceHeader).ToList();

            var entregas = conduceIds.Count == 0
                ? new List<ConduceDetalle>()
                : (await _detalleRepo.GetAllByExpresionAsync(d => conduceIds.Contains(d.IdConduceHeader))).ToList();

            var entregadoPorLinea = entregas
                .GroupBy(d => d.IdFacturaDetalle)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.CantidadEntregada));

            var productoIds = detalles.Select(d => d.IdProducto).Distinct().ToList();
            var productos = productoIds.Count == 0
                ? new Dictionary<int, Productos>()
                : (await _productosRepo.GetAllByExpresionAsync(p =>
                    productoIds.Contains(p.IdProducto))).ToDictionary(p => p.IdProducto);

            return detalles.Select(d =>
            {
                productos.TryGetValue(d.IdProducto, out var prod);
                var facturada = d.Cantidad;
                var devuelta = d.CantidadDevuelta;
                var entregada = entregadoPorLinea.TryGetValue(d.IdFacturaDetalle, out var e) ? e : 0m;
                var pendiente = Math.Max(0, facturada - devuelta - entregada);

                return new LineaPendienteEntregaDto
                {
                    IdFacturaDetalle = d.IdFacturaDetalle,
                    IdProducto = d.IdProducto,
                    ProductoNombre = prod?.Nombre ?? $"Producto #{d.IdProducto}",
                    CantidadFacturada = facturada,
                    CantidadDevuelta = devuelta,
                    CantidadEntregada = entregada,
                    CantidadPendiente = pendiente
                };
            }).ToList();
        }

        public async Task<EstadoEntregaFacturaDto> EstadoEntregaAsync(int idFactura, int idEmpresa)
        {
            var factura = await _facturaRepo.GetByExpresionAsync(f =>
                f.IdFacturaHeader == idFactura && f.IdEmpresa == idEmpresa)
                ?? throw new KeyNotFoundException("Factura no encontrada.");

            string? clienteNombre = null;
            string? clienteDoc = null;
            if (factura.IDCliente.HasValue && factura.IDCliente > 0)
            {
                var c = await _clientesRepo.GetByExpresionAsync(x =>
                    x.IDCliente == factura.IDCliente && x.IdEmpresa == idEmpresa);
                clienteNombre = c?.NombreComercial;
                clienteDoc = c?.CedulaRNC;
            }

            var lineas = (await LineasPendientesAsync(idFactura, idEmpresa)).ToList();
            var conducesHeaders = (await _headerRepo.GetAllByExpresionAsync(h =>
                h.IdEmpresa == idEmpresa && h.IdFacturaHeader == idFactura && h.Activo))
                .OrderByDescending(h => h.Fecha).ToList();

            var conduces = new List<ConduceDto>();
            foreach (var h in conducesHeaders)
                conduces.Add(await MapHeaderAsync(h, incluirDetalles: true));

            return new EstadoEntregaFacturaDto
            {
                IdFacturaHeader = idFactura,
                NumeroDocumento = string.IsNullOrWhiteSpace(factura.NumeroDocumento)
                    ? $"FACT-{idFactura}"
                    : factura.NumeroDocumento!,
                Ncf = factura.NCF,
                FechaFactura = factura.FechaInseccion,
                ClienteNombre = clienteNombre,
                ClienteDocumento = clienteDoc,
                Lineas = lineas,
                Conduces = conduces,
                TotalFacturado = lineas.Sum(l => l.CantidadFacturada - l.CantidadDevuelta),
                TotalEntregado = lineas.Sum(l => l.CantidadEntregada),
                TotalPendiente = lineas.Sum(l => l.CantidadPendiente)
            };
        }

        public async Task<ConduceDto> CrearAsync(CrearConduceRequest request)
        {
            if (request == null) throw new ArgumentException("Datos inválidos.");
            if (request.IdEmpresa <= 0) throw new ArgumentException("IdEmpresa requerido.");
            if (request.IdFacturaHeader <= 0) throw new ArgumentException("Debe seleccionar una factura.");
            if (request.Detalles == null || !request.Detalles.Any(d => d.CantidadEntregada > 0))
                throw new ArgumentException("Debe indicar al menos un ítem a entregar.");

            var factura = await _facturaRepo.GetByExpresionAsync(f =>
                f.IdFacturaHeader == request.IdFacturaHeader && f.IdEmpresa == request.IdEmpresa)
                ?? throw new KeyNotFoundException("Factura no encontrada.");

            if (factura.EstaCancelada)
                throw new ArgumentException("No se puede emitir conduce de una factura anulada.");

            var pendientes = (await LineasPendientesAsync(request.IdFacturaHeader, request.IdEmpresa))
                .ToDictionary(x => x.IdFacturaDetalle);

            var lineasValidas = new List<(ConduceLineaRequest Req, LineaPendienteEntregaDto Pend)>();
            foreach (var linea in request.Detalles.Where(d => d.CantidadEntregada > 0))
            {
                if (!pendientes.TryGetValue(linea.IdFacturaDetalle, out var pend))
                    throw new ArgumentException($"Línea de factura {linea.IdFacturaDetalle} no válida.");

                if (linea.CantidadEntregada > pend.CantidadPendiente + 0.0001m)
                    throw new ArgumentException(
                        $"La cantidad de «{pend.ProductoNombre}» supera lo pendiente ({pend.CantidadPendiente:N2}).");

                lineasValidas.Add((linea, pend));
            }

            if (lineasValidas.Count == 0)
                throw new ArgumentException("Debe indicar al menos un ítem a entregar.");

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var fecha = request.Fecha?.Date ?? DateTime.Now;
                if (fecha.TimeOfDay == TimeSpan.Zero)
                    fecha = fecha.Date.Add(DateTime.Now.TimeOfDay);

                var header = new ConduceHeader
                {
                    IdFacturaHeader = request.IdFacturaHeader,
                    Numero = "TEMP",
                    Fecha = fecha,
                    QuienEntrega = request.QuienEntrega?.Trim(),
                    QuienRecibe = request.QuienRecibe?.Trim(),
                    Observacion = request.Observacion?.Trim(),
                    IdAlmacen = request.IdAlmacen,
                    IdUsuario = request.IdUsuario,
                    IdEmpresa = request.IdEmpresa,
                    Activo = true,
                    FechaInseccion = DateTime.Now
                };

                await _headerRepo.Save(header);
                header.Numero = $"COND-{header.IdConduceHeader:D6}";
                await _context.SaveChangesAsync();

                foreach (var (linea, _) in lineasValidas)
                {
                    await _detalleRepo.Save(new ConduceDetalle
                    {
                        IdConduceHeader = header.IdConduceHeader,
                        IdFacturaDetalle = linea.IdFacturaDetalle,
                        IdProducto = linea.IdProducto,
                        CantidadEntregada = Math.Round(linea.CantidadEntregada, 4),
                        FechaInseccion = DateTime.Now,
                        IdEmpresa = request.IdEmpresa
                    });
                }

                await tx.CommitAsync();
                return (await GetByIdAsync(header.IdConduceHeader, request.IdEmpresa))!;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task AnularAsync(int idConduce, int idEmpresa)
        {
            var h = await _headerRepo.GetByExpresionAsync(x =>
                x.IdConduceHeader == idConduce && x.IdEmpresa == idEmpresa)
                ?? throw new KeyNotFoundException("Conduce no encontrado.");

            h.Activo = false;
            _context.ConduceHeader.Update(h);
            await _context.SaveChangesAsync();
        }

        private async Task<ConduceDto> MapHeaderAsync(ConduceHeader h, bool incluirDetalles)
        {
            var factura = await _facturaRepo.GetByIdAsync(h.IdFacturaHeader);
            string? clienteNombre = null;
            if (factura?.IDCliente > 0)
            {
                var c = await _clientesRepo.GetByExpresionAsync(x =>
                    x.IDCliente == factura.IDCliente && x.IdEmpresa == h.IdEmpresa);
                clienteNombre = c?.NombreComercial;
            }

            string? almacenNombre = null;
            if (h.IdAlmacen.HasValue && h.IdAlmacen > 0)
            {
                var a = await _almacenRepo.GetByIdAsync(h.IdAlmacen.Value);
                almacenNombre = a?.Nombre;
            }

            var dto = new ConduceDto
            {
                IdConduceHeader = h.IdConduceHeader,
                IdFacturaHeader = h.IdFacturaHeader,
                Numero = h.Numero,
                Fecha = h.Fecha,
                QuienEntrega = h.QuienEntrega,
                QuienRecibe = h.QuienRecibe,
                Observacion = h.Observacion,
                IdAlmacen = h.IdAlmacen,
                AlmacenNombre = almacenNombre,
                IdEmpresa = h.IdEmpresa,
                NumeroFactura = string.IsNullOrWhiteSpace(factura?.NumeroDocumento)
                    ? $"FACT-{h.IdFacturaHeader}"
                    : factura!.NumeroDocumento,
                ClienteNombre = clienteNombre,
                Ncf = factura?.NCF
            };

            if (!incluirDetalles) return dto;

            var dets = (await _detalleRepo.GetAllByExpresionAsync(d =>
                d.IdConduceHeader == h.IdConduceHeader)).ToList();
            var pendientes = (await LineasPendientesAsync(h.IdFacturaHeader, h.IdEmpresa))
                .ToDictionary(x => x.IdFacturaDetalle);

            foreach (var d in dets)
            {
                pendientes.TryGetValue(d.IdFacturaDetalle, out var pend);
                dto.Detalles.Add(new ConduceDetalleDto
                {
                    IdConduceDetalle = d.IdConduceDetalle,
                    IdFacturaDetalle = d.IdFacturaDetalle,
                    IdProducto = d.IdProducto,
                    ProductoNombre = pend?.ProductoNombre ?? $"Producto #{d.IdProducto}",
                    CantidadEntregada = d.CantidadEntregada,
                    CantidadFacturada = pend?.CantidadFacturada ?? 0,
                    CantidadPendiente = pend?.CantidadPendiente ?? 0
                });
            }

            return dto;
        }
    }
}
