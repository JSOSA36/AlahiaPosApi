using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class PerfilRolesService : IPerfilRoles
    {
        private readonly IRepository<PerfilRoles> _repository;
        private readonly IRepository<Perfiles> _perfiles;
        private readonly IEmpresaModulos _empresaModulos;

        public PerfilRolesService(
            IRepository<PerfilRoles> repository,
            IRepository<Perfiles> perfiles,
            IEmpresaModulos empresaModulos)
        {
            _repository = repository;
            _perfiles = perfiles;
            _empresaModulos = empresaModulos;
        }

        public async Task<IEnumerable<PerfilRoles>> ObtenerPorPerfil(int idPerfil, int idEmpresa)
        {
            return await _repository.GetAllByExpresionAsync(
                r => r.IdPerfil == idPerfil
                  && r.IdEmpresa == idEmpresa
                  && r.Activo,
                "Modulos"
            );
        }

        /// <summary>
        /// Menú del usuario = módulos ACTIVOS del perfil, de ESA empresa,
        /// y contratados en Empresa_Modulos. Nunca el catálogo completo ni
        /// los módulos de otro perfil.
        /// </summary>
        public async Task<IEnumerable<Modulo>> ObtenerModulos(int idPerfil, int idEmpresa)
        {
            if (idPerfil <= 0 || idEmpresa <= 0)
                return Array.Empty<Modulo>();

            var perfil = await _perfiles.GetByIdAsync(idPerfil);
            if (perfil == null || !perfil.Activo || perfil.IdEmpresa != idEmpresa)
                return Array.Empty<Modulo>();

            var relaciones = await _repository.GetAllByExpresionAsync(
                r => r.IdPerfil == idPerfil
                  && r.IdEmpresa == idEmpresa
                  && r.Activo,
                "Modulos"
            );

            var licencia = await _empresaModulos.GetModulosByEmpresa(idEmpresa);
            var idsLicencia = licencia
                .Where(x => x.Activo)
                .Select(x => x.ModuloId)
                .ToHashSet();

            return relaciones
                .Select(r => r.Modulos)
                .Where(m => m != null && m.Activo && idsLicencia.Contains(m.Id))
                .GroupBy(m => m.Id)
                .Select(g => g.First())
                .ToList();
        }

        public async Task Limpiar(int idPerfil, int idEmpresa)
        {
            var existentes = await _repository.GetAllByExpresionAsync(
                r => r.IdPerfil == idPerfil
                  && r.IdEmpresa == idEmpresa
            );

            foreach (var item in existentes)
            {
                item.Activo = false;
                _repository.Update(item.IdPerfilRol, item);
            }
        }

        public async Task<bool> AsignarModulos(int idPerfil, int idEmpresa, IEnumerable<int> idsModulos)
        {
            await Limpiar(idPerfil, idEmpresa);

            foreach (var idModulo in idsModulos.Distinct())
            {
                var perfilRol = new PerfilRoles
                {
                    IdPerfil = idPerfil,
                    IdEmpresa = idEmpresa,
                    IdModulo = idModulo,
                    Activo = true
                };

                await _repository.Save(perfilRol);
            }

            return true;
        }
    }
}
