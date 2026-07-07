using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class Categorias:BaseEntity
    {
        [Key]
        public int IdCategoria { get; set; }
        public string? ImagenPath { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public string? Tipo { get; set; }
        public bool IsActiva { get; set; }
        public int Prioridad { get; set; }
        // =========================================
        // 🔥 MODELO C#
        // =========================================

        public string? TipoOperacion { get; set; }
    }
}
