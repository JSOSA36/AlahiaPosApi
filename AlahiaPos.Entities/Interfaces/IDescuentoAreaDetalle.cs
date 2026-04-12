using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IDescuentoAreaDetalle
    {
        Task<IEnumerable<DescuentoAreaDetalle>> GetAreasByHeader(int IdDescuentoHeader);

        Task<DescuentoAreaDetalle> GetAreaDetalleById(int IdDescuentoAreaDetalle);

        Task InsertArea(DescuentoAreaDetalle detalle);

        Task InsertAreas(IEnumerable<DescuentoAreaDetalle> detalles);

        void UpdateArea(int Id, DescuentoAreaDetalle detalle);

        void DeleteArea(int IdDescuentoAreaDetalle);

        Task DeleteAreasByHeader(int IdDescuentoHeader);
    }
}
