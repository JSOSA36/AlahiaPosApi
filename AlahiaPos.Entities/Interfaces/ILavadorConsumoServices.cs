using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface ILavadorConsumoServices
    {
        // ⭐ registrar consumo (fiado del lavador)
        decimal GetConsumoLavador(DateTime desde, DateTime hasta, int idEmpleado, int idEmpresa);
        Task RegistrarConsumo(LavadorConsumoCreateDto dto);
        Task EliminarConsumo(int idConsumo);
        // ⭐ abonar a un consumo específico
        Task AbonarConsumo(int idConsumo, decimal montoAbono);

        // ⭐ saldar consumo completo manualmente
        Task SaldarConsumo(int idConsumo);

        // ⭐ obtener consumos pendientes por lavador
        Task<IEnumerable<LavadorConsumo>>
            GetPendientesByEmpleado(int idEmpleado, int idEmpresa);

        // ⭐ balance actual del lavador (lo que debe)
        Task<decimal>
            GetBalanceLavador(int idEmpleado, int idEmpresa);

        // ⭐ listado general administrativo
        Task<IEnumerable<LavadorConsumo>>
            GetListadoGeneral(int idEmpresa);

        // ⭐ historial por rango de fechas
        Task<IEnumerable<LavadorConsumo>>
            GetHistorialByEmpleado(int idEmpleado, int idEmpresa, DateTime desde, DateTime hasta);

        // ⭐ DASHBOARD financiero del lavador (🔥 módulo estrella)
       Task<LavadorDashboardDto> GetDashboardLavador(
      int idEmpleado,
      int idEmpresa,
      DateTime desde,
      DateTime hasta);

    }
}