using AlahiaPos.Entities.Dto;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IHistorialServicios
    {
        Task<IEnumerable<HistorialServicioClienteDto>> GetHistorialAsync(
            int idEmpresa,
            int idCliente,
            DateTime? desde,
            DateTime? hasta);

        Task<UltimoServicioClienteDto?> GetUltimoServicioAsync(
            int idEmpresa,
            int idCliente);
    }
}
