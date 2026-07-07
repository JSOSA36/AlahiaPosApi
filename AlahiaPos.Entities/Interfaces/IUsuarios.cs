using AlahiaPos.Entities.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IUsuarios
    {
        // ============================
        // 🔹 CRUD BÁSICO
        // ============================
        Task<Usuarios?> GetByUserName(string userName);
        Task<IEnumerable<Usuarios>> ObtenerPorEmpresa(int idEmpresa);
        Task<Usuarios?> ObtenerPorId(int idUsuario);
        Task<int> CountByEmpresa(int idEmpresa);
        

        Task<int> Crear(Usuarios usuario);
        Task<bool> Actualizar(Usuarios usuario);
        Task<bool> Eliminar(int idUsuario); // Soft delete recomendado

        // ============================
        // 🔹 VALIDACIONES
        // ============================
        Task<bool> ExisteUserName(string userName);
        Task<bool> ExisteCorreo(string correo);

        // ============================
        // 🔹 PERFIL
        // ============================
        Task<bool> AsignarPerfil(int idUsuario, int idPerfil);

        // ============================
        // 🔥 CLAVE DEL SISTEMA
        // ============================
        // Módulos visibles según:
        // Usuario → Perfil → PerfilRoles → Empresa_Modulos
        Task<IEnumerable<Modulo>> ObtenerModulosVisibles(int idUsuario);
        
    }
}
