using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    /// <summary>
    /// Deja una empresa nueva lista para operar (parámetros, secuencias, almacén, cliente).
    /// </summary>
    public interface IEmpresaOperativaSeed
    {
        /// <param name="idEmpresaNueva">Empresa recién creada</param>
        /// <param name="idEmpresaPlantilla">Fuente de plantilla (default Clinica Dental Sena = 60 en Dev)</param>
        Task SeedDesdePlantillaAsync(int idEmpresaNueva, int idEmpresaPlantilla = 60);
    }
}
