using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;

namespace AlahiaPos.DataAccess.Servicios
{
    public class TesoreriaConfiguracionService : ITesoreriaConfiguracionService
    {
        private readonly IRepository<TesoreriaConfiguracion> _repository;

        public TesoreriaConfiguracionService(IRepository<TesoreriaConfiguracion> repository)
        {
            _repository = repository;
        }

        public async Task<TesoreriaConfiguracion?> GetByEmpresaAsync(int idEmpresa)
        {
            return await _repository.GetByExpresionAsync(x => x.IdEmpresa == idEmpresa);
        }

        public async Task EnsureConfiguracionAsync(int idEmpresa)
        {
            var existente = await GetByEmpresaAsync(idEmpresa);
            if (existente != null)
                return;

            await _repository.Save(new TesoreriaConfiguracion
            {
                IdEmpresa = idEmpresa,
                ModoSaldo = "SALDO_DISPONIBLE",
                PermitirSaldoNegativo = false,
                RequiereConciliacionBanco = false,
                FechaCreacion = DateTime.UtcNow
            });
        }

        public async Task<TesoreriaConfiguracion> UpsertAsync(TesoreriaConfiguracion configuracion)
        {
            var existente = await GetByEmpresaAsync(configuracion.IdEmpresa);

            if (existente == null)
            {
                configuracion.FechaCreacion = DateTime.UtcNow;
                await _repository.Save(configuracion);
                return configuracion;
            }

            existente.ModoSaldo = configuracion.ModoSaldo;
            existente.PermitirSaldoNegativo = configuracion.PermitirSaldoNegativo;
            existente.RequiereConciliacionBanco = configuracion.RequiereConciliacionBanco;
            existente.IdCuentaCajaGeneral = configuracion.IdCuentaCajaGeneral;
            existente.IdCuentaCajaChicaDefault = configuracion.IdCuentaCajaChicaDefault;
            existente.FechaActualizacion = DateTime.UtcNow;

            _repository.Update(existente.IdTesoreriaConfiguracion, existente);
            return existente;
        }
    }
}
