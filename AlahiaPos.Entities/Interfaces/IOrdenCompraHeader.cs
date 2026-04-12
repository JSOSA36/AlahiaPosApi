using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IOrdenCompraHeader
    {
        Task<IEnumerable<OrdenCompraHeader>> GetAllOrdenCompraHeader();
        Task<decimal> GetCuentaxPagar();
        Task<OrdenCompraHeader> GetAllOrdenCompraHeaderById(int IdOrdenCompraHeader);
        void UpdateOrdenCompraHeader(int Id, OrdenCompraHeader OrdenCompraHeader);
        Task InsertOrdenCompraHeader(OrdenCompraHeader OrdenCompraHeader);
        void DeleteOrdenCompraHeader(int IdOrdenCompraHeader);
        public Task<IEnumerable<OrdenCompraHeader>> GetFacturasxPagar
            (DateTime? Desde, DateTime? Hasta, int IdProveedor);
    }
}
