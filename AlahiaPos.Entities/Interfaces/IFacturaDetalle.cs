using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IFacturaDetalle
    {

        Task<bool> ActualizarPrecioDetalle(int idDetalle, decimal nuevoPrecio);


            Task<IEnumerable<FacturaDetalles>> GetDetalleByIdHeaderAsync(int IdHeader);
        Task<bool> CambiarEmpleadoDetalle(int idFacturaDetalle, int idEmpleado);
        Task<IEnumerable<FacturaDetalles>> GetAllFacturaDetalle(int id, int IdEmpresa);
        public FacturaDetalles GetDetalle(int IdP, int IdHeader, int IdEmpresa);
        void InsertFactDNoasync(FacturaDetalles FacturaDetalle);
        Task<FacturaDetalles> GetAllFacturaDetalleById(int IdFacturaDetalle);
        void UpdateFacturaDetalle(int Id, FacturaDetalles  FacturaDetalle);
        //FacturaDetalles GetDetalle(int IdP, int IdHeader);
        FacturaDetalles GetDetallesId(int IdFacturaDetalle);
        
        IEnumerable<FacturaDetalles> GetDetalleByIdHeader(int IdHeader);
        Task InsertFactDetalle(FacturaDetalles FacturaDetalle);
        Task InsertFactDetalleRange(List<FacturaDetalles> FacturaDetalle);
        void DeleteFacturaDetalle(int IdFacturaDetalle);
        public Task<IEnumerable<FacturaDetalles>> GetOrdenesByIdMesa(int Id, int IdEmpresa);
        public Task<IEnumerable<FacturaDetalles>>? GetOrdenesByHeader(int Id, int IdEmpresa);
        public Task<IEnumerable<FacturaDetalles>>? GetOrdenesByHeader(int Id);

    }
}
