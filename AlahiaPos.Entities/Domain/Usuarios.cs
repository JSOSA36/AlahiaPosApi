using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    public class Usuarios 
    {
        [Key]
        public int IdUsuario { get; set; }
        public bool PuedeEliminarOrden { get; set; } // 👈 NUEVO
        // ============================
        // 🔗 EMPRESA (MULTI-TENANT)
        // ============================
        public int IdEmpresa { get; set; }

        /// <summary>Sucursal activa de la sesión (un token = un usuario).</summary>
        public int? IdSucursalActiva { get; set; }

        [ForeignKey(nameof(IdEmpresa))]
        public Empresas Empresa { get; set; }

        // ============================
        // 🔗 EMPLEADO (PERSONA REAL)
        // ============================
        public int IdEmpleado { get; set; }

        [ForeignKey(nameof(IdEmpleado))]
        public Empleados Empleado { get; set; }

        // ============================
        // 🔐 PERFIL (DEFINE MÓDULOS)
        // ============================
        public int IdPerfil { get; set; }

        [ForeignKey(nameof(IdPerfil))]
        public Perfiles Perfil { get; set; }

        // ============================
        // 🔐 CREDENCIALES
        // ============================
        [Required]
        [MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? Correo { get; set; }

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public bool PuedeEliminarItemCarrito { get; set; }

        public bool PuedeDisminuirCantidadCarrito { get; set; }

        public bool PuedeEditarPrecioCarrito { get; set; }

        public bool PuedeAnularFactura { get; set; }

        // ============================
        // 🔒 SEGURIDAD / LICENCIA
        // ============================
        [MaxLength(150)]
        public string? Dispositivo { get; set; }

        [MaxLength(500)]
        public string? Token { get; set; } // Token de sesión si lo usas

        // 🔑 Recuperación de contraseña
        [MaxLength(150)]
        public string? TokenRecuperacion { get; set; }

        public DateTime? TokenExpira { get; set; }

        // ============================
        // 📊 ESTADO
        // ============================
        public bool Estado { get; set; } = true;

        // ============================
        // 🕒 AUDITORÍA
        // ============================
        public DateTime? FechaCreacion { get; set; } 

        public DateTime? UltimoAcceso { get; set; }
    }
}
