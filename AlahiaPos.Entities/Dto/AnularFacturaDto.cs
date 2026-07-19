using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class AnularFacturaDto
    {
        public int IdFacturaHeader { get; set; }
        public int IdEmpresa { get; set; }
        public string MotivoAnulacion { get; set; } = "";
        public string UsuarioAnulo { get; set; } = "";
    }

}
