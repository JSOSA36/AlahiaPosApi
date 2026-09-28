using AlahiaPos.Entities.Dto;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface INotasCredito
    {
        Task<NotasCreditoResultadoDto> CrearNotaCredito(
            CrearNotaCreditoDto dto);

        Task<NotasCreditoResultadoDto> CrearNotaCreditoComercialAsync(
            CrearNotaCreditoComercialDto dto);

        Task<bool> ExisteAnticipoPorCitaAsync(int idEmpresa, int idCita);

        Task<NotasCreditoResultadoDto> AnularNotaCredito(
            int idNotaCredito,
            int idEmpresa,
            int idUsuario,
            string? motivo);

        Task<TicketNotaCreditoDto?> GetTicketNotaCredito(
            int idNotaCredito,
            int idEmpresa);

        Task<IEnumerable<NotaCreditoListadoDto>> ListarNotasCredito(
            int idEmpresa,
            DateTime? desde,
            DateTime? hasta,
            bool soloConComprobante);

        Task<NotasCreditoResultadoDto> ReintentarEmisionAsync(
            int idNotaCredito,
            int idEmpresa,
            int idUsuario);

        Task<IEnumerable<ClienteSaldoAFavorListadoDto>> ListarSaldosAFavorAsync(
            int idEmpresa,
            int? idCliente = null);

        /// <summary>
        /// Busca saldo a favor disponible por e-NCF o número interno de NC.
        /// idCliente opcional: 0 = cualquier titular (consumo al portador).
        /// </summary>
        Task<ClienteSaldoAFavorListadoDto?> ObtenerSaldoAFavorPorNumeroAsync(
            int idEmpresa,
            int idCliente,
            string numero);

        /// <summary>
        /// Consume saldo a favor al pagar una venta. Idempotente por factura+saldo.
        /// No exige cliente: aplica también a facturas de consumo al portador.
        /// </summary>
        Task ConsumirSaldoAFavorEnVentaAsync(
            int idEmpresa,
            int idFacturaHeader,
            int idCliente,
            decimal monto,
            int? idSaldoAFavor,
            int? idNotaCredito,
            string? ncfONumero,
            int? idUsuario);

        /// <summary>
        /// Restaura saldos consumidos al anular una factura pagada con NotaCredito.
        /// </summary>
        Task RevertirConsumosSaldoPorFacturaAsync(
            int idEmpresa,
            int idFacturaHeader,
            int? idUsuario);
    }

    public class NotasCreditoResultadoDto
    {
        public int IdNotaCredito { get; set; }

        public string NumeroDocumento { get; set; } = "";

        public string NCF { get; set; } = "";

        public decimal Total { get; set; }

        public string Mensaje { get; set; } = "";

        public string? TrackId { get; set; }

        public string? EstadoDgii { get; set; }

        public bool EmisionPendiente { get; set; }

        public string? MensajeEmision { get; set; }

        public decimal SaldoDisponible { get; set; }

        public int? IdSaldoAFavor { get; set; }

        public decimal MontoAplicadoCxc { get; set; }
    }
}
