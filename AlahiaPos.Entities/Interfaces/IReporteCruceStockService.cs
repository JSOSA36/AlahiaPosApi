using AlahiaPos.Entities.Dto;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IReporteCruceStockService
    {
        Task<List<CruceStockVentaLineaDto>> ObtenerCruceAsync(
            int idEmpresa,
            DateTime desde,
            DateTime hasta,
            int? idProducto = null,
            int? idUsuario = null,
            int? idCajaCierre = null);
    }
}
