using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface ICargoPagoService
    {
        Task<IReadOnlyList<CargoPagoRegla>> ListarAsync(int idEmpresa, CancellationToken ct = default);

        Task<CargoPagoRegla> GuardarAsync(CargoPagoRegla regla, CancellationToken ct = default);

        Task EliminarAsync(int idEmpresa, int id, CancellationToken ct = default);

        CargoPagoCalcularResult Calcular(
            IEnumerable<CargoPagoRegla> reglas,
            IEnumerable<string?> metodos,
            decimal baseCalculo);

        Task<CargoPagoCalcularResult> CalcularAsync(
            int idEmpresa,
            IEnumerable<string?> metodos,
            decimal baseCalculo,
            CancellationToken ct = default);

        Task<CargoPagoCalcularResult> AplicarEnFacturaAsync(
            FacturaHeaders header,
            IEnumerable<string?> metodos,
            CancellationToken ct = default);
    }
}
