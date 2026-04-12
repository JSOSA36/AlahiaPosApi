using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface ILoginService
    {
        Task<LoginResponse> Login(
            string userName,
            string password,
            string deviceId
        );
        Task<string> GenerarTokenRecuperacion(string correo);
        Task<Usuarios?> ValidarTokenRecuperacion(string token);
        Task<bool> ResetPasswordConToken(string token, string newPassword);
        Task ActualizarPassword(int idEmpleado, string nuevaPassword);
    }
}
