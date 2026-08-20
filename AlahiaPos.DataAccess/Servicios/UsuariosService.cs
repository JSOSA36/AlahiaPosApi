using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class UsuariosService : IUsuarios
    {
        private readonly IRepository<Usuarios> _usuarioRepo;
        private readonly IRepository<PerfilRoles> _perfilRolRepo;

        public UsuariosService(
            IRepository<Usuarios> usuarioRepo,
            IRepository<PerfilRoles> perfilRolRepo
        )
        {
            _usuarioRepo = usuarioRepo;
            _perfilRolRepo = perfilRolRepo;
        }

        // ============================
        // 🔹 LECTURA
        // ============================
        public async Task<Usuarios?> GetByUserName(string userName)
        {
            return await _usuarioRepo
                .GetByExpresionAsync(u => u.UserName == userName);
        }
        public async Task<IEnumerable<Usuarios>> ObtenerPorEmpresa(int empresaId)
        {
            return await _usuarioRepo.GetAllByExpresionAsync(
                u => u.IdEmpresa == empresaId && u.Estado,
                "Empleado",
                "Perfil"
            );
        }


        public async Task<Usuarios?> ObtenerPorId(int idUsuario)
        {
            return (
                await _usuarioRepo.GetAllByExpresionAsync(
                    u => u.IdUsuario == idUsuario,
                    "Empleado",
                    "Perfil"
                )
            ).FirstOrDefault();
        }

        public async Task<Usuarios?> ObtenerPorUserName(string userName)
        {
            return (
                await _usuarioRepo.GetAllByExpresionAsync(
                    u => u.UserName == userName && u.Estado,
                    "Empleado",
                    "Perfil"
                )
            ).FirstOrDefault();
        }

        // ============================
        // 🔹 ESCRITURA
        // ============================

        public async Task<int> Crear(Usuarios usuario)
        {
            usuario.FechaCreacion = System.DateTime.Now;
            usuario.Estado = true;

            await _usuarioRepo.Save(usuario);
            return usuario.IdUsuario;
        }
        public async Task<int> CountByEmpresa(int idEmpresa)
        {
            var usuarios = await _usuarioRepo
                .GetAllByExpresionAsync(u => u.IdEmpresa == idEmpresa);

            return usuarios.Count();
        }


        public async Task<bool> Actualizar(Usuarios usuario)
        {
            if (usuario == null || usuario.IdUsuario <= 0)
                return false;

            usuario.Perfil = null;
            usuario.Empleado = null;
            usuario.Empresa = null;

            _usuarioRepo.Update(usuario.IdUsuario, usuario);
            return true;
        }

        public async Task<bool> Eliminar(int idUsuario)
        {
            var usuario = await ObtenerPorId(idUsuario);
            if (usuario == null)
                return false;

            usuario.Estado = false;
            _usuarioRepo.Update(usuario.IdUsuario, usuario);

            return true;
        }

        // ============================
        // 🔹 PERFIL
        // ============================

        public async Task<bool> AsignarPerfil(int idUsuario, int idPerfil)
        {
            var usuario = await ObtenerPorId(idUsuario);
            if (usuario == null)
                return false;

            usuario.Perfil = null;
            usuario.IdPerfil = idPerfil;
            _usuarioRepo.Update(usuario.IdUsuario, usuario);
            return true;
        }

        // ============================
        // 🔥 MÓDULOS VISIBLES (MENÚ)
        // ============================

        public async Task<IEnumerable<Modulo>> ObtenerModulosVisibles(int idUsuario)
        {
            var usuario = await ObtenerPorId(idUsuario);
            if (usuario == null || usuario.IdPerfil == 0)
                return Enumerable.Empty<Modulo>();

            var modulos = await _perfilRolRepo.GetAllByExpresionAsync(
                p => p.IdPerfil == usuario.IdPerfil
                  && p.IdEmpresa == usuario.IdEmpresa
                  && p.Activo,
                "Modulos"
            );

            return modulos
                .Select(m => m.Modulos)
                .Where(m => m != null && m.Activo)
                .Distinct()
                .ToList();
        }

        // ============================
        // 🔹 UTILIDADES
        // ============================

        public async Task<bool> ExisteUserName(string userName)
        {
            var key = (userName ?? string.Empty).Trim().ToLower();
            if (string.IsNullOrEmpty(key)) return false;

            return await _usuarioRepo.GetAny(
                u => u.UserName != null && u.UserName.ToLower() == key
            );
        }

        public async Task<bool> ExisteCorreo(string correo)
        {
            var key = (correo ?? string.Empty).Trim().ToLower();
            if (string.IsNullOrEmpty(key)) return false;

            return await _usuarioRepo.GetAny(
                u => u.Correo != null && u.Correo.ToLower() == key
            );
        }
    }
}
