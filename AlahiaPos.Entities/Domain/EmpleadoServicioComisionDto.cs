using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class EmpleadoServicioComisionDto
    {
        public int Id { get; set; }  
        public int IdEmpresa { get; set; }// Identificador de la comisión
        public int IdEmpleado { get; set; }       // Empleado asignado
        public int IdProducto { get; set; }       // Servicio (Producto con EsServicio = true)
        public decimal PorcientoComision { get; set; } // % de comisión
        public virtual string? Nombre { get; set; }
    }
}
