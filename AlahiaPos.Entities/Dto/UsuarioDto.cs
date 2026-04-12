using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class UsuarioDto
    {
        public int IdEmpleados { get; set; }   // 👈 Para edición (0 si es nuevo)
        public bool? PuedeEliminarOrden { get; set; } // 👈 NUEVO
        public int IdEmpresa { get; set; }     // Empresa a la que pertenece el usuario
        public string? Dispositivo { get; set; }
        public string Nombre { get; set; } = string.Empty;   // Nombre del empleado
        public string UserName { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;   // Correo (también será UserName)

        public string Password { get; set; } = string.Empty; // Contraseña (puede venir en blanco en edición)

        public string Rol { get; set; } = "User";            // Rol: Admin, User, Manager

        public bool Activo { get; set; } = true;             // Estado activo/inactivo

        // 👇 Opcionales por si después los quieres usar
        public string? Celular { get; set; }                  // Identificación
        public string? Direccion { get; set; }
        public string? TokenNotificacion { get; set; }
        public int maxUsuarios { get; set; }

    }
}
