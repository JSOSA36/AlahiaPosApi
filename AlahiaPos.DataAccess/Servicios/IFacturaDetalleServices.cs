using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using DocumentFormat.OpenXml.Office2010.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class IFacturaDetalleServices : IFacturaDetalle
    {

        IRepository<FacturaDetalles> _Repository;
        IRepository<Empleados> _Empleados;
        IRepository<Productos> _Productos;
        IRepository<FacturaHeaders> _FacturaHeader;
        public IFacturaDetalleServices(IRepository<FacturaDetalles> repository, 
            IRepository<Empleados> Empleados,
            IRepository<Productos> productos,
            IRepository<FacturaHeaders> facturaHeader)
        {
            _Repository = repository;
            _Empleados = Empleados;
            _Productos = productos;
            _FacturaHeader = facturaHeader;
        }
        public async Task<bool> ActualizarPrecioDetalle(int idDetalle, decimal nuevoPrecio)
        {
            var detalle = await _Repository.GetByIdAsync(idDetalle);

            if (detalle == null)
                return false;

            // 🔥 actualizar precio detalle
            
            detalle.SubTotal = nuevoPrecio * detalle.Cantidad;

            _Repository.Update(idDetalle, detalle);

            // 🔥 recalcular total factura
            var detallesFactura = await _Repository
                .GetAllByExpresionAsync(x => x.IdFacturaHeader == detalle.IdFacturaHeader);

            var total = detallesFactura.Sum(x => x.SubTotal);
            
            var factura = await _FacturaHeader.GetByIdAsync(detalle.IdFacturaHeader);

            factura.Total = total;
            factura.Clientes = null;

            _FacturaHeader.Update(factura.IdFacturaHeader, factura);

            return true;
        }
        public FacturaDetalles GetDetalle(int IdP, int IdHeader, int IdEmpresa)
        {
            return  _Repository.GetByExpresion(c => c.IdProducto == IdP && c.IdFacturaHeader == IdHeader && c.IdEmpresa==IdEmpresa);
        }

        public async Task<bool> CambiarEmpleadoDetalle(int idFacturaDetalle, int idEmpleado)
        {
            var detalle = await _Repository.GetByIdAsync(idFacturaDetalle);

            if (detalle == null)
                return false;

            detalle.IdEmpleadoComision = idEmpleado;

             _Repository.Update(idFacturaDetalle, detalle);

            return true;
        }
        public IEnumerable<FacturaDetalles> GetDetalleByIdHeader(int IdHeader)
        {
            return _Repository.GetAllByExpresionNoAsync(c => c.IdFacturaHeader == IdHeader);
        }
        public async  Task<IEnumerable<FacturaDetalles>> GetDetalleByIdHeaderAsync(int IdHeader)
        {
            return await _Repository.GetAllByExpresionAsync(c => c.IdFacturaHeader == IdHeader);
        }
        public void DeleteFacturaDetalle(int IdFacturaDetalle)
        {
            _Repository.Delete(IdFacturaDetalle);
        }
        public async Task<IEnumerable<FacturaDetalles>> GetAllFacturaDetalle(int id, int IdEmpresa)
        {
            return await _Repository.GetAllByExpresionAsync(c => c.IdFacturaDetalle==id);
        }
        

        public async Task<FacturaDetalles> GetAllFacturaDetalleById(int IdFacturaDetalle)
        {
            return await _Repository.GetByIdAsync(IdFacturaDetalle);
        }
        public FacturaDetalles GetDetallesId(int IdFacturaDetalle)
        {
            return  _Repository.GetById(IdFacturaDetalle);
        }

        public async Task InsertFactDetalle(FacturaDetalles FacturaDetalle)
        {
            await _Repository.Save(FacturaDetalle);
        }
        public void InsertFactDNoasync(FacturaDetalles FacturaDetalle)
        {
             _Repository.SaveNoAsync(FacturaDetalle);
        }
        public async Task InsertFactDetalleRange( List<FacturaDetalles> FacturaDetalle)
        {

           
               
                await _Repository.Save(FacturaDetalle);
            
           
        }
        public void UpdateFacturaDetalle(int Id, FacturaDetalles FacturaDetalle)
        {
            _Repository.Update(Id, FacturaDetalle);
           
        }
        public Task<IEnumerable<FacturaDetalles>> GetOrdenesByIdMesa(int Id, int IdEmpresa)
        {
            return _Repository.GetAllByExpresionAsync(c => c.FacturaHeader.IdFacturaHeader == Id && c.IdEmpresa == IdEmpresa,
                "FacturaHeader", "Productos");
        }
        public Task<IEnumerable<FacturaDetalles>>? GetOrdenesByHeader(int Id,int IdEmpresa)
        {

            
                return _Repository.GetAllByExpresionAsync(c => c.IdFacturaHeader == Id
               , "Productos");
            
           
        }
        public Task<IEnumerable<FacturaDetalles>>? GetOrdenesByHeader(int Id)
        {


            return _Repository.GetAllByExpresionAsync(c => c.IdFacturaHeader == Id
           , "Productos");


        }
    }
}
