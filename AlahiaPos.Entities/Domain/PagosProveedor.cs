using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("PagosProveedor")]
    public class PagosProveedor : BaseEntity
    {
        [Key]
        public int IdPagoProveedor { get; set; }

        public int IdOrdenCompraHeader { get; set; }

        public string? NumeroDocumento { get; set; }

        public int IdProveedor { get; set; }

        public string FormaPago { get; set; } = string.Empty;

        public decimal Monto { get; set; }

        public string? Nota { get; set; }

        public int? IdUsuario { get; set; }

        public int? IdCuentaFinanciera { get; set; }
    }
}
