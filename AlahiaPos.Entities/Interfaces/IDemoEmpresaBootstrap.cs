using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    /// <summary>
    /// Arma módulos + perfil Administrador para cada alta/demo.
    /// </summary>
    public interface IDemoEmpresaBootstrap
    {
        /// <param name="codigos">
        /// Si viene con valores, usa esos códigos (filtrando internos).
        /// Si es null/vacío, usa la plantilla del perfil de referencia.
        /// </param>
        /// <returns>Id del perfil Administrador creado para la empresa</returns>
        Task<int> ConfigurarAsync(int idEmpresa, IEnumerable<string>? codigos = null);
    }
}