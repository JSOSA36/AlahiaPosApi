using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("CategoriasGasto")]
    public class CategoriaGasto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdCategoriaGasto { get; set; }

        public int IdEmpresa { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Descripcion { get; set; }

        public bool Activo { get; set; } = true;

        public int Orden { get; set; }

        /// <summary>
        /// Cuenta de gasto específica. Si null, el asiento usa el mapeo GASTO_OPERATIVO.
        /// </summary>
        public int? IdCuentaContable { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
