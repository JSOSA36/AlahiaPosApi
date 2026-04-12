using Alahia.eCF.Api.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
   public class DtoFacturaElectronica
{
        public EncabezadoDto Encabezado { get; set; }
        public List<DtoDetalleItem> Detalles { get; set; }
    }
}
