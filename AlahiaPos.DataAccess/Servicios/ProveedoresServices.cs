using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;

namespace AlahiaPos.DataAccess.Servicios
{
    public class ProveedoresServices : IProveedores
    {
        private readonly IRepository<Proveedores> _repository;
        private readonly IRepository<OrdenCompraHeader> _ordenCompraRepository;
        private readonly IRepository<Gastos> _gastosRepository;
        private readonly IRepository<Productos> _productosRepository;

        public ProveedoresServices(
            IRepository<Proveedores> repository,
            IRepository<OrdenCompraHeader> ordenCompraRepository,
            IRepository<Gastos> gastosRepository,
            IRepository<Productos> productosRepository)
        {
            _repository = repository;
            _ordenCompraRepository = ordenCompraRepository;
            _gastosRepository = gastosRepository;
            _productosRepository = productosRepository;
        }

        public async Task<IEnumerable<Proveedores>> GetAllProveedores(int idEmpresa, bool soloActivos = true)
        {
            return await _repository.GetAllByExpresionAsync(p =>
                p.IdEmpresa == idEmpresa && (!soloActivos || p.IsActivo));
        }

        public async Task<Proveedores?> GetProveedorById(int idProveedor, int idEmpresa)
        {
            return await _repository.GetByExpresionAsync(p =>
                p.IdProveedor == idProveedor && p.IdEmpresa == idEmpresa);
        }

        public async Task<Proveedores> InsertProveedores(Proveedores proveedor)
        {
            if (proveedor.IdEmpresa <= 0)
                throw new ArgumentException("IdEmpresa es requerido.");

            if (string.IsNullOrWhiteSpace(proveedor.NombreComercial))
                throw new ArgumentException("El nombre comercial es requerido.");

            proveedor.FechaInseccion = DateTime.Now;
            proveedor.IsActivo = true;
            proveedor.RNC ??= string.Empty;
            proveedor.Telefono ??= string.Empty;
            proveedor.Direccion ??= string.Empty;
            proveedor.Email ??= string.Empty;
            proveedor.Nota ??= string.Empty;

            await _repository.Save(proveedor);
            return proveedor;
        }

        public async Task UpdateProveedores(int id, Proveedores proveedor, int idEmpresa)
        {
            var existente = await GetProveedorById(id, idEmpresa)
                ?? throw new KeyNotFoundException("Proveedor no encontrado.");

            if (string.IsNullOrWhiteSpace(proveedor.NombreComercial))
                throw new ArgumentException("El nombre comercial es requerido.");

            existente.RNC = proveedor.RNC ?? string.Empty;
            existente.NombreComercial = proveedor.NombreComercial;
            existente.Telefono = proveedor.Telefono ?? string.Empty;
            existente.Direccion = proveedor.Direccion ?? string.Empty;
            existente.Email = proveedor.Email ?? string.Empty;
            existente.Nota = proveedor.Nota ?? string.Empty;
            existente.IsActivo = proveedor.IsActivo;

            _repository.Update(id, existente);
            await Task.CompletedTask;
        }

        public async Task DesactivarProveedor(int idProveedor, int idEmpresa)
        {
            var existente = await GetProveedorById(idProveedor, idEmpresa)
                ?? throw new KeyNotFoundException("Proveedor no encontrado.");

            existente.IsActivo = false;
            _repository.Update(idProveedor, existente);
            await Task.CompletedTask;
        }

        public async Task<bool> TieneDocumentosAsociados(int idProveedor)
        {
            if (await _ordenCompraRepository.GetAny(o => o.IdProveedor == idProveedor))
                return true;

            if (await _gastosRepository.GetAny(g => g.IdProveedor == idProveedor))
                return true;

            if (await _productosRepository.GetAny(p => p.IdProveedor == idProveedor))
                return true;

            return false;
        }
    }
}
