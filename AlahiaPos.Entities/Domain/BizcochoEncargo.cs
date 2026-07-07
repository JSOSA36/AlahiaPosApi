using AlahiaPos.Entities.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class BizcochoEncargo:BaseEntity
    {
        [Key]
        public int IdBizcochoEncargo { get; set; }

        public string TipoRelleno { get; set; }
       
        // === Cliente ===
        public string Cliente { get; set; }
        public string Celular { get; set; }
        public string FormaPago { get; set; }

        // === Pedido ===
        public string TipoMasa { get; set; }
        public decimal Libras { get; set; }

        // 🔥 PRECIO GENERAL DEFINIDO POR EL USUARIO
        public decimal PrecioUnitario { get; set; }

        // 🔥 TOTAL YA NO SE MULTIPLICA POR LIBRAS
        public decimal Total => PrecioUnitario;

        public decimal Abono { get; set; }
        public decimal Pendiente => Total - Abono;

        // Entrega
        public DateTime FechaEntrega { get; set; }
        public DateTime HoraEntrega { get; set; }
        public string Nota { get; set; }

        // Control
        public string Estado { get; set; } = "Pendiente";
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // Helper
        public bool EstaPagado => Pendiente <= 0;

        // === Datos fiscales ===
        public string NCF { get; set; } = "";
        public string RNC { get; set; } = "";
        public string NombreEmpresa { get; set; } = "";
    }
}
