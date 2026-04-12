using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class IParametroCOnfigServices : IParametroConfig
    {
        IRepository<ParametrosConfigs> _services;
        
        public IParametroCOnfigServices(IRepository<ParametrosConfigs> services)
        {
            _services = services;
        }
        public void DeleteParametrosConfig(int id)
        {
            _services.Delete(id);
        }
        public ParametrosConfigs GetVyExpression()
        {
            return _services.GetByExpresion(c=>c.IdEmpresa==1 && c.NombreParametro== "ImpuestoItbis");
        }
        public async Task<IEnumerable<ParametrosConfigs>> GetAllParametrosConfig()
        {
            return await _services.GetAllAsync();
        }

        public Task<ParametrosConfigs> GetParametrosConfigById(int id)
        {
            return _services.GetByIdAsync(id);
        }

        public async Task InsertParametrosConfig(ParametrosConfigs ParametrosConfig)
        {
            await _services.Save(ParametrosConfig);
        }

        public void UpdateParametrosConfig(ParametrosConfigs ParametrosConfig)
        {
            _services.Update(ParametrosConfig.IdParametrosConfig, ParametrosConfig);
        }
    }
}
