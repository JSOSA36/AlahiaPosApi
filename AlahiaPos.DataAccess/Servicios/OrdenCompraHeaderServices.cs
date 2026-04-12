using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class OrdenCompraHeaderServices : IOrdenCompraHeader
    {
        IRepository<OrdenCompraHeader> _repository;

        public OrdenCompraHeaderServices(IRepository<OrdenCompraHeader> repository)
        {
            _repository = repository;
        }

        public void DeleteOrdenCompraHeader(int IdCategorias)
        {
            _repository.Delete(IdCategorias);
        }

        public async Task<IEnumerable<OrdenCompraHeader>> GetAllOrdenCompraHeader()
        {
            return await _repository.GetAllByExpresionAsync(c => c.IdEmpresa == GlobalParamter.IdEmpresa);
        }
        public async Task<decimal> GetCuentaxPagar()
        {
            var result = await _repository.GetAllByExpresionAsync(c => c.IdTipoDocumentos == 11 && c.Estado ==
                "Pendiente" && c.IdEmpresa == GlobalParamter.IdEmpresa);

            if (result != null)
            {
                return result.Sum(c => c.Pendiente);
            }
            else
            {
                return 0;
            }
        }
        public async Task<IEnumerable<OrdenCompraHeader>> GetFacturasxPagar
            (DateTime? Desde, DateTime? Hasta, int IdProveedor)
        {

            IEnumerable<OrdenCompraHeader>? ordenCompraHeaders=null;

            if (IdProveedor != 0 && Desde != null && Hasta != null)
            {
                ordenCompraHeaders = await _repository.
                GetAllByExpresionAsync(c => c.IdTipoDocumentos == 11 && c.Estado ==
                "Pendiente" && c.IdEmpresa ==
                GlobalParamter.IdEmpresa && c.CondicionFactura == "Credito" && c.FechaInseccion >= Desde
                && c.FechaInseccion <= Hasta && c.IdProveedor == IdProveedor);
            }
            else if (IdProveedor == 0 && Desde == null && Hasta == null)
            {
                ordenCompraHeaders = await _repository.
                GetAllByExpresionAsync(c => c.IdTipoDocumentos == 11 && c.Estado ==
                "Pendiente" && c.IdEmpresa ==
                GlobalParamter.IdEmpresa && c.CondicionFactura == "Credito");
            }
            else if (IdProveedor == 0 && Desde != null && Hasta != null)
            {
                ordenCompraHeaders = await _repository.
                GetAllByExpresionAsync(c => c.IdTipoDocumentos == 11 && c.Estado ==
                "Pendiente" && c.IdEmpresa ==
                GlobalParamter.IdEmpresa && c.CondicionFactura == "Credito"
                && c.FechaInseccion >= Desde
                && c.FechaInseccion <= Hasta);
            }
            else if (IdProveedor != 0 && Desde == null && Hasta == null)
            {
                ordenCompraHeaders = await _repository.
                GetAllByExpresionAsync(c => c.IdTipoDocumentos == 11 && c.Estado ==
                "Pendiente" && c.IdEmpresa ==
                GlobalParamter.IdEmpresa && c.CondicionFactura == "Credito"
                && c.IdProveedor == IdProveedor
             );
            }

            return ordenCompraHeaders;

        } 
        public async Task<OrdenCompraHeader> 
            GetAllOrdenCompraHeaderById(int IdOrdenCompraHeader)
        {
            return await _repository.GetByIdAsync(IdOrdenCompraHeader);
        }

        public async Task 
            InsertOrdenCompraHeader(OrdenCompraHeader OrdenCompraHeader)
        {
            await _repository.Save(OrdenCompraHeader);
        }

        public void 
         UpdateOrdenCompraHeader(int Id, OrdenCompraHeader OrdenCompraHeader)
        {
            _repository.Update(Id, OrdenCompraHeader);
        }
    }
  }

