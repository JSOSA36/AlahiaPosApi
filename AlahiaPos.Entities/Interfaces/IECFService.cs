using AlahiaPos.Entities.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IECFService
    {
        //Task<DtoRespuestaDgii> ProcesarFactura(int IdFacturaHeader, int IdEmpresa);

        Task<object> ReenviarECF(int IdECF, int IdEmpresa);

        Task<object> ConsultarEstado(string TrackId, int IdEmpresa);
    }
}
