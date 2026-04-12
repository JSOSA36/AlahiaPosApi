using AlahiaPos.Entities.Domain;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("EmpleadoAreaComision")]
public class EmpleadoAreaComision : BaseEntity
{
    [Key]
    public int IdEmpleadoAreaComision { get; set; }

    public int? IdEmpleado { get; set; }

    public int? IdArea { get; set; }

    public decimal? PorcientoComision { get; set; }
    public decimal? MontoComision { get; set; }

    // 🔥 NUEVO tipo comisión
    [MaxLength(20)]
    public string? TipoComision { get; set; }



    [ForeignKey(nameof(IdArea))]
    public virtual Area? Area { get; set; }
}
