using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using PrinterLibrary;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class LoginService : ILoginService
    {
        private readonly IRepository<Usuarios> _usuarioRepository;
        private readonly IRepository<Empresas> _empresaRepository;
        private readonly IPerfilRoles _perfilRolesService;

        public LoginService(
            IRepository<Usuarios> usuarioRepository,
            IRepository<Empresas> empresaRepository,
            IPerfilRoles perfilRolesService
        )
        {
            _usuarioRepository = usuarioRepository;
            _empresaRepository = empresaRepository;
            _perfilRolesService = perfilRolesService;
        }

        // =====================================================
        // 🔐 LOGIN
        // =====================================================
        public async Task<LoginResponse?> Login(
        Usuarios usuario,
        string password
    )
        {
            var passwordHash = Utility.EncriptarPassword(password);

            if (usuario.PasswordHash != passwordHash)
                return null;

            var empresa = await _empresaRepository.GetByIdAsync(usuario.IdEmpresa);

            if (empresa == null || !empresa.Estado)
                throw new Exception("Empresa suspendida");

            var modulos = await _perfilRolesService
                .ObtenerModulos(usuario.IdPerfil, usuario.IdEmpresa);

            return new LoginResponse
            {
                Usuario = usuario,
                Token = usuario.Token,
                PuedeEliminarOrden = usuario.PuedeEliminarOrden,
                Modulos = modulos.Select(m => new UsuarioModulo
                {
                    ModuloId = m.Id,
                    Codigo = m.Codigo,
                    Nombre = m.Nombre,
                }).ToList()
            };
        }
        // =====================================================
        // 🔑 GENERAR TOKEN DE RECUPERACIÓN
        // =====================================================
        public async Task<string> GenerarTokenRecuperacion(string correo)
        {
            var usuario = await _usuarioRepository.GetByExpresionAsync(
                u => u.Correo == correo && u.Estado
            );

            if (usuario == null)
                throw new Exception("Correo no encontrado");

            usuario.TokenRecuperacion = Guid.NewGuid().ToString();
            usuario.TokenExpira = DateTime.Now.AddHours(2);

            _usuarioRepository.Update(usuario.IdUsuario, usuario);

            return usuario.TokenRecuperacion;
        }

        // =====================================================
        // 🔎 VALIDAR TOKEN DE RECUPERACIÓN
        // =====================================================
        public async Task<Usuarios?> ValidarTokenRecuperacion(string token)
        {
            return await _usuarioRepository.GetByExpresionAsync(
                u => u.TokenRecuperacion == token &&
                     u.TokenExpira >= DateTime.Now &&
                     u.Estado
            );
        }

        // =====================================================
        // 🔁 RESET PASSWORD CON TOKEN
        // =====================================================
        public async Task<bool> ResetPasswordConToken(
            string token,
            string newPassword)
        {
            var usuario = await ValidarTokenRecuperacion(token);

            if (usuario == null)
                return false;

            usuario.PasswordHash = Utility.EncriptarPassword(newPassword);
            usuario.TokenRecuperacion = null;
            usuario.TokenExpira = null;

            _usuarioRepository.Update(usuario.IdUsuario, usuario);
            return true;
        }

        // =====================================================
        // 🔄 ACTUALIZAR PASSWORD DIRECTO
        // =====================================================
        public async Task ActualizarPassword(
            int idEmpleado,
            string nuevaPassword)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(idEmpleado);

            if (usuario == null)
                throw new Exception("Usuario no encontrado");

            usuario.PasswordHash = Utility.EncriptarPassword(nuevaPassword);
            _usuarioRepository.Update(usuario.IdUsuario, usuario);
        }
    }
}
