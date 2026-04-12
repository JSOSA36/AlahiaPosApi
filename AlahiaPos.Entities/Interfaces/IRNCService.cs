using AlahiaPos.Entities.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IRNCService
    {
        Task<ClienteDgiiDto?> ConsultarAsync(string rncOrCedula);
    }
}
