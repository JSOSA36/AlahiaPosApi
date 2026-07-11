using AlahiaPos.Entities.Domain;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class FacturaDetalles : BaseEntity
{
    [Key]
    public int IdFacturaDetalle { get; set; }

    public int IdFacturaHeader { get; set; }

    public string? Comentario { get; set; } = "";

    public int IdProducto { get; set; }

    public decimal Dias { get; set; }

    public decimal Cantidad { get; set; }

    public decimal Itbis { get; set; }

    public decimal SubTotal { get; set; }

    public decimal Descuento { get; set; }

    public bool EnviadoCocina { get; set; }

    public bool StatuItem { get; set; }

    public decimal PrecioOferta { get; set; }

    public decimal CantidadDevuelta { get; set; }

    public int? IdEmpleadoComision { get; set; }

    public bool PrintLavador { get; set; }

    // ⭐ FK PRODUCTO
    [ForeignKey(nameof(IdProducto))]
    public virtual Productos? Productos { get; set; }

    // ⭐⭐⭐ ESTA ES LA CLAVE DEL UNIVERSO
    [ForeignKey(nameof(IdFacturaHeader))]
    public virtual FacturaHeaders? FacturaHeader { get; set; }
     
    [NotMapped]
    public string? NombreEmpleadoComision { get; set; }
    // 🔥 PERSONALIZACIÓN DE PRODUCTO (BIZCOCHO)
    public string? TipoMasa { get; set; }
    public string? TipoRelleno { get; set; }
    public decimal Libras { get; set; }
}