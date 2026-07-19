using AlahiaPos.Entities.Dto;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface INotasCredito
    {
        Task<NotasCreditoResultadoDto> CrearNotaCredito(
            CrearNotaCreditoDto dto);

        Task<TicketNotaCreditoDto?> GetTicketNotaCredito(
            int idNotaCredito,
            int idEmpresa);

        Task<IEnumerable<NotaCreditoListadoDto>> ListarNotasCredito(
            int idEmpresa,
            DateTime? desde,
            DateTime? hasta,
            bool soloConComprobante);
    }

    public class NotasCreditoResultadoDto
    {
        public int IdNotaCredito { get; set; }

        public string NumeroDocumento { get; set; } = "";

        public string NCF { get; set; } = "";

        public decimal Total { get; set; }

        public string Mensaje { get; set; } = "";
    }
}
