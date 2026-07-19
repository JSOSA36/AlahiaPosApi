using AlahiaPos.Entities.Interfaces;
using AlahiaPos.Entities.Interfaces.AlahiaAi;

namespace AlahiaPos.DataAccess.Servicios.AlahiaAi
{
    /// <summary>
    /// Única vía de datos para Alahia AI: servicios del ERP (sin DbContext).
    /// </summary>
    public class AlahiaAiErpGateway : IAlahiaAiErpGateway
    {
        private readonly IFacturaHeader _facturas;
        private readonly IProductos _productos;
        private readonly IGastos _gastos;
        private readonly IDashboardGerencialService _dashboard;
        private readonly IClientes _clientes;

        public AlahiaAiErpGateway(
            IFacturaHeader facturas,
            IProductos productos,
            IGastos gastos,
            IDashboardGerencialService dashboard,
            IClientes clientes)
        {
            _facturas = facturas;
            _productos = productos;
            _gastos = gastos;
            _dashboard = dashboard;
            _clientes = clientes;
        }

        public async Task<object> GetVentasHoyAsync(int idEmpresa, CancellationToken ct = default)
        {
            var total = await _facturas.GetVentaDelDia(idEmpresa);
            return new { fecha = DateTime.Today.ToString("yyyy-MM-dd"), total };
        }

        public async Task<object> GetClientesDebenAsync(int idEmpresa, CancellationToken ct = default)
        {
            var cxcs = (await _facturas.GetCuentasPorCobrar(idEmpresa))
                .Where(x => x.TotalDeuda > 0)
                .OrderByDescending(x => x.TotalDeuda)
                .Take(15)
                .Select(x => new { nombre = x.NombreCliente, saldo = x.TotalDeuda, telefono = x.Telefono })
                .ToList();

            return new
            {
                cantidad = cxcs.Count,
                totalPendiente = cxcs.Sum(x => x.saldo),
                clientes = cxcs
            };
        }

        public async Task<object> GetStockBajoAsync(int idEmpresa, CancellationToken ct = default)
        {
            var productos = (await _productos.GetAllProductos(idEmpresa))
                .Where(p => !p.EsServicio && p.ControlarStock && p.Cantidad <= Math.Max(p.Stock, 5))
                .OrderBy(p => p.Cantidad)
                .Take(15)
                .Select(p => new { nombre = p.Nombre, existencia = p.Cantidad, minimo = p.Stock })
                .ToList();

            return new { cantidad = productos.Count, productos };
        }

        public async Task<object> GetUtilidadMesAsync(int idEmpresa, CancellationToken ct = default)
        {
            var dash = await _dashboard.ObtenerMesActualAsync(idEmpresa);
            return new
            {
                periodo = dash.PeriodoLabel,
                ventasBrutas = dash.Pl.VentasBrutas,
                costoVenta = dash.Pl.CostoVenta,
                utilidadBruta = dash.Pl.UtilidadBruta,
                gastosOperativos = dash.Pl.GastosOperativos,
                utilidadOperativa = dash.Pl.UtilidadOperativa,
                margenOperativoPct = dash.Pl.MargenOperativoPct,
                cxC = dash.Indicadores.CuentasPorCobrar,
                caja = dash.Indicadores.Caja,
                bancos = dash.Indicadores.Bancos
            };
        }

        public async Task<object> GetGastosAltosAsync(int idEmpresa, CancellationToken ct = default)
        {
            var mes = DateTime.Today;
            var gastos = (await _gastos.GetAllGastos(idEmpresa))
                .Where(g => !g.EstaAnulado && g.FechaInseccion.Month == mes.Month && g.FechaInseccion.Year == mes.Year)
                .OrderByDescending(g => g.Monto)
                .Take(10)
                .Select(g => new
                {
                    descripcion = string.IsNullOrWhiteSpace(g.Detalle) ? (g.TipoGasto ?? "Gasto") : g.Detalle,
                    monto = g.Monto,
                    fecha = g.FechaInseccion.ToString("yyyy-MM-dd")
                })
                .ToList();

            var totalMes = await _gastos.TotalGastosDelMes(idEmpresa);
            return new { totalMes, gastos };
        }

        public async Task<object> GetProductosSinRotarAsync(int idEmpresa, CancellationToken ct = default)
        {
            // MVP: productos con stock y sin aparecer en top rentables del mes.
            var dash = await _dashboard.ObtenerMesActualAsync(idEmpresa);
            var topIds = dash.Charts.TopProductosRentables.Select(x => x.IdProducto).ToHashSet();
            var productos = (await _productos.GetAllProductos(idEmpresa))
                .Where(p => !p.EsServicio && p.Cantidad > 0 && !topIds.Contains(p.IdProducto))
                .OrderByDescending(p => p.Cantidad)
                .Take(12)
                .Select(p => new { nombre = p.Nombre, existencia = p.Cantidad })
                .ToList();

            return new
            {
                nota = "Productos con existencia que no figuran entre los más rentables del mes.",
                productos
            };
        }

        public async Task<object> GetCatalogoProductosAsync(int idEmpresa, CancellationToken ct = default)
        {
            var all = (await _productos.GetAllProductos(idEmpresa)).ToList();
            var activos = all.Where(p => p.IsActivo).ToList();
            var productos = activos.Where(p => !p.EsServicio).ToList();
            var servicios = activos.Where(p => p.EsServicio).ToList();

            return new
            {
                totalRegistrados = all.Count,
                totalActivos = activos.Count,
                productos = productos.Count,
                servicios = servicios.Count,
                inactivos = all.Count - activos.Count,
                ejemplosProductos = productos.OrderBy(p => p.Nombre).Take(8).Select(p => p.Nombre).ToList(),
                ejemplosServicios = servicios.OrderBy(p => p.Nombre).Take(8).Select(p => p.Nombre).ToList()
            };
        }

        public async Task<object> GetFlujoCajaAsync(int idEmpresa, CancellationToken ct = default)
        {
            var dash = await _dashboard.ObtenerMesActualAsync(idEmpresa);
            var caja = dash.Indicadores.Caja;
            var bancos = dash.Indicadores.Bancos;
            var cxC = dash.Indicadores.CuentasPorCobrar;
            var cxP = dash.Indicadores.CuentasPorPagar;
            var flujo = dash.Charts.FlujoEfectivo ?? new();
            var neto = flujo.FirstOrDefault(x => string.Equals(x.Nombre, "Neto", StringComparison.OrdinalIgnoreCase))?.Monto
                       ?? (caja + bancos);

            return new
            {
                periodo = dash.PeriodoLabel,
                caja,
                bancos,
                liquidez = caja + bancos,
                cuentasPorCobrar = cxC,
                cuentasPorPagar = cxP,
                flujoNeto = neto,
                detalleFlujo = flujo.Select(x => new { nombre = x.Nombre, monto = x.Monto }).ToList()
            };
        }

        public async Task<object> GetTopProductosAsync(int idEmpresa, CancellationToken ct = default)
        {
            var dash = await _dashboard.ObtenerMesActualAsync(idEmpresa);
            var top = (dash.Charts.TopProductosRentables ?? new())
                .OrderByDescending(x => x.Margen)
                .Take(10)
                .Select(x => new
                {
                    nombre = x.Nombre,
                    margen = x.Margen,
                    ventasNetas = x.VentasNetas,
                    rentabilidadPct = x.RentabilidadPct,
                    cantidad = x.Cantidad
                })
                .ToList();

            return new
            {
                periodo = dash.PeriodoLabel,
                cantidad = top.Count,
                productos = top
            };
        }

        public async Task<object> GetConteoClientesAsync(int idEmpresa, CancellationToken ct = default)
        {
            var all = (await _clientes.GetAllClientes(idEmpresa)).ToList();
            var activos = all.Where(c => c.Estado != false).ToList();
            var conCredito = activos.Count(c => (c.LimiteCredito ?? 0) > 0);

            return new
            {
                total = all.Count,
                activos = activos.Count,
                inactivos = all.Count - activos.Count,
                conLimiteCredito = conCredito,
                ejemplos = activos
                    .OrderBy(c => c.NombreComercial)
                    .Take(8)
                    .Select(c => c.NombreComercial ?? ("Cliente #" + c.IDCliente))
                    .ToList()
            };
        }

        public async Task<object> GetFacturasVencidasAsync(int idEmpresa, CancellationToken ct = default)
        {
            var pendientes = (await _facturas.GetFacturasXCobrar(null, null, 0, idEmpresa))
                .Select(f => new
                {
                    id = f.IdFacturaHeader,
                    cliente = f.Clientes?.NombreComercial ?? f.NombreCuenta ?? ("Cliente #" + f.IDCliente),
                    pendiente = f.Pendiente > 0 ? f.Pendiente : Math.Max(0, f.Total - f.Pagado),
                    fecha = f.FechaBencimiento != default ? f.FechaBencimiento : f.FechaInseccion
                })
                .Where(x => x.pendiente > 0)
                .OrderBy(x => x.fecha)
                .Take(20)
                .Select(x => new
                {
                    x.id,
                    x.cliente,
                    pendiente = x.pendiente,
                    dias = (DateTime.Today - x.fecha.Date).Days,
                    vencida = x.fecha.Date < DateTime.Today
                })
                .ToList();

            return new
            {
                cantidad = pendientes.Count,
                vencidas = pendientes.Count(x => x.vencida),
                facturas = pendientes
            };
        }

        public async Task<object> GetResumenOperativoAsync(int idEmpresa, CancellationToken ct = default)
        {
            var ventasHoy = await _facturas.GetVentaDelDia(idEmpresa);
            var utilidad = await GetUtilidadMesAsync(idEmpresa, ct);
            var stock = await GetStockBajoAsync(idEmpresa, ct);
            var cxC = await GetClientesDebenAsync(idEmpresa, ct);
            var vencidas = await GetFacturasVencidasAsync(idEmpresa, ct);

            var flujo = await GetFlujoCajaAsync(idEmpresa, ct);

            return new
            {
                hora = DateTime.Now.ToString("HH:mm"),
                ventasHoy,
                utilidad,
                stock,
                cxC,
                vencidas,
                flujo
            };
        }

        public Task<object> GetAyudaDocumentalAsync(string pregunta, CancellationToken ct = default)
        {
            var q = (pregunta ?? string.Empty).ToLowerInvariant();
            string respuesta;
            if (q.Contains("nota de crédito") || q.Contains("nota credito"))
                respuesta = "Para emitir una nota de crédito: ve a Devoluciones / Notas de crédito, selecciona la factura origen, indica los productos o montos a devolver y confirma. Luego puedes aplicarla a una factura pendiente.";
            else if (q.Contains("compra") || q.Contains("proveedor"))
                respuesta = "Para registrar una compra: entra a Compras → Facturas de compra → Nueva factura, elige proveedor, líneas, NCF si aplica y confirma. Esto impacta inventario y cuentas por pagar.";
            else if (q.Contains("cerrar") && q.Contains("caja"))
                respuesta = "Para cerrar caja: abre Finanzas y Caja → Cierre de caja, revisa movimientos del turno, captura conteo real y confirma el cierre. No olvides cuadrar diferencias.";
            else if (q.Contains("cobrar") || q.Contains("punto de venta") || q.Contains("pos"))
                respuesta = "En el POS agrega productos al carrito, elige cliente y condición de pago, luego Cobrar. Si usas crédito, la factura quedará en cuentas por cobrar.";
            else
                respuesta = "Puedo ayudarte con ventas, cobros, inventario, utilidad y gastos usando datos del ERP. Para procedimientos específicos del sistema, reformula (ej. cómo hago una nota de crédito) o crea un ticket de soporte con esta conversación.";

            return Task.FromResult<object>(new { respuesta, sugerirTicket = respuesta.Contains("ticket") });
        }
    }
}
