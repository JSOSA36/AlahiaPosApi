using AlahiaPos.Entities.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IPagoEmpresaService
    {
        Task CrearPagoAsync(CrearPagoDto dto);
        Task<List<PagoEmpresaDto>> ObtenerPagosAsync();
        Task<List<PagoEmpresaDto>> ObtenerPagosPorEmpresaAsync(int idEmpresa);
        Task ValidarPagoAsync(ValidarPagoDto dto);
    }
}
