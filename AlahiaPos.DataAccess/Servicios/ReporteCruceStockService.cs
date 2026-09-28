using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class ReporteCruceStockService : IReporteCruceStockService
    {
        private readonly IRepository<CajaCierre> _cierres;
        private readonly IRepository<CajaApertura> _aperturas;
        private readonly IRepository<Usuarios> _usuarios;
        private readonly IFacturaHeader _facturas;
        private readonly IRepository<MovimientosInventarioDetalle> _detallesMov;

        public ReporteCruceStockService(
            IRepository<CajaCierre> cierres,
            IRepository<CajaApertura> aperturas,
            IRepository<Usuarios> usuarios,
            IFacturaHeader facturas,
            IRepository<MovimientosInventarioDetalle> detallesMov)
        {
            _cierres = cierres;
            _aperturas = aperturas;
            _usuarios = usuarios;
            _facturas = facturas;
            _detallesMov = detallesMov;
        }

        public async Task<List<CruceStockVentaLineaDto>> ObtenerCruceAsync(
            int idEmpresa,
            DateTime desde,
            DateTime hasta,
            int? idProducto = null,
            int? idUsuario = null,
            int? idCajaCierre = null)
        {
            var desdeDia = desde.Date;
            var hastaDia = hasta.Date;

            var cierres = (await _cierres.GetAllByExpresionAsync(
                    x =>
                        x.IdEmpresa == idEmpresa
                        && x.FechaCierre.Date >= desdeDia
                        && x.FechaCierre.Date <= hastaDia
                        && (!idUsuario.HasValue || idUsuario.Value <= 0 || x.IdUsuario == idUsuario.Value)
                        && (!idCajaCierre.HasValue || idCajaCierre.Value <= 0 || x.IdCajaCierre == idCajaCierre.Value)
                ))
                .OrderBy(x => x.FechaCierre)
                .ToList();

            if (!cierres.Any())
                return new List<CruceStockVentaLineaDto>();

            var idsApertura = cierres
                .Select(x => x.IdCajaApertura)
                .Distinct()
                .ToList();

            var aperturas = (await _aperturas.GetAllByExpresionAsync(
                    x => idsApertura.Contains(x.IdCajaApertura)))
                .ToDictionary(x => x.IdCajaApertura);

            var idsUsuarios = cierres
                .Select(x => x.IdUsuario)
                .Distinct()
                .ToList();

            var usuarios = (await _usuarios.GetAllByExpresionAsync(
                    x => idsUsuarios.Contains(x.IdUsuario),
                    "Empleado"))
                .ToDictionary(
                    x => x.IdUsuario,
                    x =>
                        !string.IsNullOrWhiteSpace(x.UserName)
                            ? x.UserName
                            : (!string.IsNullOrWhiteSpace(x.Empleado?.Nombre)
                                ? x.Empleado!.Nombre!
                                : $"Usuario {x.IdUsuario}"));

            var lineas = new List<CruceStockVentaLineaDto>();
            var productosVendidosPorCaja =
                new Dictionary<int, List<CajaProductoDto>>();

            foreach (var cierre in cierres)
            {
                var vendidos = await _facturas.GetProductosPorCajaCierre(
                    idEmpresa,
                    cierre.IdUsuario,
                    cierre.IdCajaCierre);

                if (idProducto.HasValue && idProducto.Value > 0)
                {
                    vendidos = vendidos
                        .Where(p => p.IdProducto == idProducto.Value)
                        .ToList();
                }

                productosVendidosPorCaja[cierre.IdCajaCierre] = vendidos;
            }

            var idsProductos = productosVendidosPorCaja.Values
                .SelectMany(x => x)
                .Select(x => x.IdProducto)
                .Distinct()
                .ToList();

            if (!idsProductos.Any())
                return new List<CruceStockVentaLineaDto>();

            var minApertura = cierres
                .Select(c =>
                    aperturas.TryGetValue(c.IdCajaApertura, out var a)
                        ? a.FechaApertura
                        : c.FechaCierre)
                .Min()
                .AddDays(-7);

            var maxCierre = cierres.Max(c => c.FechaCierre).AddMinutes(1);

            var movimientos = (await _detallesMov.GetAllByExpresionAsync(
                    d =>
                        idsProductos.Contains(d.IdProducto)
                        && d.MovimientoInventario != null
                        && d.MovimientoInventario.IdEmpresa == idEmpresa
                        && d.MovimientoInventario.Activo
                        && d.MovimientoInventario.Fecha >= minApertura
                        && d.MovimientoInventario.Fecha <= maxCierre,
                    "MovimientoInventario"))
                .ToList();

            var movsPorProducto = movimientos
                .GroupBy(d => d.IdProducto)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var cierre in cierres)
            {
                if (!aperturas.TryGetValue(cierre.IdCajaApertura, out var apertura))
                    continue;

                if (!productosVendidosPorCaja.TryGetValue(cierre.IdCajaCierre, out var vendidos)
                    || vendidos == null
                    || !vendidos.Any())
                {
                    continue;
                }

                usuarios.TryGetValue(cierre.IdUsuario, out var nombreUsuario);

                foreach (var vendido in vendidos)
                {
                    movsPorProducto.TryGetValue(vendido.IdProducto, out var movsProd);
                    movsProd ??= new List<MovimientosInventarioDetalle>();

                    var ordenados = movsProd
                        .OrderBy(m => m.MovimientoInventario?.Fecha ?? m.Fecha)
                        .ThenBy(m => m.Id)
                        .ToList();

                    var stockAlAbrir = ResolverStockAlAbrir(
                        ordenados,
                        apertura.FechaApertura,
                        vendido.ExistenciaActual);

                    var stockAlCerrar = ResolverStockAlCerrar(
                        ordenados,
                        cierre.FechaCierre,
                        stockAlAbrir);

                    var otros = ResolverOtrosMovimientos(
                        ordenados,
                        apertura.FechaApertura,
                        cierre.FechaCierre);

                    var esperado =
                        stockAlAbrir
                        - vendido.CantidadVendida
                        + otros;

                    var diferencia = stockAlCerrar - esperado;
                    var cuadra = Math.Abs(diferencia) < 0.0001m;

                    lineas.Add(new CruceStockVentaLineaDto
                    {
                        IdCajaCierre = cierre.IdCajaCierre,
                        FechaApertura = apertura.FechaApertura,
                        FechaCierre = cierre.FechaCierre,
                        IdUsuario = cierre.IdUsuario,
                        Usuario = nombreUsuario ?? $"Usuario {cierre.IdUsuario}",
                        IdProducto = vendido.IdProducto,
                        Producto = vendido.Producto ?? string.Empty,
                        StockAlAbrir = stockAlAbrir,
                        CantidadVendida = vendido.CantidadVendida,
                        OtrosMovimientos = otros,
                        StockEsperado = esperado,
                        StockAlCerrar = stockAlCerrar,
                        Diferencia = diferencia,
                        Cuadra = cuadra
                    });
                }
            }

            return lineas
                .OrderByDescending(x => x.FechaCierre)
                .ThenBy(x => x.Producto)
                .ToList();
        }

        private static decimal ResolverStockAlAbrir(
            List<MovimientosInventarioDetalle> ordenados,
            DateTime fechaApertura,
            decimal fallback)
        {
            var anterior = ordenados
                .Where(m => (m.MovimientoInventario?.Fecha ?? m.Fecha) < fechaApertura)
                .LastOrDefault();

            if (anterior != null)
                return anterior.StockNuevo;

            var primeroEnTurno = ordenados
                .FirstOrDefault(m =>
                    (m.MovimientoInventario?.Fecha ?? m.Fecha) >= fechaApertura);

            if (primeroEnTurno != null)
                return primeroEnTurno.StockAnterior;

            return fallback;
        }

        private static decimal ResolverStockAlCerrar(
            List<MovimientosInventarioDetalle> ordenados,
            DateTime fechaCierre,
            decimal fallback)
        {
            var ultimo = ordenados
                .Where(m => (m.MovimientoInventario?.Fecha ?? m.Fecha) <= fechaCierre)
                .LastOrDefault();

            return ultimo?.StockNuevo ?? fallback;
        }

        private static decimal ResolverOtrosMovimientos(
            List<MovimientosInventarioDetalle> ordenados,
            DateTime fechaApertura,
            DateTime fechaCierre)
        {
            decimal neto = 0;

            foreach (var m in ordenados)
            {
                var fecha = m.MovimientoInventario?.Fecha ?? m.Fecha;
                if (fecha < fechaApertura || fecha > fechaCierre)
                    continue;

                var motivo = (m.MovimientoInventario?.Motivo ?? string.Empty)
                    .Trim()
                    .ToUpperInvariant();

                if (motivo == "VENTA")
                    continue;

                neto += m.StockNuevo - m.StockAnterior;
            }

            return neto;
        }
    }
}
