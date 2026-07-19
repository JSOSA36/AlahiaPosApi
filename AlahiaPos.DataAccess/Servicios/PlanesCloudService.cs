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
        private readonly ISuscripcionCobroService _suscripcion;

        public PlanesCloudService(
            IRepository<PlanesCloud> repositoryPlan,
            IRepository<Empresas> repositoryEmpresa,
            ISuscripcionCobroService suscripcion
        )
        {
            _repositoryPlan = repositoryPlan;
            _repositoryEmpresa = repositoryEmpresa;
            _suscripcion = suscripcion;
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

            // 🔥 ACTUALIZAR PLAN (cobro del nuevo precio en el próximo ciclo día 30)
            var planAnterior = empresa.IdPlan ?? 0;
            empresa.IdPlan = plan.IdPlan;

            // No reactivar ni prorratear: el nuevo plan se factura en el siguiente ciclo
            empresa.FechaTerminacion = DateTime.Now.AddMonths(1);
            empresa.Estado = true;

            _repositoryEmpresa.Update(idEmpresa, empresa);
            await _suscripcion.OnPlanCambiadoAsync(idEmpresa, planAnterior, plan.IdPlan, null);
        }
    }
}