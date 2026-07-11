using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IFacturaHeader
    {


        Task<List<CierreCajaDto>> GetIngresosCajaAbierta(
   int idEmpresa,
   int idUsuario
);
        Task<
    List<CajaProductoDto>>
    GetProductosPorCajaCierre(

  int idEmpresa,

    int idUsuario,

    int idCajaCierre
    );
         Task<bool> EliminarFacturaCompleta(int idFactura);
        Task
CerrarFacturasPendientes(

   int idEmpresa,

   int idUsuario,

   int idCajaCierre
);
         Task<IEnumerable<FacturaHeaders>> GetAllOrdenesByFecha(
         int IdEmpresa,
         DateTime fechaDesde,
         DateTime fechaHasta);
        Task<IEnumerable<Reporte607Dto>>
        GetReporte607Async(
        DateTime desde,
        DateTime hasta,
        int IdEmpresa
        );
        Task<TicketFacturaClienteDto?> GetFacturaClienteById(int idFacturaHeader);
        Task<IEnumerable<FacturaHeaders>> GetAllFacturas(int IdEmpresa);
        Task<List<TicketLavadorDto>> GetTicketsLavadorByFactura(int idFacturaHeader);
        public Task<IEnumerable<FacturaHeaders>> GetAllFacturaFacturaHeader(int IdEmpresa);
        public Task<IEnumerable<FacturaHeaders>> GetAllFacturaPendientes(int IdCliente, int IdEmpresa);
        public Task<IEnumerable<FacturaHeaders>> GetAllFactById(int _IdFact);
        public Task<List<HistoricoVentaDto>> GetSumFactura(int IdEmpresa);
        public IEnumerable<ComisionesResultDto> 
            GetComisionesDetalle(DateTime Desde, DateTime Hasta, int IdEmpresa);
        public decimal GetMontoEfectivo(DateTime _Desde, DateTime _Hasta, int IdEmpresa);
        public decimal GetMontoTarjeta(DateTime _Desde, DateTime _Hasta, int IdEmpresa);
        public decimal GetMontoTransferencia(DateTime _Desde, DateTime _Hasta, int IdEmpresa);
        public  FacturaHeaders GetById(int Id);
        public IEnumerable<ServicioEmpleadoDto> GetServicioByEMpleados(DateTime Desde, DateTime Hasta, int IdEmpresa);
        public Task<decimal> GetVentaDelDia(int IdEmpresa);
        public Task<decimal> GetTotalFactura(int IdEmpresa);
        public Task<decimal> GetTotalFacturaPorCobrar(int IdEmpresa);
        public Task<FacturaHeaders> GetAllFacturaFacturaHeaderById(int Id, int IdEmpresa);
        public Task <IEnumerable<FacturaHeaders>> GetAllFacturaFacturaHeaderByIdMesa(int IdEmpresa);
        public void UpdateFacturaHeader(int Id, FacturaHeaders FacturaHeader);
        public Task InsertFacturaHeader(FacturaHeaders FacturaHeader);
        public Task<FacturaHeaders> GetFacturaHeaderById(int IdFacturaHeader, int IdEmpresa);
        public Task<IEnumerable<FacturaHeaders>> GetAllOrdenes(int IdEmpresa);
        public Task<IEnumerable<FacturaHeaders>> GetAllCotizaciones(int IdEmpresa);
        public Task<IEnumerable<FacturaHeaders>> GetFacturasXCobrar(DateTime? Desde,
           DateTime? Hasta, int IdCliente, int IdEmpresa);

        public Task<IEnumerable<ServicioRankingDto>> GetTopServiciosDelMes(int IdEmpresa);
        public Task<IEnumerable<CuentaPorCobrarDto>> GetCuentasPorCobrar(int IdEmpresa);
        Task<
List<CajaProductoDto>>
GetProductosPendientesCierre(

    int idEmpresa,

    int idUsuario
    
);

    }
}
