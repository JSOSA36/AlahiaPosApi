using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    public class DashboardGerencialService : IDashboardGerencialService
    {
        private const int TipoDocumentoFactura = 1;
        private const int TipoDocumentoFacturaCompra = 11;

        /// <summary>
        /// Solo categorías de Ingresos que representan ingreso económico real
        /// (no cobros de ventas ni movimientos de caja/factura).
        /// </summary>
        private static readonly HashSet<string> CategoriasOtrosIngresosReales =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Pago por Arrendamiento",
                "Ingreso Extraordinario",
                "Otro"
            };

        /// <summary>
        /// Categorías de Ingresos que NO deben sumar a la utilidad
        /// (ya están en ventas o son cobros / encargos).
        /// </summary>
        private static readonly HashSet<string> CategoriasIngresosExcluidas =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Abono a Factura",
                "Saldo de Factura de Crédito",
                "Pago de Contado",
                "Venta de Producto",
                "Servicio Prestado",
                "Abono Encargo",
                "Pago Encargo"
            };

        private readonly AlahiaPosContext _context;
        private readonly IFacturaHeader _facturaHeader;
        private readonly ICuentaFinancieraService _cuentasFinancieras;

        public DashboardGerencialService(
            AlahiaPosContext context,
            IFacturaHeader facturaHeader,
            ICuentaFinancieraService cuentasFinancieras)
        {
            _context = context;
            _facturaHeader = facturaHeader;
            _cuentasFinancieras = cuentasFinancieras;
        }

        public async Task<DashboardGerencialDto> ObtenerMesActualAsync(int idEmpresa)
        {
            var hoy = DateTime.Today;
            var desde = new DateTime(hoy.Year, hoy.Month, 1);
            var hasta = desde.AddMonths(1).AddDays(-1);
            var finExclusivo = desde.AddMonths(1);

            var dto = new DashboardGerencialDto
            {
                IdEmpresa = idEmpresa,
                PeriodoDesde = desde,
                PeriodoHasta = hasta,
                PeriodoLabel = desde.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-DO"))
            };

            var headersMes = await _context.FacturaHeaders
                .AsNoTracking()
                .Where(f =>
                    f.IdEmpresa == idEmpresa
                    && f.IdTipoDocumentos == TipoDocumentoFactura
                    && f.EstaCancelada == false
                    && f.Estado == "Pagada"
                    && f.FechaInseccion >= desde
                    && f.FechaInseccion < finExclusivo)
                .Select(f => new
                {
                    f.IdFacturaHeader,
                    f.Total,
                    f.FechaInseccion
                })
                .ToListAsync();

            var idsHeaders = headersMes.Select(h => h.IdFacturaHeader).ToList();

            var detallesMes = idsHeaders.Count == 0
                ? new List<DetalleVentaRow>()
                : await (
                    from d in _context.FacturaDetalles.AsNoTracking()
                    join p in _context.Productos.AsNoTracking() on d.IdProducto equals p.IdProducto
                    where idsHeaders.Contains(d.IdFacturaHeader)
                    select new DetalleVentaRow
                    {
                        IdProducto = d.IdProducto,
                        Nombre = p.Nombre ?? "Sin nombre",
                        Cantidad = d.Cantidad - d.CantidadDevuelta,
                        VentaNeta = d.SubTotal - d.Itbis,
                        PrecioCompra = p.PrecioCompra,
                        EsServicio = p.EsServicio,
                        TipoComportamiento = p.TipoComportamiento
                    }
                ).ToListAsync();

            foreach (var d in detallesMes)
            {
                if (d.Cantidad < 0) d.Cantidad = 0;
            }

            var ventasBrutas = detallesMes.Sum(d => d.VentaNeta);
            if (ventasBrutas == 0 && headersMes.Count > 0)
                ventasBrutas = headersMes.Sum(h => h.Total);

            var costoVenta = detallesMes
                .Where(EsProductoInventariable)
                .Sum(d => d.Cantidad * d.PrecioCompra);

            var utilidadBruta = ventasBrutas - costoVenta;

            var gastos = await _context.Gastos
                .AsNoTracking()
                .Where(g =>
                    g.IdEmpresa == idEmpresa
                    && g.EstaAnulado == false
                    && g.FechaInseccion >= desde
                    && g.FechaInseccion < finExclusivo)
                .Select(g => new { g.Monto, g.TipoGasto })
                .ToListAsync();

            var gastosOperativos = gastos.Sum(g => g.Monto);

            var comisionesDetalle = _facturaHeader
                .GetComisionesDetalle(desde, hasta, idEmpresa)
                .ToList();
            var comisiones = comisionesDetalle.Sum(c => c.TotalComisiones);

            var perdidasDetalles = await (
                from m in _context.MovimientosInventario.AsNoTracking()
                from det in m.Detalles
                join p in _context.Productos.AsNoTracking() on det.IdProducto equals p.IdProducto into pj
                from p in pj.DefaultIfEmpty()
                where m.IdEmpresa == idEmpresa
                      && m.Activo
                      && m.Fecha >= desde
                      && m.Fecha < finExclusivo
                      && (
                          m.Motivo == "PERDIDA"
                          || (m.Motivo == "AJUSTE" && m.TipoMovimiento == "SALIDA")
                      )
                select new
                {
                    Nombre = p != null ? (p.Nombre ?? "Sin nombre") : "Sin nombre",
                    Monto = det.SubTotal ?? (det.Cantidad * (det.Precio ?? 0))
                }
            ).ToListAsync();

            var perdidasInventario = perdidasDetalles.Sum(x => x.Monto);

            // Otros ingresos: solo ingreso económico real (no cobros de factura / tesorería interna)
            var ingresosMes = await _context.Ingresos
                .AsNoTracking()
                .Where(i =>
                    i.IdEmpresa == idEmpresa
                    && i.EstaAnulado == false
                    && i.FechaRegistro >= desde
                    && i.FechaRegistro < finExclusivo)
                .Select(i => new { i.Categoria, i.Monto, i.IdFacturaHeader })
                .ToListAsync();

            var otrosIngresos = ingresosMes
                .Where(i => EsOtroIngresoEconomico(i.Categoria, i.IdFacturaHeader))
                .Sum(i => i.Monto);

            // Otros egresos: solo egresos extraordinarios explícitos.
            // No incluyen transferencias, préstamos, ajustes de caja ni pagos ya cubiertos por Gastos.
            var otrosEgresosRaw = await _context.MovimientoFinanciero
                .AsNoTracking()
                .Where(m =>
                    m.IdEmpresa == idEmpresa
                    && m.TipoMovimiento == "SALIDA"
                    && m.Estado == "CONFIRMADO"
                    && m.FechaMovimiento >= desde
                    && m.FechaMovimiento < finExclusivo)
                .Select(m => new { m.Categoria, m.Monto, m.TipoMovimiento })
                .ToListAsync();

            var otrosEgresos = otrosEgresosRaw
                .Where(m =>
                {
                    var cat = (m.Categoria ?? "").Trim().ToUpperInvariant();
                    return cat is "OTRO" or "OTROS" or "EGRESO EXTRAORDINARIO" or "EXTRAORDINARIO";
                })
                .Sum(m => m.Monto);

            var utilidadOperativa =
                utilidadBruta
                - gastosOperativos
                - comisiones
                - perdidasInventario
                - otrosEgresos
                + otrosIngresos;

            var margenBrutoPct = ventasBrutas > 0
                ? Round(utilidadBruta / ventasBrutas * 100m)
                : 0;
            var margenOperativoPct = ventasBrutas > 0
                ? Round(utilidadOperativa / ventasBrutas * 100m)
                : 0;

            dto.Pl = new DashboardGerencialPlDto
            {
                VentasBrutas = Round(ventasBrutas),
                CostoVenta = Round(costoVenta),
                UtilidadBruta = Round(utilidadBruta),
                GastosOperativos = Round(gastosOperativos),
                Comisiones = Round(comisiones),
                PerdidasInventario = Round(perdidasInventario),
                OtrosIngresos = Round(otrosIngresos),
                OtrosEgresos = Round(otrosEgresos),
                UtilidadOperativa = Round(utilidadOperativa),
                MargenBrutoPct = margenBrutoPct,
                MargenOperativoPct = margenOperativoPct
            };

            var saldos = (await _cuentasFinancieras.GetResumenSaldosAsync(idEmpresa)).ToList();
            var caja = saldos
                .Where(s => string.Equals(s.TipoCuenta, "CAJA", StringComparison.OrdinalIgnoreCase) && s.Activa)
                .Sum(s => s.SaldoCalculado);
            var bancos = saldos
                .Where(s => string.Equals(s.TipoCuenta, "BANCO", StringComparison.OrdinalIgnoreCase) && s.Activa)
                .Sum(s => s.SaldoCalculado);

            var valorInventario = await (
                from e in _context.AlmacenExistencia.AsNoTracking()
                join p in _context.Productos.AsNoTracking() on e.IdProducto equals p.IdProducto
                where e.IdEmpresa == idEmpresa
                      && !p.EsServicio
                      && (p.TipoComportamiento == null
                          || p.TipoComportamiento == "Inventario"
                          || p.TipoComportamiento == "")
                select e.Cantidad * p.PrecioCompra
            ).SumAsync();

            var valorActivosFijos = await _context.ActivosFijos
                .AsNoTracking()
                .Where(a => a.IdEmpresa == idEmpresa && a.Activo && a.Estado != "Baja")
                .SumAsync(a => (decimal?)a.ValorAdquisicion) ?? 0;

            var cuentasPorCobrar = await _context.FacturaHeaders
                .AsNoTracking()
                .Where(f =>
                    f.IdEmpresa == idEmpresa
                    && f.TipoFactura == "Credito"
                    && f.Estado == "Pendiente"
                    && f.EstaCancelada == false
                    && f.IdTipoDocumentos == TipoDocumentoFactura)
                .SumAsync(f => (decimal?)f.Pendiente) ?? 0;

            var cuentasPorPagar = await _context.OrdenCompraHeaders
                .AsNoTracking()
                .Where(h =>
                    h.IdEmpresa == idEmpresa
                    && h.IdTipoDocumentos == TipoDocumentoFacturaCompra
                    && h.Pendiente > 0
                    && h.Estado != "BORRADOR"
                    && h.Estado != "ANULADA"
                    && h.Estado != "PAGADA")
                .SumAsync(h => (decimal?)h.Pendiente) ?? 0;

            dto.Indicadores = new DashboardGerencialIndicadoresDto
            {
                Caja = Round(caja),
                Bancos = Round(bancos),
                ValorInventario = Round(valorInventario),
                ValorActivosFijos = Round(valorActivosFijos),
                CuentasPorCobrar = Round(cuentasPorCobrar),
                CuentasPorPagar = Round(cuentasPorPagar)
            };

            dto.Charts.VentasVsCostosVsUtilidad = new List<SerieNombreMontoDto>
            {
                new() { Nombre = "Ventas", Monto = dto.Pl.VentasBrutas },
                new() { Nombre = "Costos", Monto = dto.Pl.CostoVenta },
                new() { Nombre = "Utilidad bruta", Monto = dto.Pl.UtilidadBruta }
            };

            var ventasPorDia = headersMes
                .GroupBy(h => h.FechaInseccion.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Total));

            for (var d = desde; d <= hoy && d <= hasta; d = d.AddDays(1))
            {
                ventasPorDia.TryGetValue(d, out var montoDia);
                dto.Charts.EvolucionVentasMes.Add(new SerieDiariaDto
                {
                    Fecha = d.ToString("dd/MM"),
                    Monto = Round(montoDia)
                });
            }

            dto.Charts.DistribucionGastos = gastos
                .GroupBy(g => string.IsNullOrWhiteSpace(g.TipoGasto) ? "Sin clasificar" : g.TipoGasto!)
                .Select(g => new SerieNombreMontoDto { Nombre = g.Key, Monto = Round(g.Sum(x => x.Monto)) })
                .OrderByDescending(x => x.Monto)
                .Take(10)
                .ToList();

            dto.Charts.DistribucionPerdidas = perdidasDetalles
                .GroupBy(x => x.Nombre)
                .Select(g => new SerieNombreMontoDto { Nombre = g.Key, Monto = Round(g.Sum(x => x.Monto)) })
                .OrderByDescending(x => x.Monto)
                .Take(10)
                .ToList();

            dto.Charts.ComisionesPorEmpleado = comisionesDetalle
                .Where(c => c.TotalComisiones > 0)
                .OrderByDescending(c => c.TotalComisiones)
                .Select(c => new SerieNombreMontoDto
                {
                    Nombre = string.IsNullOrWhiteSpace(c.Empleados) ? $"Empleado {c.IdEmpleado}" : c.Empleados,
                    Monto = Round(c.TotalComisiones)
                })
                .ToList();

            dto.Charts.TopProductosRentables = BuildTopRentables(detallesMes, inventariablesOnly: true);
            if (dto.Charts.TopProductosRentables.Count == 0)
                dto.Charts.TopProductosRentables = BuildTopRentables(detallesMes, inventariablesOnly: false);

            dto.Charts.TopProductosPerdidas = dto.Charts.DistribucionPerdidas.Take(8).ToList();

            // Flujo de efectivo: entradas vs salidas (sin transferencias internas)
            var movsTesoreria = await _context.MovimientoFinanciero
                .AsNoTracking()
                .Where(m =>
                    m.IdEmpresa == idEmpresa
                    && m.Estado == "CONFIRMADO"
                    && m.FechaMovimiento >= desde
                    && m.FechaMovimiento < finExclusivo
                    && m.TipoMovimiento != "TRANSFERENCIA")
                .Select(m => new { m.TipoMovimiento, m.Monto })
                .ToListAsync();

            var entradas = movsTesoreria
                .Where(m => string.Equals(m.TipoMovimiento, "ENTRADA", StringComparison.OrdinalIgnoreCase))
                .Sum(m => m.Monto);
            var salidas = movsTesoreria
                .Where(m => string.Equals(m.TipoMovimiento, "SALIDA", StringComparison.OrdinalIgnoreCase))
                .Sum(m => m.Monto);

            dto.Charts.FlujoEfectivo = new List<SerieNombreMontoDto>
            {
                new() { Nombre = "Entradas", Monto = Round(entradas) },
                new() { Nombre = "Salidas", Monto = Round(salidas) },
                new() { Nombre = "Neto", Monto = Round(entradas - salidas) }
            };

            dto.Charts.EstadoResultados = BuildEstadoResultados(dto.Pl);

            return dto;
        }

        private static bool EsOtroIngresoEconomico(string? categoria, int? idFacturaHeader)
        {
            // Cobros de factura / POS nunca deben aumentar la utilidad aquí
            if (idFacturaHeader.HasValue && idFacturaHeader.Value > 0)
                return false;

            var cat = (categoria ?? "").Trim();
            if (string.IsNullOrEmpty(cat))
                return false;

            if (CategoriasIngresosExcluidas.Contains(cat))
                return false;

            return CategoriasOtrosIngresosReales.Contains(cat);
        }

        private static List<ProductoRentabilidadDto> BuildTopRentables(
            List<DetalleVentaRow> detalles,
            bool inventariablesOnly)
        {
            var query = inventariablesOnly
                ? detalles.Where(EsProductoInventariable)
                : detalles.AsEnumerable();

            return query
                .GroupBy(d => new { d.IdProducto, d.Nombre })
                .Select(g =>
                {
                    var ventas = g.Sum(x => x.VentaNeta);
                    var costo = g.Where(EsProductoInventariable).Sum(x => x.Cantidad * x.PrecioCompra);
                    var margen = ventas - costo;
                    return new ProductoRentabilidadDto
                    {
                        IdProducto = g.Key.IdProducto,
                        Nombre = g.Key.Nombre,
                        Cantidad = g.Sum(x => x.Cantidad),
                        VentasNetas = Round(ventas),
                        Costo = Round(costo),
                        Margen = Round(margen),
                        RentabilidadPct = ventas > 0 ? Round(margen / ventas * 100m) : 0
                    };
                })
                .OrderByDescending(x => x.Margen)
                .Take(8)
                .ToList();
        }

        private static List<EstadoResultadosPasoDto> BuildEstadoResultados(DashboardGerencialPlDto pl)
        {
            var pasos = new List<EstadoResultadosPasoDto>();
            decimal acum = 0;

            void Add(string concepto, decimal monto, string tipo)
            {
                if (tipo == "base")
                    acum = monto;
                else if (tipo == "resta")
                    acum -= monto;
                else if (tipo == "suma")
                    acum += monto;
                else if (tipo is "subtotal" or "total")
                    acum = monto;

                pasos.Add(new EstadoResultadosPasoDto
                {
                    Concepto = concepto,
                    Monto = Round(monto),
                    Acumulado = Round(acum),
                    Tipo = tipo
                });
            }

            Add("Ventas brutas", pl.VentasBrutas, "base");
            Add("Costo de venta", pl.CostoVenta, "resta");
            Add("Utilidad bruta", pl.UtilidadBruta, "subtotal");
            Add("Gastos operativos", pl.GastosOperativos, "resta");
            Add("Comisiones", pl.Comisiones, "resta");
            Add("Pérdidas de inventario", pl.PerdidasInventario, "resta");
            if (pl.OtrosEgresos > 0)
                Add("Otros egresos", pl.OtrosEgresos, "resta");
            if (pl.OtrosIngresos > 0)
                Add("Otros ingresos", pl.OtrosIngresos, "suma");
            Add("Utilidad operativa", pl.UtilidadOperativa, "total");

            return pasos;
        }

        private static bool EsProductoInventariable(DetalleVentaRow d)
        {
            if (d.EsServicio) return false;
            if (string.Equals(d.TipoComportamiento, "Servicio", StringComparison.OrdinalIgnoreCase))
                return false;
            if (string.Equals(d.TipoComportamiento, "Gasto", StringComparison.OrdinalIgnoreCase))
                return false;
            if (string.Equals(d.TipoComportamiento, "ActivoFijo", StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }

        private static decimal Round(decimal value) =>
            Math.Round(value, 2, MidpointRounding.AwayFromZero);

        private class DetalleVentaRow
        {
            public int IdProducto { get; set; }
            public string Nombre { get; set; } = "";
            public decimal Cantidad { get; set; }
            public decimal VentaNeta { get; set; }
            public decimal PrecioCompra { get; set; }
            public bool EsServicio { get; set; }
            public string? TipoComportamiento { get; set; }
        }
    }
}
