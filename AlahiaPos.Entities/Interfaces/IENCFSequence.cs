using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IENCFSequence
    {
        Task<string> GetNextENCFAsync(int idEmpresa, string tipoECF);
        Task<string> PeekNextENCFAsync(int idEmpresa, string tipoECF); // opcional (solo ver el próximo sin incrementar)
        Task ResetAsync(int idEmpresa, string tipoECF, int nuevoConsecutivo);
    }
}
