using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;

namespace AlahiaPos.DataAccess.Servicios
{
    public class GastosServices : IGastos
    {
        private readonly IRepository<Gastos> _services;
        private readonly IMovimientoFinancieroService _movimientoFinancieroService;
        private readonly IMetodoPagoCuentaService _metodoPagoCuentaService;

        public GastosServices(
            IRepository<Gastos> services,
            IMovimientoFinancieroService movimientoFinancieroService,
            IMetodoPagoCuentaService metodoPagoCuentaService
        )
        {
            _services = services;
            _movimientoFinancieroService = movimientoFinancieroService;
            _metodoPagoCuentaService = metodoPagoCuentaService;
        }

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

        public async Task AnularGastoAsync(
            int idGasto,
            int idEmpresa,
            string motivoAnulacion,
            string? usuarioAnulo)
        {
            var gasto = await _services.GetByExpresionAsync(x =>
                x.IdGasto == idGasto
                && x.IdEmpresa == idEmpresa);

            if (gasto == null)
                throw new KeyNotFoundException("El gasto no existe.");

            if (gasto.EstaAnulado)
                throw new InvalidOperationException("El gasto ya está anulado.");

            if (gasto.EstaCerrada == true)
                throw new InvalidOperationException(
                    "No se puede anular un gasto ya cerrado en caja.");

            if (string.Equals(gasto.OrigenModulo, "COMPRAS", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Los gastos generados por compras deben anularse desde el módulo de compras.");

            var idCuentaFinanciera = gasto.IdCuentaFinanciera;

            if (!idCuentaFinanciera.HasValue || idCuentaFinanciera.Value <= 0)
            {
                var formaPago = string.IsNullOrWhiteSpace(gasto.FormaPago)
                    ? "EFECTIVO"
                    : gasto.FormaPago;

                var metodoCuenta = await _metodoPagoCuentaService
                    .GetByMetodoAsync(idEmpresa, formaPago);

                if (metodoCuenta == null)
                    throw new InvalidOperationException(
                        "No se encontró la cuenta financiera asociada al gasto.");

                idCuentaFinanciera = metodoCuenta.IdCuentaFinanciera;
            }

            await _movimientoFinancieroService.RegistrarEntradaAsync(
                idEmpresa,
                gasto.IdUsuario ?? gasto.IdEmpleado ?? 0,
                idCuentaFinanciera.Value,
                gasto.Monto,
                $"Anulación gasto - {gasto.TipoGasto}",
                motivoAnulacion,
                categoria: "GASTO",
                referenciaId: gasto.IdGasto,
                referenciaTipo: "GASTO_ANULACION",
                claveIdempotencia: $"GASTO-ANUL-{gasto.IdGasto}");

            gasto.EstaAnulado = true;
            gasto.MotivoAnulacion = motivoAnulacion;

            _services.Update(gasto.IdGasto, gasto);
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

                    &&

                    c.EstaAnulado == false
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
