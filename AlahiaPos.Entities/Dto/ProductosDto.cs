using AlahiaPos.Entities.Domain;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class ProductosDto:BaseEntity
    {
        public int idProducto { get; set; }
        public int? idcategoria { get; set; }
        public int? IdArea { get; set; }
        public bool? EsServicio { get; set; }
        public string? nombre { get; set; } = "";
        public decimal? cantidad { get; set; }
        public decimal? stockminimo { get; set; }
        public decimal? precio { get; set; }
        public decimal? costo { get; set; }
        public string? TipoOperacion { get; set; }
        public bool? isproductobelleza { get; set; }
        public bool ControlarStock { get; set; }
        public IFormFile? Imagen { get; set; }  // Cambiado a IFormFile
        public bool? isActiva { get; set; }
        public int? DuracionServicio { get; set; } = 60; // minutos por defecto
        public bool? DisponibleEnCitas { get; set; } = true;
        public bool ManejaGuarniciones { get; set; }
        public string? CodigoBarra { get; set; }
        public bool? Itbis { get; set; }
        public string? TipoComportamiento { get; set; }
    }
}
