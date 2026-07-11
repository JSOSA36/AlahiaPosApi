using AlahiaPos.Entities.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IPlantillasDocumentosClinicos
    {
        Task<IEnumerable<PlantillasDocumentosClinicos>> GetByEmpresa(int idEmpresa);

        Task<PlantillasDocumentosClinicos?> GetById(int idPlantilla);

        Task Insert(PlantillasDocumentosClinicos plantilla);

        Task Update(int idPlantilla, PlantillasDocumentosClinicos plantilla);

        Task CambiarEstado(int idPlantilla, bool activa);
    }
}
