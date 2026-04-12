using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class DescuentoAreaDetalleService : IDescuentoAreaDetalle
    {
        private readonly IRepository<DescuentoAreaDetalle> _repository;

        public DescuentoAreaDetalleService(IRepository<DescuentoAreaDetalle> repository)
        {
            _repository = repository;
        }

        // 🔹 Obtiene todas las áreas asignadas a un descuento
        public async Task<IEnumerable<DescuentoAreaDetalle>> GetAreasByHeader(int IdDescuentoHeader)
        {
            return await _repository.GetAllByExpresionAsync(
                d => d.IdDescuentoHeader == IdDescuentoHeader
            );
        }

        // 🔹 Obtiene un área-detalle por ID
        public async Task<DescuentoAreaDetalle> GetAreaDetalleById(int IdDescuentoAreaDetalle)
        {
            return await _repository.GetByIdAsync(IdDescuentoAreaDetalle);
        }

        // 🔹 Inserta un solo detalle de área
        public async Task InsertArea(DescuentoAreaDetalle detalle)
        {
            await _repository.Save(detalle);
        }

        // 🔹 Inserta varias áreas a la vez
        public async Task InsertAreas(IEnumerable<DescuentoAreaDetalle> detalles)
        {
            foreach (var d in detalles)
            {
                await _repository.Save(d);
            }
        }

        // 🔹 Actualiza un detalle de área
        public void UpdateArea(int Id, DescuentoAreaDetalle detalle)
        {
            _repository.Update(Id, detalle);
        }

        // 🔹 Elimina un detalle por Id
        public void DeleteArea(int IdDescuentoAreaDetalle)
        {
            _repository.Delete(IdDescuentoAreaDetalle);
        }

        // 🔹 Elimina TODAS las áreas asociadas a un Header
        public async Task DeleteAreasByHeader(int IdDescuentoHeader)
        {
            var detalles = await _repository.GetAllByExpresionAsync(
                d => d.IdDescuentoHeader == IdDescuentoHeader
            );

            foreach (var item in detalles)
            {
                _repository.Delete(item.IdDescuentoAreaDetalle);
            }
        }
    }
}
