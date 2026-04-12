using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IPerfiles
    {
        // =====================================================
        // 🔥 AGREGADO COMPLETO (Perfil + Roles)
        // =====================================================
        Task<int> CrearPerfilCompleto(PerfilCreateDto dto);
        Task<bool> ActualizarPerfilCompleto(PerfilUpdateDto dto);

        // =====================================================
        // 📋 CONSULTAS
        // =====================================================
        Task<IEnumerable<PerfilWithModulosDto>> ObtenerPorEmpresa(int idEmpresa);
        Task<Perfiles?> ObtenerPorId(int idPerfil);

        // =====================================================
        // 🧱 CRUD BÁSICO (Perfil SOLO)
        // =====================================================
        Task<int> Crear(Perfiles perfil);
        Task<bool> Actualizar(Perfiles perfil);
        Task<bool> Eliminar(int idPerfil);
    }
}
