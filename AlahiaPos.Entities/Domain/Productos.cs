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

        public int? IdUnidadMedida { get; set; }

        public int? IdCategoria { get; set; }

        public int? IdAlmacen { get; set; }

        [Column(TypeName = "VARCHAR")]
        [StringLength(30)]
       
        public string? CodigoBarra { get; set; } = "";
        public string? TipoProducto { get; set; } = "";

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
        public string? Nota { get; set; } = "";

        [NotMapped]
        public virtual UnidadMedidas UnidadMedidas { get; set; } = new UnidadMedidas();
        [NotMapped]
        public virtual Categorias Categorias { get; set; }=new Categorias();
        [NotMapped]
        public virtual Almacen Almacen { get; set; } = new Almacen();
        [NotMapped]
        public virtual Proveedores Proveedores { get; set; }=new Proveedores();
        
        public bool SeCompra { get; set; }
        public bool SeAlquila { get; set; }
        public bool SeVende { get; set; }
        public bool ControlarStock { get; set; }
        public bool IsActivo { get; set; }
       
        public decimal Ganancia { get; set; }
        public int IdCocina { get; set; }
        public bool? EsProductoBelleza { get; set; }
        public int DuracionServicio { get; set; } = 60; // minutos por defecto
        public bool DisponibleEnCitas { get; set; } = true;

    }
}
