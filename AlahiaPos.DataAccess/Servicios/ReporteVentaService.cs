using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    /// <summary>
    /// Reporte operativo de ventas: facturas del período (no caja) con líneas,
    /// NCF, ITBIS y descuentos, agrupadas por forma de pago; y consolidado por producto.
    /// </summary>
    public class ReporteVentaService : IReporteVentaService
    {
        private const int TipoDocumentoFactura = 1;

        private readonly AlahiaPosContext _ctx;

        public ReporteVentaService(AlahiaPosContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<ReporteVentaFacturasDto> ObtenerFacturasAsync(
            int idEmpresa,
            DateTime desde,
            DateTime hasta,
            SucursalConsultaScope? consulta = null)
        {
            var (d, hExcl) = Rango(desde, hasta);
            var raw = await CargarVentasAsync(idEmpresa, d, hExcl, consulta);

            var grupos = raw.Headers
                .Select(h => MapearFactura(h, raw, consulta))
                .GroupBy(f => f.FormaPago, StringComparer.OrdinalIgnoreCase)
                .Select(g => new ReporteVentaFormaPagoGrupoDto
                {
                    FormaPago = g.Key,
                    CantidadFacturas = g.Count(),
                    SubTotal = Round(g.Sum(x => x.SubTotal)),
                    TotalDescuento = Round(g.Sum(x => x.TotalDescuento)),
                    TotalItbis = Round(g.Sum(x => x.TotalItbis)),
                    Total = Round(g.Sum(x => x.Total)),
                    Facturas = g.OrderBy(x => x.Fecha).ThenBy(x => x.NumeroDocumento).ToList()
                })
                .OrderByDescending(g => g.Total)
                .ThenBy(g => g.FormaPago)
                .ToList();

            var facturas = raw.Headers.Select(h => MapearFactura(h, raw, consulta)).ToList();
            var gruposSucursal = consulta?.EsConsolidado == true
                ? facturas
                    .GroupBy(f => new { f.IdSucursal, f.NombreSucursal })
                    .Select(g => new ReporteVentaSucursalGrupoDto
                    {
                        IdSucursal = g.Key.IdSucursal,
                        NombreSucursal = g.Key.NombreSucursal,
                        CantidadFacturas = g.Count(),
                        SubTotal = Round(g.Sum(x => x.SubTotal)),
                        TotalDescuento = Round(g.Sum(x => x.TotalDescuento)),
                        TotalItbis = Round(g.Sum(x => x.TotalItbis)),
                        Total = Round(g.Sum(x => x.Total))
                    })
                    .OrderByDescending(g => g.Total)
                    .ToList()
                : new List<ReporteVentaSucursalGrupoDto>();

            return new ReporteVentaFacturasDto
            {
                Desde = d,
                Hasta = hExcl.AddDays(-1),
                CantidadFacturas = raw.Headers.Count,
                ConNcf = raw.Headers.Count(h => TieneNcf(h.Ncf)),
                SinNcf = raw.Headers.Count(h => !TieneNcf(h.Ncf)),
                SubTotal = Round(raw.Headers.Sum(h => h.SubTotal)),
                TotalDescuento = Round(raw.Headers.Sum(h => h.TotalDescuento)),
                TotalItbis = Round(raw.Headers.Sum(h => h.TotalItbis)),
                Total = Round(raw.Headers.Sum(h => h.Total)),
                Grupos = grupos,
                GruposSucursal = gruposSucursal,
                EsConsolidado = consulta?.EsConsolidado == true
            };
        }

        public async Task<ReporteVentaProductosDto> ObtenerProductosAsync(
            int idEmpresa,
            DateTime desde,
            DateTime hasta,
            SucursalConsultaScope? consulta = null)
        {
            var (d, hExcl) = Rango(desde, hasta);
            var raw = await CargarVentasAsync(idEmpresa, d, hExcl, consulta);

            var productos = raw.Lineas
                .GroupBy(l => l.IdProducto)
                .Select(g =>
                {
                    var cantidad = g.Sum(x => x.Cantidad);
                    var subTotal = g.Sum(x => x.SubTotal);
                    var first = g.First();
                    return new ReporteVentaProductoDto
                    {
                        IdProducto = g.Key,
                        Codigo = first.Codigo,
                        Nombre = first.Producto,
                        EsServicio = first.EsServicio,
                        Cantidad = Round(cantidad),
                        CantidadFacturas = g.Select(x => x.IdFacturaHeader).Distinct().Count(),
                        PrecioPromedio = cantidad == 0 ? 0 : Round(subTotal / cantidad),
                        Descuento = Round(g.Sum(x => x.Descuento)),
                        Itbis = Round(g.Sum(x => x.Itbis)),
                        SubTotal = Round(subTotal),
                        Total = Round(g.Sum(x => x.Total))
                    };
                })
                .OrderByDescending(p => p.Total)
                .ThenBy(p => p.Nombre)
                .ToList();

            return new ReporteVentaProductosDto
            {
                Desde = d,
                Hasta = hExcl.AddDays(-1),
                CantidadProductos = productos.Count,
                CantidadVendida = Round(productos.Sum(p => p.Cantidad)),
                TotalDescuento = Round(productos.Sum(p => p.Descuento)),
                TotalItbis = Round(productos.Sum(p => p.Itbis)),
                Total = Round(productos.Sum(p => p.Total)),
                Productos = productos
            };
        }

        private async Task<VentasRaw> CargarVentasAsync(
            int idEmpresa,
            DateTime desde,
            DateTime hastaExcl,
            SucursalConsultaScope? consulta)
        {
            var idsSucursal = consulta?.IdsPermitidos?.ToList();
            var principal = consulta?.IdPrincipal ?? 0;

            var headersQ = _ctx.FacturaHeaders.AsNoTracking()
                .Where(h =>
                    h.IdEmpresa == idEmpresa
                    && h.IdTipoDocumentos == TipoDocumentoFactura
                    && h.EstaCancelada == false
                    && h.FechaInseccion >= desde
                    && h.FechaInseccion < hastaExcl);
            if (idsSucursal != null)
                headersQ = headersQ.Where(h => idsSucursal.Contains(h.IdSucursal ?? principal));

            var headers = await headersQ
                .Select(h => new HeaderRaw
                {
                    IdFacturaHeader = h.IdFacturaHeader,
                    NumeroDocumento = h.NumeroDocumento ?? "",
                    Fecha = h.FechaInseccion,
                    IdCliente = h.IDCliente,
                    Ncf = h.NCF ?? "",
                    FormaPagoHeader = h.FormaPago ?? "",
                    TipoFactura = h.TipoFactura ?? "",
                    Estado = h.Estado ?? "",
                    SubTotal = h.SubTotal,
                    TotalDescuento = h.TotalDescuento,
                    TotalItbis = h.TotalItbis,
                    Total = h.Total,
                    IdSucursal = h.IdSucursal
                })
                .ToListAsync();

            var raw = new VentasRaw { Headers = headers };
            if (headers.Count == 0)
            {
                return raw;
            }

            var ids = headers.Select(h => h.IdFacturaHeader).ToList();
            var clienteIds = headers
                .Where(h => h.IdCliente.HasValue && h.IdCliente.Value > 0)
                .Select(h => h.IdCliente!.Value)
                .Distinct()
                .ToList();

            if (clienteIds.Count > 0)
            {
                raw.Clientes = await _ctx.Clientes.AsNoTracking()
                    .Where(c => clienteIds.Contains(c.IDCliente))
                    .ToDictionaryAsync(c => c.IDCliente, c => c.NombreComercial ?? "");
            }

            var lineas = await (
                from d in _ctx.FacturaDetalles.AsNoTracking()
                join p in _ctx.Productos.AsNoTracking() on d.IdProducto equals p.IdProducto into pj
                from p in pj.DefaultIfEmpty()
                where ids.Contains(d.IdFacturaHeader)
                select new LineaRaw
                {
                    IdFacturaHeader = d.IdFacturaHeader,
                    IdProducto = d.IdProducto,
                    Codigo = p != null ? (p.CodigoBarra ?? "") : "",
                    Producto = p != null ? (p.Nombre ?? "") : "",
                    EsServicio = p != null && p.EsServicio,
                    Cantidad = d.Cantidad,
                    PrecioOferta = d.PrecioOferta,
                    Descuento = d.Descuento,
                    Itbis = d.Itbis,
                    TotalLinea = d.SubTotal
                }
            ).ToListAsync();

            raw.Lineas = lineas.Select(MapearLinea).ToList();

            raw.Pagos = await _ctx.Ingresos.AsNoTracking()
                .Where(i =>
                    i.IdEmpresa == idEmpresa
                    && i.EstaAnulado == false
                    && i.IdFacturaHeader != null
                    && ids.Contains(i.IdFacturaHeader.Value))
                .Select(i => new PagoRaw
                {
                    IdFacturaHeader = i.IdFacturaHeader!.Value,
                    FormaPago = i.FormaPago ?? "",
                    Monto = i.Monto
                })
                .ToListAsync();

            var conIngreso = raw.Pagos.Select(p => p.IdFacturaHeader).ToHashSet();
            var idsSinIngreso = ids.Where(id => !conIngreso.Contains(id)).ToList();
            if (idsSinIngreso.Count > 0)
            {
                var pagosFactura = await _ctx.PagosFacturasClientes.AsNoTracking()
                    .Where(p => idsSinIngreso.Contains(p.IdFacturaHeader))
                    .Select(p => new PagoRaw
                    {
                        IdFacturaHeader = p.IdFacturaHeader,
                        FormaPago = p.FormaPago ?? "",
                        Monto = p.Monto
                    })
                    .ToListAsync();
                raw.Pagos.AddRange(pagosFactura);
            }

            return raw;
        }

        private ReporteVentaFacturaDto MapearFactura(
            HeaderRaw h,
            VentasRaw raw,
            SucursalConsultaScope? consulta)
        {
            raw.Clientes.TryGetValue(h.IdCliente ?? 0, out var cliente);
            var pagos = raw.Pagos
                .Where(p => p.IdFacturaHeader == h.IdFacturaHeader)
                .GroupBy(p => NormalizarPago(p.FormaPago), StringComparer.OrdinalIgnoreCase)
                .Select(g => new ReporteVentaPagoDto
                {
                    FormaPago = g.Key,
                    Monto = Round(g.Sum(x => x.Monto))
                })
                .OrderByDescending(p => p.Monto)
                .ToList();

            var formaPago = ResolverFormaPago(h, pagos);
            var lineas = raw.Lineas
                .Where(l => l.IdFacturaHeader == h.IdFacturaHeader)
                .ToList();

            var idSuc = h.IdSucursal is > 0
                ? h.IdSucursal.Value
                : (consulta?.IdPrincipal ?? 0);
            var nombreSuc = consulta?.Sucursales
                .FirstOrDefault(s => s.IdSucursal == idSuc)?.Nombre
                ?? "";

            return new ReporteVentaFacturaDto
            {
                IdFacturaHeader = h.IdFacturaHeader,
                NumeroDocumento = string.IsNullOrWhiteSpace(h.NumeroDocumento)
                    ? $"#{h.IdFacturaHeader}"
                    : h.NumeroDocumento,
                Fecha = h.Fecha,
                Cliente = string.IsNullOrWhiteSpace(cliente) ? "Consumidor final" : cliente,
                Ncf = (h.Ncf ?? "").Trim(),
                TieneNcf = TieneNcf(h.Ncf),
                TieneItbis = h.TotalItbis > 0 || lineas.Any(l => l.Itbis > 0),
                TipoFactura = string.IsNullOrWhiteSpace(h.TipoFactura) ? "Contado" : h.TipoFactura,
                Estado = h.Estado,
                FormaPago = formaPago,
                SubTotal = Round(h.SubTotal),
                TotalDescuento = Round(h.TotalDescuento),
                TotalItbis = Round(h.TotalItbis),
                Total = Round(h.Total),
                IdSucursal = idSuc,
                NombreSucursal = nombreSuc,
                Pagos = pagos,
                Lineas = lineas
                    .Select(l => new ReporteVentaLineaDto
                    {
                        IdProducto = l.IdProducto,
                        Codigo = l.Codigo,
                        Producto = l.Producto,
                        EsServicio = l.EsServicio,
                        Cantidad = Round(l.Cantidad),
                        PrecioUnitario = Round(l.PrecioUnitario),
                        Descuento = Round(l.Descuento),
                        Itbis = Round(l.Itbis),
                        SubTotal = Round(l.SubTotal),
                        Total = Round(l.Total)
                    })
                    .ToList()
            };
        }

        private static LineaMapped MapearLinea(LineaRaw d)
        {
            var cantidad = d.Cantidad;
            var itbis = d.Itbis;
            var total = d.TotalLinea;
            var baseLinea = total - itbis;
            if (baseLinea < 0)
            {
                baseLinea = total;
            }

            var precio = d.PrecioOferta > 0
                ? d.PrecioOferta
                : (cantidad == 0 ? 0 : baseLinea / cantidad);

            return new LineaMapped
            {
                IdFacturaHeader = d.IdFacturaHeader,
                IdProducto = d.IdProducto,
                Codigo = (d.Codigo ?? "").Trim(),
                Producto = string.IsNullOrWhiteSpace(d.Producto) ? $"Producto #{d.IdProducto}" : d.Producto.Trim(),
                EsServicio = d.EsServicio,
                Cantidad = cantidad,
                PrecioUnitario = precio,
                Descuento = d.Descuento,
                Itbis = itbis,
                SubTotal = baseLinea,
                Total = total
            };
        }

        private static string ResolverFormaPago(HeaderRaw h, List<ReporteVentaPagoDto> pagos)
        {
            var distintos = pagos
                .Select(p => p.FormaPago)
                .Where(x => !string.Equals(x, "Sin forma de pago", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (distintos.Count == 1)
            {
                return distintos[0];
            }

            if (distintos.Count > 1)
            {
                return "Mixta";
            }

            var header = NormalizarPago(h.FormaPagoHeader);
            if (!string.Equals(header, "Sin forma de pago", StringComparison.OrdinalIgnoreCase))
            {
                return header;
            }

            if (string.Equals(h.TipoFactura, "Credito", StringComparison.OrdinalIgnoreCase)
                || string.Equals(h.Estado, "Pendiente", StringComparison.OrdinalIgnoreCase))
            {
                return "Crédito";
            }

            return "Sin forma de pago";
        }

        private static string NormalizarPago(string? valor)
        {
            var t = (valor ?? "").Trim();
            return string.IsNullOrWhiteSpace(t) ? "Sin forma de pago" : t;
        }

        private static bool TieneNcf(string? ncf)
        {
            var t = (ncf ?? "").Trim();
            return t.Length >= 3 && !t.Equals("0", StringComparison.OrdinalIgnoreCase);
        }

        private static (DateTime desde, DateTime hastaExcl) Rango(DateTime desde, DateTime hasta)
        {
            var d = desde.Date;
            var h = hasta.Date;
            if (h < d)
            {
                (d, h) = (h, d);
            }

            return (d, h.AddDays(1));
        }

        private static decimal Round(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

        private sealed class VentasRaw
        {
            public List<HeaderRaw> Headers { get; set; } = new();
            public Dictionary<int, string> Clientes { get; set; } = new();
            public List<LineaMapped> Lineas { get; set; } = new();
            public List<PagoRaw> Pagos { get; set; } = new();
        }

        private sealed class HeaderRaw
        {
            public int IdFacturaHeader { get; set; }
            public string NumeroDocumento { get; set; } = "";
            public DateTime Fecha { get; set; }
            public int? IdCliente { get; set; }
            public string Ncf { get; set; } = "";
            public string FormaPagoHeader { get; set; } = "";
            public string TipoFactura { get; set; } = "";
            public string Estado { get; set; } = "";
            public decimal SubTotal { get; set; }
            public decimal TotalDescuento { get; set; }
            public decimal TotalItbis { get; set; }
            public decimal Total { get; set; }
            public int? IdSucursal { get; set; }
        }

        private sealed class LineaRaw
        {
            public int IdFacturaHeader { get; set; }
            public int IdProducto { get; set; }
            public string Codigo { get; set; } = "";
            public string Producto { get; set; } = "";
            public bool EsServicio { get; set; }
            public decimal Cantidad { get; set; }
            public decimal PrecioOferta { get; set; }
            public decimal Descuento { get; set; }
            public decimal Itbis { get; set; }
            public decimal TotalLinea { get; set; }
        }

        private sealed class LineaMapped
        {
            public int IdFacturaHeader { get; set; }
            public int IdProducto { get; set; }
            public string Codigo { get; set; } = "";
            public string Producto { get; set; } = "";
            public bool EsServicio { get; set; }
            public decimal Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal Descuento { get; set; }
            public decimal Itbis { get; set; }
            public decimal SubTotal { get; set; }
            public decimal Total { get; set; }
        }

        private sealed class PagoRaw
        {
            public int IdFacturaHeader { get; set; }
            public string FormaPago { get; set; } = "";
            public decimal Monto { get; set; }
        }
    }
}
