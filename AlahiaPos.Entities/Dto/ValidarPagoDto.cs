using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class ValidarPagoDto
    {
        public int IdPago { get; set; }
        public string Estado { get; set; } // APROBADO o RECHAZADO
        public string Observacion { get; set; }
        public string UsuarioValida { get; set; }
    }
}
