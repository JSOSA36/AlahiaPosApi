using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("EmpleadosP")]
    public class Empleados 
    {
        [Key]
        public int IdEmpleados { get; set; }

        public int IdEmpresa { get; set; }
        [ForeignKey(nameof(IdEmpresa))]
        public Empresas Empresa { get; set; }
        public string? Cedula { get; set; }
        public string? Nombre { get; set; }
        public string? Direccion { get; set; } 
        public string? Telefono { get; set; } 
        public string? Celular { get; set; } 

        public decimal ComisionServicio { get; set; }
        public decimal ComisionProductos { get; set; }

        public string? Nota { get; set; } = "";

        public bool Estado { get; set; } = true;
        public string? Ocupacion { get; set; }
        // 🔗 Navegación opcional
       

        // ❌ SIN LOGIN
        // ❌ SIN ROLES
        // ❌ SIN PASSWORD
        // ❌ SIN TOKEN
        // ❌ SIN DISPOSITIVO
    }
}
