using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class AlmacenExistenciaServices : IAlmacenExistencia
    {
        private readonly IRepository<AlmacenExistencia> _existenciaRepository;
        private readonly IRepository<Almacen> _almacenRepository;
        private readonly IRepository<Productos> _productosRepository;

        public AlmacenExistenciaServices(
            IRepository<AlmacenExistencia> existenciaRepository,
            IRepository<Almacen> almacenRepository,
            IRepository<Productos> productosRepository)
        {
            _existenciaRepository = existenciaRepository;
            _almacenRepository = almacenRepository;
            _productosRepository = productosRepository;
        }

        public async Task<AlmacenExistencia?> GetExistencia(
            int idAlmacen,
            int idProducto,
            int idEmpresa)
        {
            return (
                await _existenciaRepository.GetAllByExpresionAsync(
                    x =>
                        x.IdAlmacen == idAlmacen
                        &&
                        x.IdProducto == idProducto
                        &&
                        x.IdEmpresa == idEmpresa
                )
            ).FirstOrDefault();
        }

        public async Task<decimal> GetTotalPorProducto(
            int idProducto,
            int idEmpresa,
            int? idSucursal = null)
        {
            var existencias =
                await _existenciaRepository.GetAllByExpresionAsync(
                    x =>
                        x.IdProducto == idProducto
                        &&
                        x.IdEmpresa == idEmpresa
                );

            var filas = existencias ?? Enumerable.Empty<AlmacenExistencia>();

            if (idSucursal is > 0)
            {
                var idsAlmacen = await IdsAlmacenDeSucursal(idEmpresa, idSucursal.Value);
                filas = filas.Where(x => idsAlmacen.Contains(x.IdAlmacen));
            }

            return filas.Sum(x => x.Cantidad);
        }

        public async Task<List<AlmacenExistenciaDto>> GetDetallePorProducto(
            int idProducto,
            int idEmpresa,
            int? idSucursal = null)
        {
            var existencias =
                await _existenciaRepository.GetAllByExpresionAsync(
                    x =>
                        x.IdProducto == idProducto
                        &&
                        x.IdEmpresa == idEmpresa
                );

            var almacenes =
                await _almacenRepository.GetAllByExpresionAsync(
                    x => x.IdEmpresa == idEmpresa && x.Activo
                );

            IEnumerable<Almacen> almacenesFiltrados =
                almacenes ?? Enumerable.Empty<Almacen>();

            if (idSucursal is > 0)
            {
                almacenesFiltrados = almacenesFiltrados
                    .Where(x => x.IdSucursal == idSucursal);
            }

            var mapaExistencia = (existencias ?? Enumerable.Empty<AlmacenExistencia>())
                .GroupBy(x => x.IdAlmacen)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.Cantidad)
                );

            return almacenesFiltrados
                .OrderByDescending(x => x.EsPrincipal)
                .ThenBy(x => x.Nombre)
                .Select(x => new AlmacenExistenciaDto
                {
                    IdAlmacen = x.IdAlmacen,
                    IdSucursal = x.IdSucursal,
                    NombreAlmacen = x.Nombre,
                    Cantidad = mapaExistencia.TryGetValue(x.IdAlmacen, out var cantidad)
                        ? cantidad
                        : 0,
                    EsPrincipal = x.EsPrincipal
                })
                .ToList();
        }

        private async Task<HashSet<int>> IdsAlmacenDeSucursal(int idEmpresa, int idSucursal)
        {
            var almacenes = await _almacenRepository.GetAllByExpresionAsync(
                x => x.IdEmpresa == idEmpresa && x.IdSucursal == idSucursal
            );

            return (almacenes ?? Enumerable.Empty<Almacen>())
                .Select(x => x.IdAlmacen)
                .ToHashSet();
        }

        public async Task AjustarExistencia(
            int idAlmacen,
            int idProducto,
            int idEmpresa,
            decimal delta)
        {
            var existencia = await GetExistencia(
                idAlmacen,
                idProducto,
                idEmpresa
            );

            if (existencia == null)
            {
                if (delta < 0)
                {
                    throw new System.Exception(
                        "No hay existencia en el almacén seleccionado."
                    );
                }

                existencia = new AlmacenExistencia
                {
                    IdAlmacen = idAlmacen,
                    IdProducto = idProducto,
                    IdEmpresa = idEmpresa,
                    Cantidad = delta
                };

                await _existenciaRepository.Save(existencia);
            }
            else
            {
                existencia.Cantidad += delta;

                if (existencia.Cantidad < 0)
                {
                    throw new System.Exception(
                        "Stock insuficiente en el almacén seleccionado."
                    );
                }

                _existenciaRepository.Update(
                    existencia.IdAlmacenExistencia,
                    existencia
                );
            }

            await SincronizarCantidadProducto(idProducto);
        }

        public async Task SincronizarCantidadProducto(int idProducto)
        {
            var producto =
                await _productosRepository.GetByIdAsync(idProducto);

            if (producto == null)
            {
                return;
            }

            producto.Cantidad =
                await GetTotalPorProducto(
                    idProducto,
                    producto.IdEmpresa
                );

            _productosRepository.Update(
                producto.IdProducto,
                producto
            );
        }
    }
}
