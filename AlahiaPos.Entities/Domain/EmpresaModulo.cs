using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class EmpresaModulo
    {
        [Key]
        public int Id { get; set; }

        // 🔗 FK Empresa
        [Required]
        public int EmpresaId { get; set; }

       
        public Empresas Empresa { get; set; }

        // 🔗 FK Módulo
        [Required]
        public int ModuloId { get; set; }

       
        public Modulo Modulo { get; set; }

        // ⚙️ Estado del módulo para la empresa
        public bool Activo { get; set; } = true;

        // 📅 Fecha de activación
        public DateTime FechaActivacion { get; set; } = DateTime.Now;

        // 📅 Fecha opcional de desactivación
        public DateTime? FechaDesactivacion { get; set; }

        
        

    }
}
