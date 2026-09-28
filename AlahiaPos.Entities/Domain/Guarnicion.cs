using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("Guarniciones")]
    public class Guarnicion : BaseEntity
    {
        [Key]
        public int IdGuarnicion { get; set; }

        public string Nombre { get; set; } = "";

        public bool Activo { get; set; } = true;
    }
}
