using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IDgiiClient
    {
        Task<string> AutenticarAsync();
        Task<string> EnviarRecepcionAsync(string token, string xmlFirmado);
        Task<string> EnviarRecepcionFCAsync(string token, string xmlFirmado);
        Task<string> ConsultarResultadoAsync(string token, string trackId);
    }
}
