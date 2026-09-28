using AlahiaPos.Entities.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace AlahiaPos.Entities.Domain
{
    public class Productos: BaseEntity
    {
        [Key]
        public int IdProducto { get; set; }
        public bool EsServicio { get; set; }
        public int? IdArea { get; set; }
        public string? Nombre { get; set; } = "";
        public string? Descripcion { get; set; } = "";
        public decimal? Rentado { get; set; }
        public decimal? Disponibles { get; set; }
        public int IdProveedor { get; set; }
        public decimal Cantidad { get; set; }
        public decimal Stock { get; set; }
        public decimal PrecioVenta { get; set; }
        public virtual ICollection<MovimientosInventarioDetalle>
        MovimientosInventarioDetalle
        { get; set; }
        = new List<MovimientosInventarioDetalle>();
        public int? IdUnidadMedida { get; set; }
        public string? TipoOperacion { get; set; }
        public int? IdCategoria { get; set; }

        public int? IdAlmacen { get; set; }

        [Column(TypeName = "VARCHAR")]
        [StringLength(30)]
       
        public string? CodigoBarra { get; set; } = "";
        public string? TipoProducto { get; set; } = "";

        /// <summary>
        /// Comportamiento ERP: Inventario | Servicio | Gasto | ActivoFijo
        /// </summary>
        public string? TipoComportamiento { get; set; } = TipoComportamientoConstantes.Inventario;

        public decimal? Descuento { get; set; }

        public decimal Precio1 { get; set; }
        public decimal Precio2 { get; set; }
        public decimal Precio3 { get; set; }
        public decimal PrecioDolar { get; set; }
        public int? IdRecetasHeader { get; set; }
        public decimal PorcientoDescuento { get; set; }
        public decimal PorcientoGanancia { get; set; }
        public decimal PrecioCompra { get; set; }
        public string? FechaVencimiento { get; set; } = "";
        public string? Imagen1 { get; set; } = "";
        public string? Imagen2 { get; set; } = "";
        public string? Imagen3 { get; set; } = "";
        public bool Itbis { get; set; }

        /// <summary>
        /// Default de captura para operaciones NUEVAS únicamente.
        /// Nunca fuente oficial ni para recalcular histórico (foto en documento).
        /// Null = usar ParametrosConfigs.ImpuestoItbis.
        /// </summary>
        public decimal? TasaItbis { get; set; }
        /// <summary>Default sugerido al facturar; no recalcula histórico.</summary>
        public byte? TipoIngresoDgiiDefault { get; set; }
        public string? CodigoExencionDgii { get; set; }

        public string? Nota { get; set; } = "";

        // No instanciar por defecto: el POS puede enviar Productos vacío en el detalle
        // y Almacen.Nombre [Required] generaba 400 Bad Request.
        [NotMapped]
        public virtual UnidadMedidas? UnidadMedidas { get; set; }
        [NotMapped]
        public virtual Categorias? Categorias { get; set; }
        [NotMapped]
        public virtual Almacen? Almacen { get; set; }
        [NotMapped]
        public virtual Proveedores? Proveedores { get; set; }
        
        public bool SeCompra { get; set; }
        public bool SeAlquila { get; set; }
        public bool SeVende { get; set; }
        public bool ControlarStock { get; set; }
        public bool IsActivo { get; set; }
       
        public decimal Ganancia { get; set; }
        public int? IdCocina { get; set; }
        public bool? EsProductoBelleza { get; set; }
        public int DuracionServicio { get; set; } = 60; // minutos por defecto
        public bool DisponibleEnCitas { get; set; } = true;
        /// <summary>Si es true, el POS pide guarnición al agregar el ítem.</summary>
        public bool ManejaGuarniciones { get; set; }

    }
}
