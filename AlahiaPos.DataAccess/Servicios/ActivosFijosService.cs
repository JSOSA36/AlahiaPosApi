using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    public class ActivosFijosService : IActivosFijosService
    {
        private readonly AlahiaPosContext _context;
        private readonly IRepository<ActivoFijo> _repository;
        private readonly IRepository<Productos> _productos;
        private readonly IRepository<OrdenCompraHeader> _compras;
        private readonly IRepository<Almacen> _almacenes;

        public ActivosFijosService(
            AlahiaPosContext context,
            IRepository<ActivoFijo> repository,
            IRepository<Productos> productos,
            IRepository<OrdenCompraHeader> compras,
            IRepository<Almacen> almacenes)
        {
            _context = context;
            _repository = repository;
            _productos = productos;
            _compras = compras;
            _almacenes = almacenes;
        }

        public async Task<IEnumerable<ActivoFijoDto>> ListarAsync(
            int idEmpresa,
            string? estado = null,
            string? texto = null)
        {
            var q = _context.ActivosFijos
                .AsNoTracking()
                .Where(a => a.IdEmpresa == idEmpresa && a.Activo);

            if (!string.IsNullOrWhiteSpace(estado))
                q = q.Where(a => a.Estado == estado);

            if (!string.IsNullOrWhiteSpace(texto))
            {
                var t = texto.Trim().ToLower();
                q = q.Where(a =>
                    a.CodigoActivo.ToLower().Contains(t)
                    || a.Descripcion.ToLower().Contains(t)
                    || (a.NumeroSerie != null && a.NumeroSerie.ToLower().Contains(t))
                    || (a.Responsable != null && a.Responsable.ToLower().Contains(t)));
            }

            var lista = await q
                .OrderByDescending(a => a.FechaCreacion)
                .ThenByDescending(a => a.IdActivoFijo)
                .Take(500)
                .ToListAsync();

            var result = new List<ActivoFijoDto>();
            foreach (var a in lista)
                result.Add(await MapAsync(a));

            return result;
        }

        public async Task<ActivoFijoDto?> ObtenerPorIdAsync(int idActivoFijo, int idEmpresa)
        {
            var a = await _repository.GetByExpresionAsync(x =>
                x.IdActivoFijo == idActivoFijo && x.IdEmpresa == idEmpresa);

            return a == null ? null : await MapAsync(a);
        }

        public async Task<ActivoFijoDto> ActualizarAsync(int idActivoFijo, ActualizarActivoFijoRequest request)
        {
            var a = await _repository.GetByExpresionAsync(x =>
                x.IdActivoFijo == idActivoFijo && x.IdEmpresa == request.IdEmpresa)
                ?? throw new KeyNotFoundException("Activo fijo no encontrado.");

            if (!string.IsNullOrWhiteSpace(request.Descripcion))
                a.Descripcion = request.Descripcion.Trim();

            a.Marca = request.Marca?.Trim();
            a.Modelo = request.Modelo?.Trim();
            a.NumeroSerie = request.NumeroSerie?.Trim();
            a.Ubicacion = request.Ubicacion?.Trim();
            a.Responsable = request.Responsable?.Trim();
            a.Observacion = request.Observacion?.Trim();

            if (request.ValorResidual.HasValue)
                a.ValorResidual = request.ValorResidual.Value;

            if (request.VidaUtilMeses.HasValue)
                a.VidaUtilMeses = request.VidaUtilMeses.Value;

            if (request.IdCuentaContable.HasValue)
                a.IdCuentaContable = request.IdCuentaContable.Value;

            if (!string.IsNullOrWhiteSpace(request.Estado))
            {
                var est = request.Estado.Trim().ToUpperInvariant();
                a.Estado = est switch
                {
                    "PENDIENTE_DATOS" => EstadoActivoFijoConstantes.PendienteDatos,
                    "ACTIVO" => EstadoActivoFijoConstantes.Activo,
                    "BAJA" => EstadoActivoFijoConstantes.Baja,
                    "EN_MANTENIMIENTO" => EstadoActivoFijoConstantes.EnMantenimiento,
                    _ => a.Estado
                };
            }

            // Si completa serie/ubicación/responsable, promover a ACTIVO si estaba pendiente
            if (a.Estado == EstadoActivoFijoConstantes.PendienteDatos
                && !string.IsNullOrWhiteSpace(a.NumeroSerie)
                && !string.IsNullOrWhiteSpace(a.Ubicacion))
            {
                a.Estado = EstadoActivoFijoConstantes.Activo;
            }

            _repository.Update(a.IdActivoFijo, a);
            return await MapAsync(a);
        }

        public async Task<ResumenActivosFijosDto> ObtenerResumenAsync(int idEmpresa)
        {
            var lista = await _context.ActivosFijos
                .AsNoTracking()
                .Where(a => a.IdEmpresa == idEmpresa && a.Activo)
                .ToListAsync();

            return new ResumenActivosFijosDto
            {
                IdEmpresa = idEmpresa,
                CantidadActivos = lista.Count(a => a.Estado != EstadoActivoFijoConstantes.Baja),
                CantidadPendienteDatos = lista.Count(a => a.Estado == EstadoActivoFijoConstantes.PendienteDatos),
                CantidadDadosDeBaja = lista.Count(a => a.Estado == EstadoActivoFijoConstantes.Baja),
                ValorActivosFijos = lista
                    .Where(a => a.Estado != EstadoActivoFijoConstantes.Baja)
                    .Sum(a => a.ValorAdquisicion),
                ValorActivosOperativos = lista
                    .Where(a => a.Estado == EstadoActivoFijoConstantes.Activo)
                    .Sum(a => a.ValorAdquisicion)
            };
        }

        private async Task<ActivoFijoDto> MapAsync(ActivoFijo a)
        {
            string? nombreProducto = null;
            if (a.IdProducto.HasValue)
            {
                var p = await _productos.GetByIdAsync(a.IdProducto.Value);
                nombreProducto = p?.Nombre;
            }

            string? numeroDoc = null;
            if (a.IdOrdenCompraHeader.HasValue)
            {
                var h = await _compras.GetByIdAsync(a.IdOrdenCompraHeader.Value);
                numeroDoc = h?.NumeroDocumento;
            }

            string? nombreAlmacen = null;
            if (a.IdAlmacenRecepcion.HasValue)
            {
                var al = await _almacenes.GetByIdAsync(a.IdAlmacenRecepcion.Value);
                nombreAlmacen = al?.Nombre;
            }

            return new ActivoFijoDto
            {
                IdActivoFijo = a.IdActivoFijo,
                IdEmpresa = a.IdEmpresa,
                IdProducto = a.IdProducto,
                NombreProducto = nombreProducto,
                IdOrdenCompraHeader = a.IdOrdenCompraHeader,
                NumeroDocumentoCompra = numeroDoc,
                IdOrdenCompraDetalle = a.IdOrdenCompraDetalle,
                CodigoActivo = a.CodigoActivo,
                Descripcion = a.Descripcion,
                Marca = a.Marca,
                Modelo = a.Modelo,
                NumeroSerie = a.NumeroSerie,
                FechaAdquisicion = a.FechaAdquisicion,
                FechaRecepcion = a.FechaRecepcion,
                ValorAdquisicion = a.ValorAdquisicion,
                ValorResidual = a.ValorResidual,
                VidaUtilMeses = a.VidaUtilMeses,
                Estado = a.Estado,
                IdCuentaContable = a.IdCuentaContable,
                IdAlmacenRecepcion = a.IdAlmacenRecepcion,
                NombreAlmacenRecepcion = nombreAlmacen,
                Ubicacion = a.Ubicacion,
                Responsable = a.Responsable,
                Observacion = a.Observacion,
                FechaCreacion = a.FechaCreacion,
                Activo = a.Activo
            };
        }
    }
}
