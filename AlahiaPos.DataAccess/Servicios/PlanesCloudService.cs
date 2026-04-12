using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class PlanesCloudService : IPlanesCloud
    {
        private readonly IRepository<PlanesCloud> _repositoryPlan;
        private readonly IRepository<Empresas> _repositoryEmpresa;

        public PlanesCloudService(
            IRepository<PlanesCloud> repositoryPlan,
            IRepository<Empresas> repositoryEmpresa
        )
        {
            _repositoryPlan = repositoryPlan;
            _repositoryEmpresa = repositoryEmpresa;
        }

        // =====================================================
        // 📦 PLANES
        // =====================================================

        public async Task<IEnumerable<PlanesCloud>> GetAllPlanes()
        {
            return await _repositoryPlan
                .GetAllAsync();
        }

        public async Task<PlanesCloud?> GetPlanById(int idPlan)
        {
            return await _repositoryPlan
                .GetByExpresionAsync(p => p.IdPlan == idPlan);
        }

        // =====================================================
        // 🔄 CAMBIO DE PLAN
        // =====================================================

        public async Task<IEnumerable<PlanCloudDto>> GetPlanesConActual(int idEmpresa)
        {
            var empresa = await _repositoryEmpresa.GetByIdAsync(idEmpresa);

            if (empresa == null)
                throw new Exception("Empresa no encontrada");

            var planes = await _repositoryPlan.GetAllAsync();

            var result = planes.Select(p => new PlanCloudDto
            {
                IdPlan = p.IdPlan,
                Nombre = p.Nombre,
                Precio = p.PrecioUSD,
                LimiteFacturacion=p.LimiteFacturacion,

                EsActual = p.IdPlan == empresa.IdPlan // 🔥 AQUÍ ESTÁ LA MAGIA
            });

            return result;
        }
        public async Task CambiarPlan(int idEmpresa, int idPlan)
        {
            var empresa = await _repositoryEmpresa.GetByIdAsync(idEmpresa);

            if (empresa == null)
                throw new Exception("Empresa no encontrada");

            var plan = await _repositoryPlan
                .GetByExpresionAsync(p => p.IdPlan == idPlan);

            if (plan == null)
                throw new Exception("Plan no válido");

            // 🔥 ACTUALIZAR PLAN
            empresa.IdPlan = plan.IdPlan;

            // 🔥 OPCIONAL (recomendado): guardar nombre o precio si usas cache local
            
            // 🔥 REINICIAR PERIODO
            empresa.FechaTerminacion = DateTime.Now.AddMonths(1);

            // 🔥 ACTIVAR SERVICIO (por si estaba bloqueado)
            empresa.Estado = true;

            // 🔥 GUARDAR
            _repositoryEmpresa.Update(idEmpresa, empresa);
        }
    }
}