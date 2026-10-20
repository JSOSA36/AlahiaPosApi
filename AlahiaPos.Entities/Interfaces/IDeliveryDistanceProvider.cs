using System.Threading;
using System.Threading.Tasks;
using AlahiaPos.Entities.Delivery;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IDeliveryDistanceProvider
    {
        string Nombre { get; }

        Task<DeliveryMedicion> MedirAsync(
            decimal origenLat,
            decimal origenLng,
            decimal destinoLat,
            decimal destinoLng,
            CancellationToken cancellationToken);
    }
}
