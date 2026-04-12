using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface ITestSetLoader
    {
        (List<EcfRow> ecfs, List<RfceRow> rfces) Cargar(string pathXlsx);
    }
}
