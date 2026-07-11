using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System;
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

        public async Task<IEnumerable<Almacen>> GetAllAlmacenes(int idEmpresa)
        {
            return await _repository.GetAllByExpresionAsync(
                x => x.IdEmpresa == idEmpresa
            );
        }

        public async Task<Almacen?> GetAlmacenById(int idAlmacen)
        {
            return await _repository.GetByIdAsync(idAlmacen);
        }

        public async Task<Almacen?> GetAlmacenPrincipal(int idEmpresa)
        {
            var almacenes = await _repository.GetAllByExpresionAsync(
                x => x.IdEmpresa == idEmpresa && x.Activo
            );

            return almacenes?
                .OrderByDescending(x => x.EsPrincipal)
                .ThenBy(x => x.IdAlmacen)
                .FirstOrDefault();
        }

        public async Task InsertAlmacen(Almacen almacen)
        {
            if (almacen.EsPrincipal)
            {
                await QuitarPrincipalDeOtros(almacen.IdEmpresa, 0);
            }

            await _repository.Save(almacen);
        }

        public async Task UpdateAlmacen(int id, Almacen almacen)
        {
            if (almacen.EsPrincipal)
            {
                await QuitarPrincipalDeOtros(almacen.IdEmpresa, id);
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

        private async Task QuitarPrincipalDeOtros(int idEmpresa, int idActual)
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
                item.EsPrincipal = false;
                _repository.Update(item.IdAlmacen, item);
            }
        }
    }
}
