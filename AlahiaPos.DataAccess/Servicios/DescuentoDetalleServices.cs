using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class DescuentoDetalleServices : IDescuentoDetalle
    {
        private readonly IRepository<DescuentoDetalle> _repository;

        public DescuentoDetalleServices(IRepository<DescuentoDetalle> repository)
        {
            _repository = repository;
        }

        // 🔹 Obtiene todos los detalles de un evento
        public async Task<IEnumerable<DescuentoDetalle>> GetDetallesByHeader(int IdDescuentoHeader)
        {
            return await _repository.GetAllByExpresionAsync(
                d => d.IdDescuentoHeader == IdDescuentoHeader
            );
        }

        // 🔹 Obtiene un detalle por Id
        public async Task<DescuentoDetalle> GetDetalleById(int IdDescuentoDetalle)
        {
            return await _repository.GetByIdAsync(IdDescuentoDetalle);
        }

        // 🔹 Inserta un detalle
        public async Task InsertDetalle(DescuentoDetalle detalle)
        {
            await _repository.Save(detalle);
        }

        // 🔹 Inserta varios detalles de forma eficiente
        public async Task InsertDetalles(IEnumerable<DescuentoDetalle> detalles)
        {
            foreach (var d in detalles)
            {
                await _repository.Save(d);
            }
        }

        // 🔹 Actualiza un detalle
        public void UpdateDetalle(int Id, DescuentoDetalle detalle)
        {
            _repository.Update(Id, detalle);
        }

        // 🔹 Elimina un detalle específico
        public void DeleteDetalle(int IdDescuentoDetalle)
        {
            _repository.Delete(IdDescuentoDetalle);
        }

        // 🔹 Elimina todos los detalles asociados a un Header
        public async Task DeleteDetallesByHeader(int IdDescuentoHeader)
        {
            var detalles = await _repository.GetAllByExpresionAsync(
                d => d.IdDescuentoHeader == IdDescuentoHeader
            );

            foreach (var detalle in detalles)
            {
                _repository.Delete(detalle.IdDescuentoDetalle);
            }
        }
    }
}
