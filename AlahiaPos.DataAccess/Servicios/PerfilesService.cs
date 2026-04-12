using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class PerfilesService : IPerfiles
    {
        private readonly IRepository<Perfiles> _perfilesRepo;
        private readonly IRepository<PerfilRoles> _perfilRolesRepo;

        public PerfilesService(
            IRepository<Perfiles> perfilesRepo,
            IRepository<PerfilRoles> perfilRolesRepo
        )
        {
            _perfilesRepo = perfilesRepo;
            _perfilRolesRepo = perfilRolesRepo;
        }

        // =====================================================
        // 🔥 CREAR PERFIL + ROLES (AGREGADO COMPLETO)
        // =====================================================
        public async Task<int> CrearPerfilCompleto(PerfilCreateDto dto)
        {
            // 1️⃣ Crear perfil
            var perfil = new Perfiles
            {
                IdEmpresa = dto.IdEmpresa,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                Activo = dto.Activo
            };

            await _perfilesRepo.Save(perfil);

            // 2️⃣ Crear roles asociados
            foreach (var idModulo in dto.Modulos)
            {
                var perfilRol = new PerfilRoles
                {
                    IdEmpresa = dto.IdEmpresa,
                    IdPerfil = perfil.IdPerfil,
                    IdModulo = idModulo,
                    Activo = true
                };

                await _perfilRolesRepo.Save(perfilRol);
            }

            return perfil.IdPerfil;
        }

        // =====================================================
        // 📋 OBTENER PERFILES POR EMPRESA
        // =====================================================
        public async Task<IEnumerable<PerfilWithModulosDto>> ObtenerPorEmpresa(int idEmpresa)
        {
            var perfiles = await _perfilesRepo.GetAllByExpresionAsync(
                p => p.IdEmpresa == idEmpresa 
            );

            var perfilRoles = await _perfilRolesRepo.GetAllByExpresionAsync(
                pr => pr.IdEmpresa == idEmpresa 
            );

            var result = perfiles.Select(p => new PerfilWithModulosDto
            {
                IdPerfil = p.IdPerfil,
                IdEmpresa = p.IdEmpresa,
                Nombre = p.Nombre,
                Descripcion = p.Descripcion,
                Activo = p.Activo,
                Modulos = perfilRoles
                    .Where(r => r.IdPerfil == p.IdPerfil)
                    .Select(r => r.IdModulo)
                    .ToList()
            });

            return result;
        }


        // =====================================================
        // 🔎 OBTENER PERFIL POR ID
        // =====================================================
        public async Task<Perfiles?> ObtenerPorId(int idPerfil)
        {
            return await _perfilesRepo.GetByIdAsync(idPerfil);
        }

        // =====================================================
        // ➕ CREAR PERFIL SIMPLE (opcional)
        // =====================================================
        public async Task<int> Crear(Perfiles perfil)
        {
            await _perfilesRepo.Save(perfil);
            return perfil.IdPerfil;
        }

        // =====================================================
        // ✏️ ACTUALIZAR PERFIL
        // =====================================================
        public async Task<bool> Actualizar(Perfiles perfil)
        {
            _perfilesRepo.Update(perfil.IdPerfil, perfil);
            return true;
        }
        public async Task<bool> ActualizarPerfilCompleto(PerfilUpdateDto dto)
        {
            // 1️⃣ Obtener perfil
            var perfil = await _perfilesRepo.GetByIdAsync(dto.IdPerfil);
            if (perfil == null) return false;

            // 2️⃣ Actualizar datos básicos
            perfil.Nombre = dto.Nombre;
            perfil.Descripcion = dto.Descripcion;
            perfil.Activo = dto.Activo;

            _perfilesRepo.Update(perfil.IdPerfil, perfil);

            // 3️⃣ Eliminar roles actuales (soft o hard)
            var rolesActuales = await _perfilRolesRepo
                .GetAllByExpresionAsync(x => x.IdPerfil == dto.IdPerfil);

            foreach (var rol in rolesActuales)
            {
                _perfilRolesRepo.Delete(rol.IdPerfilRol);
                // o rol.Activo = false; Update(...)
            }

            // 4️⃣ Insertar nuevos roles
            foreach (var idModulo in dto.Modulos)
            {
                var perfilRol = new PerfilRoles
                {
                    IdEmpresa = dto.IdEmpresa,
                    IdPerfil = dto.IdPerfil,
                    IdModulo = idModulo,
                    Activo = true
                };

                await _perfilRolesRepo.Save(perfilRol);
            }

            return true;
        }

        // =====================================================
        // 🗑️ ELIMINAR PERFIL (SOFT DELETE)
        // =====================================================
        public async Task<bool> Eliminar(int idPerfil)
        {
            var perfil = await _perfilesRepo.GetByIdAsync(idPerfil);
            if (perfil == null) return false;

            perfil.Activo = false;
            _perfilesRepo.Update(idPerfil, perfil);
            return true;
        }
    }
}
