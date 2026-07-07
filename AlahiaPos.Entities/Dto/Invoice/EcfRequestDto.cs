using Alahia.eCF.Api.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto.Invoice
{
    public class EcfRequestDto
    {
        public EncabezadoDto Encabezado { get; set; }
        public List<DetalleDto> Detalle { get; set; }
        public List<DescuentoRecargoDto> DescuentosORecargos { get; set; }
    }
}
