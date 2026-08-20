using System.Threading;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public class VoucherMontoLectura
    {
        public bool Ok { get; set; }
        public decimal? Monto { get; set; }
        /// <summary>DOP | USD</summary>
        public string Moneda { get; set; } = "DOP";
        public string? Error { get; set; }
        public string? Raw { get; set; }
    }

    /// <summary>Lee el monto de un voucher bancario desde imagen (visión IA).</summary>
    public interface IVoucherMontoReader
    {
        Task<VoucherMontoLectura> LeerMontoAsync(
            byte[] imagenBytes,
            string? contentType,
            CancellationToken ct = default);
    }
}
