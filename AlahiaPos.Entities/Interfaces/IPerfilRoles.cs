using AlahiaPos.Entities.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IPerfilRoles
    {
        // 🔹 Módulos visibles para un perfil
        Task<IEnumerable<PerfilRoles>> ObtenerPorPerfil(int idPerfil, int idEmpresa);

        // 🔹 Asignar módulos a un perfil (reemplaza todos)
        Task<bool> AsignarModulos(int idPerfil, int idEmpresa, IEnumerable<int> idsModulos);
        Task<IEnumerable<Modulo>> ObtenerModulos(int idPerfil, int idEmpresa);
        // 🔹 Quitar todos los módulos de un perfil
        Task Limpiar(int idPerfil, int idEmpresa);
    }
}
