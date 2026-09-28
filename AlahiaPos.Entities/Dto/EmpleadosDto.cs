using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class EmpleadosDto
    {
        [Required]
        public int IdEmpleados { get; set; }

        [Required]
        public string Nombre { get; set; }

        // Ocupación / Rol (Peluquera, Cajero, Recepcionista, etc.)
        

        public string? Direccion { get; set; }

        public string? Celular { get; set; }

        public bool Estado { get; set; }
        public string Ocupacion { get; set; }

        [Required]
        public int IdEmpresa { get; set; }

        /// <summary>
        /// Sucursal operativa. Obligatorio si la ocupación no es Administrador.
        /// </summary>
        public int? IdSucursal { get; set; }

    }
}
