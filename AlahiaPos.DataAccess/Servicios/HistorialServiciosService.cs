using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class HistorialServiciosService : IHistorialServicios
    {
        private readonly IRepository<FacturaHeaders> _facturas;
        private readonly IRepository<FacturaDetalles> _detalles;
        private readonly IRepository<Productos> _productos;
        private readonly IRepository<Usuarios> _usuarios;
        private readonly IRepository<Empleados> _empleados;

        public HistorialServiciosService(
            IRepository<FacturaHeaders> facturas,
            IRepository<FacturaDetalles> detalles,
            IRepository<Productos> productos,
            IRepository<Usuarios> usuarios,
            IRepository<Empleados> empleados)
        {
            _facturas = facturas;
            _detalles = detalles;
            _productos = productos;
            _usuarios = usuarios;
            _empleados = empleados;
        }

        public async Task<IEnumerable<HistorialServicioClienteDto>> GetHistorialAsync(
            int idEmpresa,
            int idCliente,
            DateTime? desde,
            DateTime? hasta)
        {
            if (idEmpresa <= 0 || idCliente <= 0)
            {
                return Array.Empty<HistorialServicioClienteDto>();
            }

            var facturas = await _facturas.GetAllByExpresionAsync(
                f =>
                    f.IdEmpresa == idEmpresa &&
                    f.IDCliente == idCliente &&
                    f.IdTipoDocumentos == 1 &&
                    f.EstaCancelada != true &&
                    (!desde.HasValue || f.FechaInseccion.Date >= desde.Value.Date) &&
                    (!hasta.HasValue || f.FechaInseccion.Date <= hasta.Value.Date),
                "Clientes");

            var facturasOrdenadas = facturas
                .OrderByDescending(f => f.FechaInseccion)
                .ThenByDescending(f => f.IdFacturaHeader)
                .ToList();

            if (!facturasOrdenadas.Any())
            {
                return Array.Empty<HistorialServicioClienteDto>();
            }

            var productosEmpresa = await _productos.GetAllByExpresionAsync(
                p => p.IdEmpresa == idEmpresa && p.EsServicio);

            var productosServicio = productosEmpresa
                .ToDictionary(p => p.IdProducto);

            var usuariosCache = new Dictionary<int, string?>();
            var resultado = new List<HistorialServicioClienteDto>();

            foreach (var factura in facturasOrdenadas)
            {
                var lineasFactura = await _detalles.GetAllByExpresionAsync(
                    d => d.IdFacturaHeader == factura.IdFacturaHeader);

                var lineas = lineasFactura
                    .Where(d => d.StatuItem != true)
                    .OrderByDescending(d => d.IdFacturaDetalle);

                foreach (var linea in lineas)
                {
                    if (!productosServicio.TryGetValue(linea.IdProducto, out var producto))
                    {
                        continue;
                    }

                    var precioUnitario = linea.PrecioOferta > 0
                        ? linea.PrecioOferta
                        : producto.PrecioVenta;

                    resultado.Add(new HistorialServicioClienteDto
                    {
                        IdFacturaHeader = factura.IdFacturaHeader,
                        IdFacturaDetalle = linea.IdFacturaDetalle,
                        FechaServicio = factura.FechaInseccion,
                        Hora = factura.Hora,
                        NumeroFactura = ObtenerNumeroFactura(factura),
                        EstadoFactura = factura.Estado ?? string.Empty,
                        IdCliente = factura.IDCliente,
                        NombreCliente = factura.NombreCuenta
                            ?? factura.Clientes?.NombreComercial
                            ?? string.Empty,
                        IdProducto = producto.IdProducto,
                        NombreServicio = producto.Nombre,
                        Cantidad = linea.Cantidad,
                        PrecioUnitario = precioUnitario,
                        Total = linea.SubTotal,
                        IdUsuario = factura.IdUsuario,
                        NombreUsuario = ObtenerNombreUsuario(
                            factura.IdUsuario,
                            usuariosCache)
                    });
                }
            }

            return resultado
                .OrderByDescending(r => r.FechaServicio)
                .ThenByDescending(r => r.IdFacturaDetalle)
                .ToList();
        }

        public async Task<UltimoServicioClienteDto?> GetUltimoServicioAsync(
            int idEmpresa,
            int idCliente)
        {
            var historial = await GetHistorialAsync(idEmpresa, idCliente, null, null);
            var ultimo = historial.FirstOrDefault();

            if (ultimo == null)
            {
                return null;
            }

            return new UltimoServicioClienteDto
            {
                IdFacturaDetalle = ultimo.IdFacturaDetalle,
                IdFacturaHeader = ultimo.IdFacturaHeader,
                FechaServicio = ultimo.FechaServicio,
                NombreServicio = ultimo.NombreServicio,
                PrecioUnitario = ultimo.PrecioUnitario,
                Cantidad = ultimo.Cantidad,
                Total = ultimo.Total,
                NumeroFactura = ultimo.NumeroFactura
            };
        }

        private static string ObtenerNumeroFactura(FacturaHeaders factura)
        {
            if (!string.IsNullOrWhiteSpace(factura.NumeroDocumento))
            {
                return factura.NumeroDocumento.Trim();
            }

            return factura.IdFacturaHeader.ToString();
        }

        private string? ObtenerNombreUsuario(
            int? idUsuario,
            Dictionary<int, string?> cache)
        {
            if (!idUsuario.HasValue || idUsuario.Value <= 0)
            {
                return null;
            }

            if (cache.TryGetValue(idUsuario.Value, out var nombreCache))
            {
                return nombreCache;
            }

            var usuario = _usuarios.GetById(idUsuario.Value);
            if (usuario == null)
            {
                cache[idUsuario.Value] = null;
                return null;
            }

            string? nombre = null;

            if (usuario.IdEmpleado > 0)
            {
                var empleado = _empleados.GetById(usuario.IdEmpleado);
                if (!string.IsNullOrWhiteSpace(empleado?.Nombre))
                {
                    nombre = empleado.Nombre.Trim();
                }
            }

            if (string.IsNullOrWhiteSpace(nombre))
            {
                nombre = usuario.UserName?.Trim();
            }

            cache[idUsuario.Value] = nombre;
            return nombre;
        }
    }
}
