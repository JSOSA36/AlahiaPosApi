using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("AlmacenExistencias")]
    public class AlmacenExistencia
    {
        [Key]
        public int IdAlmacenExistencia { get; set; }

        public int IdAlmacen { get; set; }

        public int IdProducto { get; set; }

        public int IdEmpresa { get; set; }

        [Column("Existencia", TypeName = "decimal(18,2)")]
        public decimal Cantidad { get; set; }

        [ForeignKey(nameof(IdAlmacen))]
        public virtual Almacen? Almacen { get; set; }

        [ForeignKey(nameof(IdProducto))]
        public virtual Productos? Producto { get; set; }
    }
}
