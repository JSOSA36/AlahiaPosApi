using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IParametroConfig
    {
        Task<IEnumerable<ParametrosConfigs>> GetAllParametrosConfig();
        ParametrosConfigs GetVyExpression();

        Task<ParametrosConfigs> GetParametrosConfigById(int id);
        Task InsertParametrosConfig(ParametrosConfigs Cocinas);

        void UpdateParametrosConfig(ParametrosConfigs ParametrosConfig);

        void DeleteParametrosConfig(int id);
    }
}
