using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IProveedores
    {
        Task<IEnumerable<Proveedores>> GetAllProveedores(int idEmpresa, bool soloActivos = true);
        Task<Proveedores?> GetProveedorById(int idProveedor, int idEmpresa);
        Task<Proveedores> InsertProveedores(Proveedores proveedor);
        Task UpdateProveedores(int id, Proveedores proveedor, int idEmpresa);
        Task DesactivarProveedor(int idProveedor, int idEmpresa);
        Task<bool> TieneDocumentosAsociados(int idProveedor);
    }
}
