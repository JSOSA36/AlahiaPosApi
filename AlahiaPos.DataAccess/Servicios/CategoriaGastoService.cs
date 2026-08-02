using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class CategoriaGastoService : ICategoriaGastoService
    {
        private readonly IRepository<CategoriaGasto> _repository;

        public CategoriaGastoService(IRepository<CategoriaGasto> repository)
        {
            _repository = repository;
        }

        public async Task EnsureDefaultsAsync(int idEmpresa)
        {
            var existentes = (await _repository.GetAllByExpresionAsync(x =>
                x.IdEmpresa == idEmpresa)).ToList();

            if (existentes.Count > 0)
                return;

            foreach (var (nombre, orden) in CategoriaGastoDefaults.Iniciales)
            {
                await _repository.Save(new CategoriaGasto
                {
                    IdEmpresa = idEmpresa,
                    Nombre = nombre,
                    Orden = orden,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                });
            }
        }

        public async Task<IEnumerable<CategoriaGasto>> GetByEmpresaAsync(
            int idEmpresa,
            bool soloActivos = false)
        {
            await EnsureDefaultsAsync(idEmpresa);

            var list = await _repository.GetAllByExpresionAsync(x =>
                x.IdEmpresa == idEmpresa
                && (!soloActivos || x.Activo));

            return list
                .OrderBy(x => x.Orden)
                .ThenBy(x => x.Nombre)
                .ToList();
        }

        public async Task<CategoriaGasto?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<CategoriaGasto> CreateAsync(CategoriaGasto entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            entity.Nombre = (entity.Nombre ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(entity.Nombre))
                throw new InvalidOperationException("El nombre de la categoría es obligatorio.");

            var duplicado = await _repository.GetByExpresionAsync(x =>
                x.IdEmpresa == entity.IdEmpresa
                && x.Nombre == entity.Nombre);

            if (duplicado != null)
                throw new InvalidOperationException("Ya existe una categoría con ese nombre.");

            entity.Activo = true;
            entity.FechaCreacion = DateTime.UtcNow;
            if (entity.Orden <= 0)
                entity.Orden = 50;

            await _repository.Save(entity);
            return entity;
        }

        public async Task UpdateAsync(CategoriaGasto entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            entity.Nombre = (entity.Nombre ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(entity.Nombre))
                throw new InvalidOperationException("El nombre de la categoría es obligatorio.");

            var duplicado = await _repository.GetByExpresionAsync(x =>
                x.IdEmpresa == entity.IdEmpresa
                && x.Nombre == entity.Nombre
                && x.IdCategoriaGasto != entity.IdCategoriaGasto);

            if (duplicado != null)
                throw new InvalidOperationException("Ya existe una categoría con ese nombre.");

            _repository.Update(entity.IdCategoriaGasto, entity);
            await Task.CompletedTask;
        }

        public async Task SoftDeleteAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                throw new KeyNotFoundException("Categoría no encontrada.");

            entity.Activo = false;
            _repository.Update(entity.IdCategoriaGasto, entity);
            await Task.CompletedTask;
        }
    }
}
