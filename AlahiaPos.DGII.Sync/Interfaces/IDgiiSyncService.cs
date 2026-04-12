using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.DGII.Sync.Interfaces
{
    public interface IDgiiSyncService
    {
        Task EjecutarSyncAsync(CancellationToken cancellationToken = default);
    }
}
