using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IEmpresas
    {
        Task<Empresas> GetEmpresaById(int Id);
        Task<Empresas> GetEmpresaByGUID(Guid Id);
        void UpdateEmpresas(int Id,Empresas empresas);
        Task InsertEmpresas(Empresas empresas);
        Task MarcarPago(int empresaId);

        Task MarcarPendiente(int empresaId);
        AlertaPagoDto ObtenerAlertaPago(Empresas empresa);
        Task ActualizarEstadoAutomatico();
        Task ActualizarEstadoEmpresa(int empresaId);
        bool PuedeOperar(Empresas empresa);
    }
}
