using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.DGII.Sync.Interfaces
{
    public interface IDgiiProcessor
    {
        Task ProcesarArchivoAsync(string carpeta, CancellationToken cancellationToken = default);
    }
}
