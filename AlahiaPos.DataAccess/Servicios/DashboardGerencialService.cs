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

        public async Task<DashboardGerencialDto> ObtenerMesActualAsync(
            int idEmpresa,
            SucursalConsultaScope? consulta = null)
        {
            var hoy = DateTime.Today;
            var desde = new DateTime(hoy.Year, hoy.Month, 1);
            var hasta = desde.AddMonths(1).AddDays(-1);
            var finExclusivo = desde.AddMonths(1);
            var cultura = new System.Globalization.CultureInfo("es-DO");

            var dto = new DashboardGerencialDto
            {
                IdEmpresa = idEmpresa,
                PeriodoDesde = desde,
                PeriodoHasta = hasta,
                PeriodoLabel = desde.ToString("MMMM yyyy", cultura),
                PeriodoHoyLabel = hoy.ToString("dddd, d 'de' MMMM 'de' yyyy", cultura),
                EsConsolidado = consulta?.EsConsolidado == true
            };

            if (consulta != null && consulta.IdsPermitidos.Count == 0)
                return dto;

            // Día primero en el payload: el propietario ve ingresos/utilidad de hoy al entrar.
            // Secuencial: DbContext no es thread-safe.
            dto.PlHoy = await CalcularPlPeriodoAsync(idEmpresa, hoy, hoy.AddDays(1), consulta);
            var mes = await ObtenerSnapshotPeriodoAsync(idEmpresa, desde, hasta, finExclusivo, consulta);
            dto.Pl = mes.Pl;

            var headersMes = mes.Headers;
            var detallesMes = mes.Detalles;
            var gastos = mes.Gastos;
            var comisionesDetalle = mes.ComisionesDetalle;
            var perdidasDetalles = mes.PerdidasDetalles;

            var saldos = (await _cuentasFinancieras.GetResumenSaldosAsync(idEmpresa)).ToList();
            // Tesorería y activos fijos son a nivel EMPRESA (no hay IdSucursal en cuentas).
            // Si el filtro es UNA sucursal, no atribuir caja/bancos/flujo de la empresa a esa sucursal.
            var mostrarTesoreriaEmpresa = consulta == null || consulta.EsConsolidado;
            var caja = mostrarTesoreriaEmpresa
                ? saldos
                    .Where(s => string.Equals(s.TipoCuenta, "CAJA", StringComparison.OrdinalIgnoreCase) && s.Activa)
                    .Sum(s => s.SaldoCalculado)
                : 0m;
            var bancos = mostrarTesoreriaEmpresa
                ? saldos
                    .Where(s => string.Equals(s.TipoCuenta, "BANCO", StringComparison.OrdinalIgnoreCase) && s.Activa)
                    .Sum(s => s.SaldoCalculado)
                : 0m;

            var valorInventario = await ValorInventarioAsync(idEmpresa, consulta);

            var valorActivosFijos = mostrarTesoreriaEmpresa
                ? await _context.ActivosFijos
                    .AsNoTracking()
                    .Where(a => a.IdEmpresa == idEmpresa && a.Activo && a.Estado != "Baja")
                    .SumAsync(a => (decimal?)a.ValorAdquisicion) ?? 0
                : 0m;

            var cuentasPorCobrar = await CuentasPorCobrarAsync(idEmpresa, consulta);
            var cuentasPorPagar = await CuentasPorPagarAsync(idEmpresa, consulta);

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
                new() { Nombre = "Ventas", Monto = dto.Pl.VentasNetas },
                new() { Nombre = "Costos", Monto = dto.Pl.CostoVenta },
                new() { Nombre = "Utilidad bruta", Monto = dto.Pl.UtilidadBruta }
            };

            var ventasPorDia = headersMes
                .GroupBy(h => h.FechaInseccion.Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Total - x.TotalItbis));

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

            // Flujo de efectivo: entradas vs salidas (sin transferencias internas).
            // Igual que caja/bancos: solo en vista consolidada (tesorería es de empresa).
            decimal entradas = 0m;
            decimal salidas = 0m;
            if (mostrarTesoreriaEmpresa)
            {
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

                entradas = movsTesoreria
                    .Where(m => string.Equals(m.TipoMovimiento, "ENTRADA", StringComparison.OrdinalIgnoreCase))
                    .Sum(m => m.Monto);
                salidas = movsTesoreria
                    .Where(m => string.Equals(m.TipoMovimiento, "SALIDA", StringComparison.OrdinalIgnoreCase))
                    .Sum(m => m.Monto);
            }

            dto.Charts.FlujoEfectivo = new List<SerieNombreMontoDto>
            {
                new() { Nombre = "Entradas", Monto = Round(entradas) },
                new() { Nombre = "Salidas", Monto = Round(salidas) },
                new() { Nombre = "Neto", Monto = Round(entradas - salidas) }
            };

            dto.Charts.EstadoResultados = BuildEstadoResultados(dto.Pl);

            if (consulta?.EsConsolidado == true)
            {
                var snapHoy = await ObtenerSnapshotPeriodoAsync(
                    idEmpresa, hoy, hoy, hoy.AddDays(1), consulta);
                dto.PorSucursal = await BuildPorSucursalAsync(
                    consulta, mes.Headers, snapHoy.Headers, idEmpresa);
            }

            return dto;
        }

        private async Task<DashboardGerencialPlDto> CalcularPlPeriodoAsync(
            int idEmpresa,
            DateTime desde,
            DateTime finExclusivo,
            SucursalConsultaScope? consulta)
        {
            var hasta = finExclusivo.AddDays(-1);
            var snap = await ObtenerSnapshotPeriodoAsync(idEmpresa, desde, hasta, finExclusivo, consulta);
            return snap.Pl;
        }

        private async Task<PeriodoSnapshot> ObtenerSnapshotPeriodoAsync(
            int idEmpresa,
            DateTime desde,
            DateTime hasta,
            DateTime finExclusivo,
            SucursalConsultaScope? consulta)
        {
            var ids = consulta?.IdsPermitidos?.ToList();
            var principal = consulta?.IdPrincipal ?? 0;

            var facturasQ = _context.FacturaHeaders
                .AsNoTracking()
                .Where(f =>
                    f.IdEmpresa == idEmpresa
                    && f.IdTipoDocumentos == TipoDocumentoFactura
                    && f.EstaCancelada == false
                    && f.Estado == "Pagada"
                    && f.FechaInseccion >= desde
                    && f.FechaInseccion < finExclusivo);
            if (ids != null)
                facturasQ = facturasQ.Where(f => ids.Contains(f.IdSucursal ?? principal));

            var headers = await facturasQ
                .Select(f => new HeaderVentaRow
                {
                    IdFacturaHeader = f.IdFacturaHeader,
                    Total = f.Total,
                    TotalItbis = f.TotalItbis,
                    TotalDescuento = f.TotalDescuento,
                    FechaInseccion = f.FechaInseccion,
                    IdSucursal = f.IdSucursal
                })
                .ToListAsync();

            var idsHeaders = headers.Select(h => h.IdFacturaHeader).ToList();

            var detalles = idsHeaders.Count == 0
                ? new List<DetalleVentaRow>()
                : await (
                    from d in _context.FacturaDetalles.AsNoTracking()
                    join p in _context.Productos.AsNoTracking() on d.IdProducto equals p.IdProducto
                    where idsHeaders.Contains(d.IdFacturaHeader)
                    select new DetalleVentaRow
                    {
                        IdFacturaHeader = d.IdFacturaHeader,
                        IdProducto = d.IdProducto,
                        Nombre = p.Nombre ?? "Sin nombre",
                        Cantidad = d.Cantidad - d.CantidadDevuelta,
                        VentaNeta = d.SubTotal - d.Itbis,
                        PrecioCompra = p.PrecioCompra,
                        EsServicio = p.EsServicio,
                        TipoComportamiento = p.TipoComportamiento
                    }
                ).ToListAsync();

            foreach (var d in detalles)
            {
                if (d.Cantidad < 0) d.Cantidad = 0;
            }

            // El descuento de cabecera no se reparte a las líneas. La venta neta
            // del periodo es lo facturado (Total − ITBIS), no la suma de SubTotal.
            AplicarDescuentoCabeceraALineas(headers, detalles);

            var descuentos = headers.Sum(h => h.TotalDescuento);
            var ventasNetas = headers.Sum(h => h.Total - h.TotalItbis);
            if (ventasNetas < 0)
                ventasNetas = 0;
            if (ventasNetas == 0 && detalles.Count > 0)
                ventasNetas = Math.Max(0, detalles.Sum(d => d.VentaNeta));

            var ventasBrutas = ventasNetas + descuentos;
            if (ventasBrutas == 0 && headers.Count > 0)
                ventasBrutas = headers.Sum(h => h.Total);

            var costoVenta = detalles
                .Where(EsProductoInventariable)
                .Sum(d => d.Cantidad * d.PrecioCompra);

            var utilidadBruta = ventasNetas - costoVenta;

            var gastosQ = _context.Gastos
                .AsNoTracking()
                .Where(g =>
                    g.IdEmpresa == idEmpresa
                    && g.EstaAnulado == false
                    && g.FechaInseccion >= desde
                    && g.FechaInseccion < finExclusivo);
            if (ids != null)
                gastosQ = gastosQ.Where(g => ids.Contains(g.IdSucursal ?? principal));

            var gastos = await gastosQ
                .Select(g => new GastoRow { Monto = g.Monto, TipoGasto = g.TipoGasto })
                .ToListAsync();

            var gastosOperativos = gastos.Sum(g => g.Monto);

            var comisionesDetalle = new List<ComisionesResultDto>();
            try
            {
                comisionesDetalle = _facturaHeader
                    .GetComisionesDetalle(desde, hasta, idEmpresa)
                    .ToList();
            }
            catch (Exception)
            {
                // Schema/data drift (p. ej. API local vs Prod) no debe tumbar el panel.
                comisionesDetalle = new List<ComisionesResultDto>();
            }
            var comisiones = comisionesDetalle.Sum(c => c.TotalComisiones);

            var perdidasQ =
                from m in _context.MovimientosInventario.AsNoTracking()
                from det in m.Detalles
                join p in _context.Productos.AsNoTracking() on det.IdProducto equals p.IdProducto into pj
                from p in pj.DefaultIfEmpty()
                where m.IdEmpresa == idEmpresa
                      && m.Activo
                      && m.Fecha >= desde
                      && m.Fecha < finExclusivo
                      // Solo merma explícita. AJUSTE/conteo es organización de stock, no pérdida.
                      && m.Motivo == "PERDIDA"
                select new { m.IdSucursal, m.IdSucursalDestino, p, det };

            var perdidasRaw = await perdidasQ.ToListAsync();
            if (ids != null)
            {
                perdidasRaw = perdidasRaw
                    .Where(x =>
                        ids.Contains(x.IdSucursal ?? principal)
                        || (x.IdSucursalDestino is > 0 && ids.Contains(x.IdSucursalDestino.Value)))
                    .ToList();
            }

            var perdidasDetalles = perdidasRaw
                .Select(x => new PerdidaRow
                {
                    Nombre = x.p != null ? (x.p.Nombre ?? "Sin nombre") : "Sin nombre",
                    Monto = x.det.SubTotal ?? (x.det.Cantidad * (x.det.Precio ?? 0))
                })
                .ToList();

            var perdidasInventario = perdidasDetalles.Sum(x => x.Monto);

            var ingresosQ = _context.Ingresos
                .AsNoTracking()
                .Where(i =>
                    i.IdEmpresa == idEmpresa
                    && i.EstaAnulado == false
                    && i.FechaRegistro >= desde
                    && i.FechaRegistro < finExclusivo);
            if (ids != null)
                ingresosQ = ingresosQ.Where(i => ids.Contains(i.IdSucursal ?? principal));

            var ingresos = await ingresosQ
                .Select(i => new { i.Categoria, i.Monto, i.IdFacturaHeader })
                .ToListAsync();

            var otrosIngresos = ingresos
                .Where(i => EsOtroIngresoEconomico(i.Categoria, i.IdFacturaHeader))
                .Sum(i => i.Monto);

            var otrosEgresosRaw = await _context.MovimientoFinanciero
                .AsNoTracking()
                .Where(m =>
                    m.IdEmpresa == idEmpresa
                    && m.TipoMovimiento == "SALIDA"
                    && m.Estado == "CONFIRMADO"
                    && m.FechaMovimiento >= desde
                    && m.FechaMovimiento < finExclusivo)
                .Select(m => new { m.Categoria, m.Monto })
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

            var pl = new DashboardGerencialPlDto
            {
                VentasBrutas = Round(ventasBrutas),
                Descuentos = Round(descuentos),
                VentasNetas = Round(ventasNetas),
                CostoVenta = Round(costoVenta),
                UtilidadBruta = Round(utilidadBruta),
                GastosOperativos = Round(gastosOperativos),
                Comisiones = Round(comisiones),
                PerdidasInventario = Round(perdidasInventario),
                OtrosIngresos = Round(otrosIngresos),
                OtrosEgresos = Round(otrosEgresos),
                UtilidadOperativa = Round(utilidadOperativa),
                MargenBrutoPct = ventasNetas > 0 ? Round(utilidadBruta / ventasNetas * 100m) : 0,
                MargenOperativoPct = ventasNetas > 0 ? Round(utilidadOperativa / ventasNetas * 100m) : 0
            };

            return new PeriodoSnapshot
            {
                Pl = pl,
                Headers = headers,
                Detalles = detalles,
                Gastos = gastos,
                ComisionesDetalle = comisionesDetalle,
                PerdidasDetalles = perdidasDetalles
            };
        }

        private async Task<decimal> ValorInventarioAsync(int idEmpresa, SucursalConsultaScope? consulta)
        {
            var ids = consulta?.IdsPermitidos?.ToList();
            var principal = consulta?.IdPrincipal ?? 0;

            var q =
                from e in _context.AlmacenExistencia.AsNoTracking()
                join p in _context.Productos.AsNoTracking() on e.IdProducto equals p.IdProducto
                join a in _context.Almacenes.AsNoTracking() on e.IdAlmacen equals a.IdAlmacen
                where e.IdEmpresa == idEmpresa
                      && !p.EsServicio
                      && (p.TipoComportamiento == null
                          || p.TipoComportamiento == "Inventario"
                          || p.TipoComportamiento == "")
                select new { e.Cantidad, p.PrecioCompra, a.IdSucursal };

            if (ids != null)
                q = q.Where(x => ids.Contains(x.IdSucursal ?? principal));

            return await q.SumAsync(x => (decimal?)(x.Cantidad * x.PrecioCompra)) ?? 0;
        }

        private async Task<decimal> CuentasPorCobrarAsync(int idEmpresa, SucursalConsultaScope? consulta)
        {
            var ids = consulta?.IdsPermitidos?.ToList();
            var principal = consulta?.IdPrincipal ?? 0;
            var q = _context.FacturaHeaders
                .AsNoTracking()
                .Where(f =>
                    f.IdEmpresa == idEmpresa
                    && f.TipoFactura == "Credito"
                    && f.Estado == "Pendiente"
                    && f.EstaCancelada == false
                    && f.IdTipoDocumentos == TipoDocumentoFactura);
            if (ids != null)
                q = q.Where(f => ids.Contains(f.IdSucursal ?? principal));
            return await q.SumAsync(f => (decimal?)f.Pendiente) ?? 0;
        }

        private async Task<decimal> CuentasPorPagarAsync(int idEmpresa, SucursalConsultaScope? consulta)
        {
            var ids = consulta?.IdsPermitidos?.ToList();
            var principal = consulta?.IdPrincipal ?? 0;
            var q = _context.OrdenCompraHeaders
                .AsNoTracking()
                .Where(h =>
                    h.IdEmpresa == idEmpresa
                    && h.IdTipoDocumentos == TipoDocumentoFacturaCompra
                    && h.Pendiente > 0
                    && h.Estado != "BORRADOR"
                    && h.Estado != "ANULADA"
                    && h.Estado != "PAGADA");
            if (ids != null)
                q = q.Where(h => ids.Contains(h.IdSucursal ?? principal));
            return await q.SumAsync(h => (decimal?)h.Pendiente) ?? 0;
        }

        private async Task<List<DashboardGerencialSucursalMontoDto>> BuildPorSucursalAsync(
            SucursalConsultaScope consulta,
            List<HeaderVentaRow> headersMes,
            List<HeaderVentaRow> headersHoy,
            int idEmpresa)
        {
            var principal = consulta.IdPrincipal;
            int Key(int? id) => id is > 0 ? id.Value : principal;

            var ventasMes = headersMes
                .GroupBy(h => Key(h.IdSucursal))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Total - x.TotalItbis));
            var ventasHoy = headersHoy
                .GroupBy(h => Key(h.IdSucursal))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Total - x.TotalItbis));

            var invRows = await (
                from e in _context.AlmacenExistencia.AsNoTracking()
                join p in _context.Productos.AsNoTracking() on e.IdProducto equals p.IdProducto
                join a in _context.Almacenes.AsNoTracking() on e.IdAlmacen equals a.IdAlmacen
                where e.IdEmpresa == idEmpresa
                      && !p.EsServicio
                      && (p.TipoComportamiento == null
                          || p.TipoComportamiento == "Inventario"
                          || p.TipoComportamiento == "")
                select new { a.IdSucursal, Valor = e.Cantidad * p.PrecioCompra }
            ).ToListAsync();

            var inv = invRows
                .Where(x => consulta.Incluye(x.IdSucursal))
                .GroupBy(x => Key(x.IdSucursal))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Valor));

            var cxcRows = await _context.FacturaHeaders
                .AsNoTracking()
                .Where(f =>
                    f.IdEmpresa == idEmpresa
                    && f.TipoFactura == "Credito"
                    && f.Estado == "Pendiente"
                    && f.EstaCancelada == false
                    && f.IdTipoDocumentos == TipoDocumentoFactura)
                .Select(f => new { f.IdSucursal, f.Pendiente })
                .ToListAsync();
            var cxc = cxcRows
                .Where(x => consulta.Incluye(x.IdSucursal))
                .GroupBy(x => Key(x.IdSucursal))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Pendiente));

            var cxpRows = await _context.OrdenCompraHeaders
                .AsNoTracking()
                .Where(h =>
                    h.IdEmpresa == idEmpresa
                    && h.IdTipoDocumentos == TipoDocumentoFacturaCompra
                    && h.Pendiente > 0
                    && h.Estado != "BORRADOR"
                    && h.Estado != "ANULADA"
                    && h.Estado != "PAGADA")
                .Select(h => new { h.IdSucursal, h.Pendiente })
                .ToListAsync();
            var cxp = cxpRows
                .Where(x => consulta.Incluye(x.IdSucursal))
                .GroupBy(x => Key(x.IdSucursal))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Pendiente));

            return consulta.Sucursales
                .Select(s => new DashboardGerencialSucursalMontoDto
                {
                    IdSucursal = s.IdSucursal,
                    Nombre = s.Nombre,
                    VentasNetas = Round(ventasMes.GetValueOrDefault(s.IdSucursal)),
                    VentasHoy = Round(ventasHoy.GetValueOrDefault(s.IdSucursal)),
                    ValorInventario = Round(inv.GetValueOrDefault(s.IdSucursal)),
                    CuentasPorCobrar = Round(cxc.GetValueOrDefault(s.IdSucursal)),
                    CuentasPorPagar = Round(cxp.GetValueOrDefault(s.IdSucursal))
                })
                .ToList();
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
            if (pl.Descuentos > 0)
                Add("Descuentos", pl.Descuentos, "resta");
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

        /// <summary>
        /// Prorratea el descuento de cabecera sobre las líneas para que
        /// rentabilidad por producto coincida con lo facturado.
        /// </summary>
        private static void AplicarDescuentoCabeceraALineas(
            List<HeaderVentaRow> headers,
            List<DetalleVentaRow> detalles)
        {
            if (headers.Count == 0 || detalles.Count == 0)
                return;

            var porHeader = headers.ToDictionary(h => h.IdFacturaHeader);
            foreach (var grupo in detalles.GroupBy(d => d.IdFacturaHeader))
            {
                if (!porHeader.TryGetValue(grupo.Key, out var header))
                    continue;

                var destino = header.Total - header.TotalItbis;
                if (destino < 0)
                    destino = 0;

                var lineas = grupo.ToList();
                var actual = lineas.Sum(x => x.VentaNeta);
                if (actual <= 0 || actual == destino)
                    continue;

                var factor = destino / actual;
                foreach (var linea in lineas)
                    linea.VentaNeta *= factor;
            }
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

        private class PeriodoSnapshot
        {
            public DashboardGerencialPlDto Pl { get; set; } = new();
            public List<HeaderVentaRow> Headers { get; set; } = new();
            public List<DetalleVentaRow> Detalles { get; set; } = new();
            public List<GastoRow> Gastos { get; set; } = new();
            public List<ComisionesResultDto> ComisionesDetalle { get; set; } = new();
            public List<PerdidaRow> PerdidasDetalles { get; set; } = new();
        }

        private class HeaderVentaRow
        {
            public int IdFacturaHeader { get; set; }
            public decimal Total { get; set; }
            public decimal TotalItbis { get; set; }
            public decimal TotalDescuento { get; set; }
            public DateTime FechaInseccion { get; set; }
            public int? IdSucursal { get; set; }
        }

        private class GastoRow
        {
            public decimal Monto { get; set; }
            public string? TipoGasto { get; set; }
        }

        private class PerdidaRow
        {
            public string Nombre { get; set; } = "";
            public decimal Monto { get; set; }
        }

        private class DetalleVentaRow
        {
            public int IdFacturaHeader { get; set; }
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
