using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IConducesService
    {
        Task<IEnumerable<ConduceDto>> ListarAsync(int idEmpresa, DateTime? desde, DateTime? hasta, int? idFactura, string? q);
        Task<ConduceDto?> GetByIdAsync(int idConduce, int idEmpresa);
        Task<IEnumerable<FacturaParaConduceDto>> FacturasDisponiblesAsync(int idEmpresa, DateTime? desde, DateTime? hasta, string? q);
        Task<IEnumerable<LineaPendienteEntregaDto>> LineasPendientesAsync(int idFactura, int idEmpresa);
        Task<EstadoEntregaFacturaDto> EstadoEntregaAsync(int idFactura, int idEmpresa);
        Task<ConduceDto> CrearAsync(CrearConduceRequest request);
        Task AnularAsync(int idConduce, int idEmpresa);
    }
}
