using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;

namespace AlahiaPos.DataAccess.Servicios
{
    public class CuentaContableService : ICuentaContableService
    {
        private readonly IRepository<CuentaContable> _repository;

        public CuentaContableService(IRepository<CuentaContable> repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<CuentaContable>> GetByEmpresaAsync(int idEmpresa)
        {
            return await _repository.GetAllByExpresionAsync(c => c.IdEmpresa == idEmpresa);
        }

        public async Task<IEnumerable<CuentaContable>> GetArbolByEmpresaAsync(int idEmpresa)
        {
            var cuentas = (await GetByEmpresaAsync(idEmpresa))
                .OrderBy(c => c.Codigo)
                .ToList();

            var lookup = cuentas.ToLookup(c => c.IdCuentaPadre);

            foreach (var cuenta in cuentas)
            {
                cuenta.Hijos = lookup[cuenta.IdCuentaContable].ToList();
            }

            return cuentas.Where(c => c.IdCuentaPadre == null).ToList();
        }

        public async Task<CuentaContable?> GetByIdAsync(int id, int idEmpresa)
        {
            var cuenta = await _repository.GetByIdAsync(id);
            if (cuenta == null || cuenta.IdEmpresa != idEmpresa)
                return null;

            return cuenta;
        }

        public async Task<int> CreateAsync(CuentaContable entity)
        {
            ValidarCuenta(entity);

            if (entity.IdCuentaPadre.HasValue)
            {
                var padre = await _repository.GetByIdAsync(entity.IdCuentaPadre.Value);
                if (padre == null || padre.IdEmpresa != entity.IdEmpresa)
                    throw new InvalidOperationException("La cuenta padre no existe para esta empresa.");

                entity.Nivel = padre.Nivel + 1;
                entity.TipoCuenta = padre.TipoCuenta;
            }
            else
            {
                entity.Nivel = 1;
            }

            entity.FechaInseccion = DateTime.Now;
            await _repository.Save(entity);
            return entity.IdCuentaContable;
        }

        public async Task UpdateAsync(CuentaContable entity)
        {
            ValidarCuenta(entity);

            var existente = await GetByIdAsync(entity.IdCuentaContable, entity.IdEmpresa);
            if (existente == null)
                throw new InvalidOperationException("Cuenta contable no encontrada.");

            if (entity.IdCuentaPadre.HasValue)
            {
                if (entity.IdCuentaPadre == entity.IdCuentaContable)
                    throw new InvalidOperationException("Una cuenta no puede ser padre de sí misma.");

                var padre = await _repository.GetByIdAsync(entity.IdCuentaPadre.Value);
                if (padre == null || padre.IdEmpresa != entity.IdEmpresa)
                    throw new InvalidOperationException("La cuenta padre no existe para esta empresa.");

                entity.Nivel = padre.Nivel + 1;
            }
            else
            {
                entity.Nivel = 1;
            }

            _repository.Update(entity.IdCuentaContable, entity);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(int id, int idEmpresa)
        {
            var cuenta = await GetByIdAsync(id, idEmpresa);
            if (cuenta == null)
                throw new InvalidOperationException("Cuenta contable no encontrada.");

            var hijos = await _repository.GetAllByExpresionAsync(c =>
                c.IdCuentaPadre == id && c.IdEmpresa == idEmpresa);

            if (hijos.Any())
                throw new InvalidOperationException("No se puede eliminar una cuenta que tiene subcuentas.");

            _repository.Delete(id);
            await Task.CompletedTask;
        }

        private static void ValidarCuenta(CuentaContable entity)
        {
            if (string.IsNullOrWhiteSpace(entity.Codigo))
                throw new InvalidOperationException("El código de la cuenta es obligatorio.");

            if (string.IsNullOrWhiteSpace(entity.Nombre))
                throw new InvalidOperationException("El nombre de la cuenta es obligatorio.");

            var tiposValidos = new[]
            {
                ContabilidadConstantes.TipoActivo,
                ContabilidadConstantes.TipoPasivo,
                ContabilidadConstantes.TipoCapital,
                ContabilidadConstantes.TipoIngresos,
                ContabilidadConstantes.TipoGastos,
                ContabilidadConstantes.TipoCostos
            };

            if (!tiposValidos.Contains(entity.TipoCuenta))
                throw new InvalidOperationException("Tipo de cuenta no válido.");
        }
    }
}
