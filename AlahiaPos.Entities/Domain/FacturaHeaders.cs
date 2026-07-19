using AlahiaPos.Entities.Interfaces;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class FacturaHeaders:BaseEntity
    {
        [Key]
        public int IdFacturaHeader { get; set; }
        public string? RNC { get; set; }
        public int? IdCajaCierre
        {
            get;
            set;
        }
       
        public int? IdUsuario { get; set; }
        public string? NombreEmpresa { get; set; }
        public bool? PrintLavador { get; set; }
        public string? NumeroDocumento { get; set; } = "";
         public string? Moneda { get; set; }
        public string? Plazo { get; set; } = "";
        public string? TipoFactura { get; set; } = "";
        public decimal MontoPropina { get; set; }
        public bool PrintPending { get; set; }
        public bool? PrintAcount { get; set; }
        public int? IdEmpleados { get; set; }
        public int? IdEmpleadoComision { get; set; }
        public string? Hora { get; set; } = "";
        public int? IdMoso { get; set; }
        public int? IdMesa { get; set; }
        public int? IdTipoDocumentos { get; set; }
        public string? NCF { get; set; } = "";
        public string? FormaPago { get; set; } = "";
        public int? IDCliente { get; set; }
        
        public string? MotivoAnulacion { get; set; }
        public decimal Efectivo { get; set; }

        public decimal SubTotal { get; set; }
        public decimal MontoTarjetaVisa { get; set; }
        public decimal MontoTarjetaMasterCard { get; set; }
        public decimal MontoTransferencia { get; set; }
        public decimal MontoCheques { get; set; }
        public decimal MontoEfectivo { get; set; }
        public decimal MontoNotaCredito { get; set; }
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
        [ForeignKey(nameof(IDCliente))]
        //[NotMapped]
        public Clientes? Clientes { get; set; } = new Clientes();
        //[NotMapped]
        [ForeignKey(nameof(IdEmpleados))]
        public virtual Empleados? Empleados { get; set; } = new Empleados();
        [NotMapped]
        public virtual TipoDocumentos? TipoDocumentos { get; set; } = new TipoDocumentos();
        //[NotMapped]
        public virtual IEnumerable<FacturaDetalles>? FacturaDetalles { get; set; }
        public decimal Pendiente { get; set; }
       
        public bool AjustadoInventario { get; set; }
        [NotMapped]
        public Mesas? Mesas { get; set; }=new Mesas();
        public string? NombreCuenta { get; set; } = "";
        public string? Estado_Orden { get; set; }
        //public string? EstadoOrden { get; set; }
        public string? TipoOrden { get; set; } = "";
        // 🔥 ENTREGA
        public DateTime? FechaEntrega { get; set; }
        public string? HoraEntrega { get; set; }
        // 🔥 CONTROL DE ENCARGOS
        public decimal Abono { get; set; } // pago inicial
        public decimal Balance { get; set; } // Total - Pagado

        // --- Fotografía fiscal IT-1 / 607 (Sprint A) ---
        /// <summary>Código DGII 01/02/…/31…. Null si no determinado.</summary>
        public string? CodigoTipoComprobanteDgii { get; set; }
        /// <summary>Tipo ingreso Anexo A. Histórico suele ser null; nuevos docs default app=1.</summary>
        public byte? TipoIngresoDgii { get; set; }
        public byte? IndicadorFacturacion { get; set; }
        public decimal MontoGravado { get; set; }
        public decimal MontoExento { get; set; }
        public decimal MontoGravadoI1 { get; set; }
        public decimal MontoGravadoI2 { get; set; }
        public decimal MontoGravadoI3 { get; set; }
        public decimal MontoGravadoI4 { get; set; }
        public decimal DescuentoAfectaBase { get; set; }
        public decimal MontoPropinaLegal { get; set; }
        /// <summary>Solo si hay certeza (ej. crédito=15). Mixto/contado = null; usar montos por medio.</summary>
        public byte? FormaVentaFiscalDgii { get; set; }
        public string? RegimenFiscalClienteCodigo { get; set; }
        public decimal? TasaItbisPrincipal { get; set; }
        /// <summary>0 = backfill estimado; ≥1 = foto al emitir.</summary>
        public int FotografiaFiscalVersion { get; set; }
        public DateTime? FechaFotografiaFiscal { get; set; }
        public string? EstadoFiscalDocumento { get; set; }

    }
}
