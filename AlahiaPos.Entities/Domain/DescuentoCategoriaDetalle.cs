using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    public class DescuentoCategoriaDetalle
    {
        [Key]
        public int IdDescuentoCategoriaDetalle { get; set; }

        public int IdDescuentoHeader { get; set; }

        [ForeignKey(nameof(IdDescuentoHeader))]
        public DescuentoHeader? Header { get; set; }

        public int IdCategoria { get; set; }
    }
}
