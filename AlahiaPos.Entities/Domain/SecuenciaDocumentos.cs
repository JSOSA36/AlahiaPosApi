using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class SecuenciaDocumentos
    {
        [Key]
        public int Id { get; set; }

        public int SecuenciaInicial { get; set; }

        public int SecuenciaActual { get; set; }

        public string? Prefijo { get; set; }

        public int IdTipoDocumento { get; set; }

        public DateTime FechaInseccion { get; set; }

        public int IdEmpresa { get; set; }
    }
}
