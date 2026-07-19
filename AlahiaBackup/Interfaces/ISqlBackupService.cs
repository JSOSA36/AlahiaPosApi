using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaBackup.Interfaces
{
    public interface ISqlBackupService
    {
        Task RealizarBackupAsync();
    }
}
