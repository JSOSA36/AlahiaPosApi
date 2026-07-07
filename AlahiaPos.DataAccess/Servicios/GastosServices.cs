using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;

namespace AlahiaPos.DataAccess.Servicios
{
    public class GastosServices : IGastos
    {
        private readonly IRepository<Gastos> _services;

        public GastosServices(
            IRepository<Gastos> services
        )
        {
            _services = services;
        }

        /* =====================================
        🔥 CERRAR GASTOS PENDIENTES
        ====================================== */

        public async Task CerrarGastosPendientes(
            int idEmpresa,
            int idUsuario,
            int idCajaCierre
        )
        {
            var gastos =
                await _services
                .GetAllByExpresionAsync(x =>

                    x.IdEmpresa == idEmpresa

                    && x.IdUsuario == idUsuario

                    && x.EstaAnulado == false

                    && x.EstaCerrada != true

                    && x.FormaPago != null

                    && x.FormaPago.ToUpper() == "EFECTIVO"
                );

            foreach (var gasto in gastos)
            {
                gasto.EstaCerrada = true;

                gasto.IdCajaCierre = idCajaCierre;

                _services.Update(

                    gasto.IdGasto,

                    gasto
                );
            }
        }

        public void DeleteGastos(int id)
        {
            _services.Delete(id);
        }

        public Task<IEnumerable<Gastos>> GetAllGastos(
            int IdEmpresa
        )
        {
            return _services
                .GetAllByExpresionAsync(c =>
                    c.IdEmpresa == IdEmpresa);
        }

        public Task<Gastos> GetGastosById(int id)
        {
            return _services.GetByIdAsync(id);
        }

        public Task InsertGastos(Gastos gastos)
        {
            return _services.Save(gastos);
        }

        public async Task<decimal> TotalGastosDelMes(
            int IdEmpresa
        )
        {
            var result =
                await _services
                .GetAllByExpresionAsync(c =>

                    c.FechaInseccion.Year == DateTime.Now.Year

                    &&

                    c.FechaInseccion.Month == DateTime.Now.Month

                    &&

                    c.IdEmpresa == IdEmpresa
                );

            return result?.Sum(c => c.Monto) ?? 0;
        }

        public void UpdateGastos(
            Gastos gastos
        )
        {
            _services.Update(
                gastos.IdGasto,
                gastos
            );
        }
    }
}