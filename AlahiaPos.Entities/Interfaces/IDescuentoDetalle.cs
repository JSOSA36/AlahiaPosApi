using AlahiaPos.Entities.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IDescuentoDetalle
    {
        // 🔹 Obtiene todos los detalles (productos) de un evento de descuento
        Task<IEnumerable<DescuentoDetalle>> GetDetallesByHeader(int IdDescuentoHeader);

        // 🔹 Obtiene un detalle en particular
        Task<DescuentoDetalle> GetDetalleById(int IdDescuentoDetalle);

        // 🔹 Inserta un nuevo producto dentro de un evento de descuento
        Task InsertDetalle(DescuentoDetalle detalle);

        // 🔹 Inserta varios productos en lote (más eficiente)
        Task InsertDetalles(IEnumerable<DescuentoDetalle> detalles);

        // 🔹 Actualiza un producto dentro de un evento de descuento
        void UpdateDetalle(int Id, DescuentoDetalle detalle);

        // 🔹 Elimina un producto de un evento de descuento
        void DeleteDetalle(int IdDescuentoDetalle);

        // 🔹 Elimina todos los productos asociados a un evento de descuento
        Task DeleteDetallesByHeader(int IdDescuentoHeader);
    }
}
