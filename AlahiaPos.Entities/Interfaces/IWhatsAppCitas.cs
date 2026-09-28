using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IWhatsAppCitas
    {
        bool EstaListo { get; }

        Task EnviarCitaAsync(WhatsAppCitaMensaje mensaje, CancellationToken ct = default);

        WhatsAppCitasEstadoDto ObtenerEstado();

        IReadOnlyList<WhatsAppCitasPlantillaDef> PlantillasMeta();

        /// <summary>
        /// Crea en Twilio las 4 plantillas si no existen y las envía a aprobación UTILITY de WhatsApp.
        /// </summary>
        Task<IReadOnlyList<WhatsAppCitasPlantillaRegistroDto>> AsegurarPlantillasAsync(CancellationToken ct = default);

        Task<WhatsAppCitasConsumoResumenDto> ResumenConsumoAsync(
            int? idEmpresa = null, DateTime? desde = null, DateTime? hasta = null, CancellationToken ct = default);
    }
}
