using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
     [Table("AreaNegocio")]
        public class AreaNegocio
        {
            [Key]
            public int IdAreaNegocio { get; set; }

            [Required]
            [StringLength(100)]
            public string Nombre { get; set; }

            [StringLength(250)]
            public string Descripcion { get; set; }

            public bool Activo { get; set; } = true;

            public int IdEmpresa { get; set; }

            public DateTime FechaCreacion { get; set; } = DateTime.Now;

            // Navegación
            public virtual ICollection<Area> AreasOperativas { get; set; }
                = new HashSet<Area>();
        }
    }

