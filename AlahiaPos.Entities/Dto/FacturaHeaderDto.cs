using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class FacturaHeaderDto
    {
        public int IdFacturaHeader { get; set; }
        public int IdEmpresa { get; set; }
        public string? Plazo { get; set; } = "";
        public string? TipoFactura { get; set; } = "";
        public string? NombreCuenta { get; set; } = "";
        public decimal MontoPropina { get; set; }
        public string? Moneda { get; set; }
        public int? IdEmpleados { get; set; }
        public DateTime FechaInseccion { get; set; }
        public string? TipoOrden { get; set; } = "";
        public int? IdMoso { get; set; }
        public int IdMesa { get; set; }
        public int? IdTipoDocumentos { get; set; }
        public string? NCF { get; set; } = "";
        public string? FormaPago { get; set; } = "";

        public int? IDCliente { get; set; }
        public decimal Efectivo { get; set; }
        public decimal SubTotal { get; set; }
        public decimal MontoTarjeta { get; set; }
        public decimal Cambio { get; set; }
        public decimal Total { get; set; }
        public decimal TotalItbis { get; set; }
        public decimal TotalDescuento { get; set; }
       
        public bool EstaCancelada { get; set; }
        public bool EstaCerrada { get; set; }
        public string? Nota { get; set; } = "";
        public DateTime FechaBencimiento { get; set; }
        public string? Estado { get; set; } = "";
     
        public decimal Pagado { get; set; }
        public decimal Pendiente { get; set; }
        public string? Hora { get; set; } = "";
        public bool AjustadoInventario { get; set; }
        public virtual List<FacturaDetallesDto>? FacturaDetalles { get; set; }
        public string? Estado_Orden { get; set; }
        public virtual Mesas? Mesas { get; set; }
        public virtual ClienteDto? Clientes { get; set; }
       
    }
}
