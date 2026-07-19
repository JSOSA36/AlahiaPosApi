using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("ActivosFijos")]
    public class ActivoFijo
    {
        [Key]
        public int IdActivoFijo { get; set; }

        public int IdEmpresa { get; set; }

        public int? IdProducto { get; set; }

        /// <summary>FACTC origen (documento comercial).</summary>
        public int? IdOrdenCompraHeader { get; set; }

        /// <summary>Línea de compra origen.</summary>
        public int? IdOrdenCompraDetalle { get; set; }

        [Required, StringLength(40)]
        public string CodigoActivo { get; set; } = "";

        [Required, StringLength(250)]
        public string Descripcion { get; set; } = "";

        [StringLength(100)]
        public string? Marca { get; set; }

        [StringLength(100)]
        public string? Modelo { get; set; }

        [StringLength(100)]
        public string? NumeroSerie { get; set; }

        public DateTime FechaAdquisicion { get; set; }

        /// <summary>Momento de recepción física (alta del activo).</summary>
        public DateTime? FechaRecepcion { get; set; }

        public decimal ValorAdquisicion { get; set; }

        public decimal ValorResidual { get; set; }

        public int? VidaUtilMeses { get; set; }

        /// <summary>PENDIENTE_DATOS | ACTIVO | BAJA | EN_MANTENIMIENTO</summary>
        [StringLength(30)]
        public string Estado { get; set; } = EstadoActivoFijoConstantes.PendienteDatos;

        public int? IdCuentaContable { get; set; }

        /// <summary>Almacén usado como punto de recepción (no existencias AF).</summary>
        public int? IdAlmacenRecepcion { get; set; }

        [StringLength(200)]
        public string? Ubicacion { get; set; }

        [StringLength(150)]
        public string? Responsable { get; set; }

        [StringLength(500)]
        public string? Observacion { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public int? IdUsuarioCreacion { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime? FechaInseccion { get; set; }
    }

    public static class EstadoActivoFijoConstantes
    {
        public const string PendienteDatos = "PENDIENTE_DATOS";
        public const string Activo = "ACTIVO";
        public const string Baja = "BAJA";
        public const string EnMantenimiento = "EN_MANTENIMIENTO";
    }
}
