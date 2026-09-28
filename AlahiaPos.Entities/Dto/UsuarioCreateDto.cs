using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class UsuarioCreateDto
    {
        [Required]
        public int IdEmpresa { get; set; }

        [Required]
        public int IdEmpleado { get; set; }

        [Required]
        public int IdPerfil { get; set; }

        /// <summary>
        /// Sucursal operativa. Obligatorio si el perfil no es Administrador.
        /// El administrador no se asigna a una sucursal.
        /// </summary>
        public int? IdSucursal { get; set; }

        [Required]
        public string Correo { get; set; }

  
        public string? Password { get; set; }

        public bool Activo { get; set; } = true;

        public bool PuedeEliminarOrden { get; set; }

        public bool PuedeEliminarItemCarrito { get; set; }

        public bool PuedeDisminuirCantidadCarrito { get; set; }

        public bool PuedeEditarPrecioCarrito { get; set; }

        public bool PuedeAnularFactura { get; set; }
    }

}
