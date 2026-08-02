using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class EmpresaServices : IEmpresas
    {
        private readonly IRepository<Empresas> repository;
        private readonly ISuscripcionCobroService _suscripcion;

        public EmpresaServices(IRepository<Empresas> repository, ISuscripcionCobroService suscripcion)
        {
            this.repository = repository;
            _suscripcion = suscripcion;
        }

        public async Task<Empresas> GetEmpresaById(int Id)
        {
            return await repository.GetByIdAsync(Id);
        }

        public void UpdateEmpresas(int Id, Empresas empresas)
        {
            repository.Update(Id, empresas);
        }

        public async Task InsertEmpresas(Empresas empresas)
        {
            await repository.Save(empresas);
        }

        public async Task<Empresas> GetEmpresaByGUID(Guid Id)
        {
            return await repository.GetByExpresionAsync(c => c.GuidPublico == Id);
        }

        public async Task ActualizarEstadoEmpresa(int empresaId)
        {
            await _suscripcion.ActualizarEstadoEmpresaAsync(empresaId);
        }

        public async Task MarcarPago(int empresaId)
        {
            await _suscripcion.OnMarcarPagoManualAsync(empresaId, "ADMIN");
        }

        public async Task MarcarPendiente(int empresaId)
        {
            var empresa = await repository.GetByIdAsync(empresaId);
            if (empresa == null) return;

            empresa.EstadoServicio = SuscripcionEstados.PendientePago;
            repository.Update(empresaId, empresa);
        }

        public async Task ActualizarEstadoAutomatico()
        {
            await _suscripcion.ProcesarCicloDiarioAsync();
        }

        public bool PuedeOperar(Empresas empresa)
        {
            return _suscripcion.PuedeOperar(empresa);
        }

        public AlertaPagoDto? ObtenerAlertaPago(Empresas empresa)
        {
            return _suscripcion.ObtenerAlertaPago(empresa);
        }
    }
}
