using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class AlmacenesServices : IAlmacenes
    {
        private readonly IRepository<Almacen> _repository;

        public AlmacenesServices(IRepository<Almacen> repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Almacen>> GetAllAlmacenes(
            int idEmpresa,
            int? idSucursal = null,
            IReadOnlyCollection<int>? idsSucursalesPermitidas = null)
        {
            var almacenes = await _repository.GetAllByExpresionAsync(
                x => x.IdEmpresa == idEmpresa
            );

            IEnumerable<Almacen> q = almacenes ?? Enumerable.Empty<Almacen>();

            if (idsSucursalesPermitidas != null)
            {
                q = q.Where(x =>
                    x.IdSucursal.HasValue
                    && idsSucursalesPermitidas.Contains(x.IdSucursal.Value));
            }
            else if (idSucursal is > 0)
            {
                q = q.Where(x => x.IdSucursal == idSucursal);
            }

            return q.ToList();
        }

        public async Task<Almacen?> GetAlmacenById(int idAlmacen)
        {
            return await _repository.GetByIdAsync(idAlmacen);
        }

        public async Task<Almacen?> GetAlmacenPrincipal(int idEmpresa, int? idSucursal = null)
        {
            var almacenes = await _repository.GetAllByExpresionAsync(
                x => x.IdEmpresa == idEmpresa && x.Activo
            );

            IEnumerable<Almacen> q = almacenes ?? Enumerable.Empty<Almacen>();
            if (idSucursal is > 0)
            {
                q = q.Where(x => x.IdSucursal == idSucursal);
            }

            return q
                .OrderByDescending(x => x.EsPrincipal)
                .ThenBy(x => x.IdAlmacen)
                .FirstOrDefault();
        }

        public async Task InsertAlmacen(Almacen almacen)
        {
            if (almacen.EsPrincipal)
            {
                await QuitarPrincipalDeOtros(
                    almacen.IdEmpresa,
                    almacen.IdSucursal,
                    0);
            }

            await _repository.Save(almacen);
        }

        public async Task UpdateAlmacen(int id, Almacen almacen)
        {
            if (almacen.EsPrincipal)
            {
                await QuitarPrincipalDeOtros(
                    almacen.IdEmpresa,
                    almacen.IdSucursal,
                    id);
            }

            _repository.Update(id, almacen);
        }

        public async Task DeleteAlmacen(int idAlmacen)
        {
            var almacen = await _repository.GetByIdAsync(idAlmacen);

            if (almacen == null)
            {
                return;
            }

            almacen.Activo = false;
            _repository.Update(idAlmacen, almacen);
        }

        private async Task QuitarPrincipalDeOtros(int idEmpresa, int? idSucursal, int idActual)
        {
            var almacenes = await _repository.GetAllByExpresionAsync(
                x =>
                    x.IdEmpresa == idEmpresa
                    &&
                    x.IdAlmacen != idActual
                    &&
                    x.EsPrincipal
            );

            foreach (var item in almacenes ?? Enumerable.Empty<Almacen>())
            {
                if (idSucursal is > 0)
                {
                    if (item.IdSucursal != idSucursal)
                        continue;
                }
                else if (item.IdSucursal is > 0)
                {
                    continue;
                }

                item.EsPrincipal = false;
                _repository.Update(item.IdAlmacen, item);
            }
        }
    }
}
