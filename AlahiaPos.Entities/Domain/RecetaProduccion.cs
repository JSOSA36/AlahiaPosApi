using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("RecetaProduccion")]
    public class RecetaProduccion
    {
        [Key]
        public int IdReceta { get; set; }
        public int IdEmpresa { get; set; }
        public int IdProductoTerminado { get; set; }

        [Required, MaxLength(160)]
        public string Nombre { get; set; } = "";

        public decimal RendimientoBase { get; set; } = 1;
        public int? IdUnidadMedida { get; set; }
        public bool Activa { get; set; } = true;

        [MaxLength(500)]
        public string? Observacion { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? IdUsuario { get; set; }
    }

    [Table("RecetaProduccionItem")]
    public class RecetaProduccionItem
    {
        [Key]
        public int IdRecetaItem { get; set; }
        public int IdReceta { get; set; }
        public int IdProducto { get; set; }
        public decimal Cantidad { get; set; }
        public int? IdUnidadMedida { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; } = true;
    }

    [Table("OrdenProduccion")]
    public class OrdenProduccion
    {
        [Key]
        public int IdOrdenProduccion { get; set; }
        public int IdEmpresa { get; set; }

        [Required, MaxLength(40)]
        public string Numero { get; set; } = "";

        public int IdReceta { get; set; }
        public int IdProductoTerminado { get; set; }
        public decimal CantidadPlanificada { get; set; }
        public decimal? CantidadReal { get; set; }
        public int IdAlmacenOrigen { get; set; }
        public int IdAlmacenDestino { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public int? IdUsuarioResponsable { get; set; }

        [MaxLength(500)]
        public string? Observacion { get; set; }

        [Required, MaxLength(20)]
        public string Estado { get; set; } = "BORRADOR";

        public int? IdMovimientoSalida { get; set; }
        public int? IdMovimientoEntrada { get; set; }
        public decimal CostoMateriales { get; set; }
        public decimal CostoUnitario { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaCompletado { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? IdUsuario { get; set; }
    }

    [Table("OrdenProduccionMaterial")]
    public class OrdenProduccionMaterial
    {
        [Key]
        public int IdOrdenMaterial { get; set; }
        public int IdOrdenProduccion { get; set; }
        public int IdProducto { get; set; }
        public int? IdUnidadMedida { get; set; }
        public decimal CantidadTeorica { get; set; }
        public decimal? CantidadReal { get; set; }
        public decimal Disponible { get; set; }
        public decimal Faltante { get; set; }
        public decimal PrecioCompra { get; set; }
        public decimal CostoLinea { get; set; }
    }
}
