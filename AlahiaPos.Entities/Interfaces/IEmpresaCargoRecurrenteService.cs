using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IEmpresaCargoRecurrenteService
    {
        Task<List<EmpresaCargoRecurrenteDto>> ListarPorEmpresaAsync(int idEmpresa, bool soloActivos = false);
        Task<EmpresaCargoRecurrenteDto> CrearAsync(CrearEmpresaCargoRecurrenteDto dto);
        Task<EmpresaCargoRecurrenteDto> ActualizarAsync(ActualizarEmpresaCargoRecurrenteDto dto);
        Task DesactivarAsync(int id, int? idUsuario = null);
        Task<SuscripcionCalculoFacturaDto> CalcularFacturaAsync(int idEmpresa, DateTime? fechaReferencia = null);
        Task RecalcularCicloAbiertoAsync(int idEmpresa);
    }
}
