using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IIngresos
    {
        /// <summary>
        /// Retorna todos los ingresos registrados en una empresa.
        /// </summary>
        /// 
        Task<List<CajaMetodoPagoDto>> GetIngresosByCajaCierre(
     int idCajaCierre
 );
        Task CerrarIngresosPendientes(
    int idEmpresa,
    int idUsuario,
    int idCajaCierre
);
        Task<List<CierreCajaDto>>
GetIngresosEncargosPorFecha(

    int idEmpresa,

    DateTime fechaInicio,

    DateTime fechaFin
);
        Task<bool> ExisteIngreso(int idFactura, string metodo);
        Task<IEnumerable<IngresosPorLineaNegocioDto>> GetIngresosPorLineaNegocio(
       int idEmpresa,
       DateTime fechaInicio,
        DateTime fechaFin);
        Task<IEnumerable<Ingresos>> GetAllIngresos(int IdEmpresa);
        Task<bool> ExisteIngresoPorCita(int idCita);
        Task RevertirIngresoPorFactura(int idFactura, int idEmpresa);
        Task<List<CierreCajaDto>> GetIngresosByFechaCaja(
        int idEmpresa,
        DateTime fechaInicio,
        DateTime fechaFin);
        /// <summary>
        /// Retorna un ingreso específico por su ID.
        /// </summary>
        Task<Ingresos?> GetIngresoById(int IdIngreso);

        /// <summary>
        /// Registra un nuevo ingreso en la base de datos.
        /// </summary>
        Task InsertIngreso(Ingresos ingreso);

        /// <summary>
        /// Actualiza la información de un ingreso existente.
        /// </summary>
        Task UpdateIngreso(int IdIngreso, Ingresos ingreso);

        /// <summary>
        /// Elimina un ingreso del sistema.
        /// </summary>
        void DeleteIngreso(int IdIngreso);
        Task<List<CajaMetodoPagoDto>> GetIngresosPendientesCaja(
    int idEmpresa,
    int idUsuario
);
        /// <summary>
        /// Retorna los ingresos registrados dentro de un rango de fechas.
        /// </summary>
        Task<IEnumerable<Ingresos>> GetIngresosByFecha(int IdEmpresa, System.DateTime fechaInicio, System.DateTime fechaFin);

        /// <summary>
        /// Retorna el total de ingresos del día actual para una empresa.
        /// </summary>
        Task<decimal> GetTotalIngresosDia(int IdEmpresa);

        /// <summary>
        /// Retorna el total de ingresos del mes actual para una empresa.
        /// </summary>
        Task<decimal> GetTotalIngresosMes(int IdEmpresa);

        /// <summary>
        /// Retorna un resumen histórico de ingresos agrupado por mes (últimos 12 meses).
        /// </summary>
        Task<IEnumerable<HistoricoIngresosDto>> GetHistoricoIngresos(int IdEmpresa);

    }
}
