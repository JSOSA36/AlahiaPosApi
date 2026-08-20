using AlahiaPos.Entities.Dto;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IFichaClinica
    {
        Task<FichaClinicaVistaDto?> GetVista(int idEmpresa, int idCliente);

        Task<FichaClinicaVistaDto> Guardar(FichaClinicaDto dto);
    }
}
