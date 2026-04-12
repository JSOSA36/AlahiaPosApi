using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IPlanesCloud
    {
        Task<IEnumerable<PlanesCloud>> GetAllPlanes();
        Task CambiarPlan(int idEmpresa, int idPlan);
        Task<PlanesCloud?> GetPlanById(int idPlan);
        Task<IEnumerable<PlanCloudDto>> GetPlanesConActual(int idEmpresa);

        // =====================================================
        // 🔄 CAMBIO DE PLAN
        // =====================================================


    }
}
