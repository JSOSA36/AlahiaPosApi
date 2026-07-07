using Alahia.eCF.Api.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto.Invoice
{
    public class EncabezadoDto
    {
        public IdDocDto IdDoc { get; set; }
        public EmisorDto Emisor { get; set; }
        public CompradorDto Comprador { get; set; }
        public InformacionesAdicionalesDto InformacionesAdicionales { get; set; }
        public TotalesDto Totales { get; set; }
    }
}
