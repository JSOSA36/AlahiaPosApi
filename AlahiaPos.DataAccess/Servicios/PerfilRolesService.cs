using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class PerfilRolesService : IPerfilRoles
    {
        private readonly IRepository<PerfilRoles> _repository;

        public PerfilRolesService(IRepository<PerfilRoles> repository)
        {
            _repository = repository;
        }

        // =====================================================
        // 🔹 Obtener relaciones perfil → módulos
        // =====================================================
        public async Task<IEnumerable<PerfilRoles>> ObtenerPorPerfil(int idPerfil, int idEmpresa)
        {
            return await _repository.GetAllByExpresionAsync(
                r => r.IdPerfil == idPerfil
                  && r.IdEmpresa == idEmpresa   // 👈🔥 CLAVE
                  && r.Activo,
                "Modulo"
            );
        }

        // =====================================================
        // 🔹 Obtener SOLO los módulos visibles del perfil
        // =====================================================
        public async Task<IEnumerable<Modulo>> ObtenerModulos(int idPerfil, int idEmpresa)
        {
            var relaciones = await _repository.GetAllByExpresionAsync(
                r => r.IdPerfil == idPerfil
                  && r.IdEmpresa == idEmpresa   // 👈🔥 CLAVE
                  && r.Activo,
                "Modulos"
            );

            return relaciones
        .Select(r => r.Modulos)        // ✅ singular
        .Where(m => m != null)
        .GroupBy(m => m.Id)
        .Select(g => g.First())
        .ToList();
        }

        // =====================================================
        // 🔹 Limpiar módulos del perfil (soft delete)
        // =====================================================
        public async Task Limpiar(int idPerfil, int idEmpresa)
        {
            var existentes = await _repository.GetAllByExpresionAsync(
                r => r.IdPerfil == idPerfil
                  && r.IdEmpresa == idEmpresa   // 👈🔥 CLAVE
            );

            foreach (var item in existentes)
            {
                item.Activo = false;
                 _repository.Update(item.IdPerfilRol,item);
            }
        }

        // =====================================================
        // 🔹 Asignar módulos al perfil
        // =====================================================
        public async Task<bool> AsignarModulos(int idPerfil, int idEmpresa, IEnumerable<int> idsModulos)
        {
            await Limpiar(idPerfil, idEmpresa);   // 👈🔥 SOLO esa empresa

            foreach (var idModulo in idsModulos.Distinct())
            {
                var perfilRol = new PerfilRoles
                {
                    IdPerfil = idPerfil,
                    IdEmpresa = idEmpresa,   // 👈🔥 CLAVE
                    IdModulo = idModulo,
                    Activo = true
                };

                await _repository.Save(perfilRol);
            }

            return true;
        }
    }
}
