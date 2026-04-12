using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class Zonas:BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ZonaId { get; set; }
        public string ZonaName { get; set; } = "";
        public bool Estado { get; set; }
        public virtual IEnumerable<Mesas>? Mesas { get; set; }
        public int ImpresoraId { get; set; }
        [ForeignKey("ImpresoraId")]
        public virtual ImpresorasZonas? ImpresorasZonas { get; set; }
    }
}
