using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class PagosFacturasClientes : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public int IdFacturaHeader { get; set; }
        public string NumeroDocumento { get; set; }
        public int? IDCliente { get; set; }
        public string FormaPago { get; set; }
        public decimal Monto { get; set; }
        public string? Nota { get; set; }
        

    }
}
