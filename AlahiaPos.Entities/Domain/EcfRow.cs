using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public record EcfRow(
        string TipoeCF,
        string ENCF,
        string RNCEmisor,
        DateTime FechaEmision,
        string RNCComprador,
        string RazonSocialComprador,
        decimal MontoGravadoTotal,
        decimal TotalITBIS,
        decimal MontoTotal
    );
}
