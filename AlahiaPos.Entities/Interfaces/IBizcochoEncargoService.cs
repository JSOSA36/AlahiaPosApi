using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IBizcochoEncargoService
    {
        Task<int> CreateAsync(RequestBizcochoEncargoDto request);

        Task<FacturaHeaderDto?> GetEncargoPrintByIdAsync(
            int idFacturaHeader,
            int idEmpresa);
        Task<CierreEncargoDiaDto>
GetCierreDelDiaAsync(int idEmpresa);
        Task<bool> UpdateAsync(UpdateBizcochoEncargoDto dto);

        Task<bool> DeleteAsync(int id, int idEmpresa);

        Task<FacturaHeaders?> GetByIdAsync(int id, int idEmpresa);

        Task<IEnumerable<FacturaHeaderDto>> GetAllAsync(int idEmpresa);

        Task<IEnumerable<FacturaHeaders>> GetPendientesAsync(int idEmpresa);

        Task<IEnumerable<FacturaHeaders>> GetPorFechaAsync(int idEmpresa, DateTime fecha);

        Task<IEnumerable<FacturaHeaders>> BuscarAsync(int idEmpresa, string filtro);

        Task RegistrarPagoAsync(
            int idEmpresa,
            int idEncargo,
            decimal monto,
            decimal itbis,
            decimal totalPago,
            string formaPago,
            string? tipoComprobante,
            string? rnc,
            string? nombreEmpresa);

        Task<bool> MarcarComoEntregadoAsync(int idEncargo, int idEmpresa);

        Task<decimal> GetTotalCobradoHoyAsync(int idEmpresa);

        Task<IEnumerable<FacturaHeaders>> GetPagadosDelDiaAsync(int idEmpresa);

        Task<IEnumerable<FacturaDto>> GetFacturasPagadasAsync(
            int idEmpresa,
            DateTime desde,
            DateTime hasta);
    }
}
