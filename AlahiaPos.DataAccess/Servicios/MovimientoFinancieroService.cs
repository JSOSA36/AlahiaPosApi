using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;

namespace AlahiaPos.DataAccess.Servicios
{
    public class MovimientoFinancieroService
      : IMovimientoFinancieroService
    {
        private readonly IRepository<MovimientoFinanciero>
            _repository;

        private readonly ICuentaFinancieraService
            _cuentaRepository;

        public MovimientoFinancieroService(

            IRepository<MovimientoFinanciero>
                repository,

            ICuentaFinancieraService
                cuentaRepository
        )
        {
            _repository =
                repository;

            _cuentaRepository =
                cuentaRepository;
        }

        public async Task<int>
            CreateAsync(
                MovimientoFinanciero entity
            )
        {
            await _repository.Save(
                entity
            );

            return entity
                .IdMovimientoFinanciero;
        }

        public async Task
            UpdateAsync(
                MovimientoFinanciero entity
            )
        {
            _repository.Update(

                entity.IdMovimientoFinanciero,

                entity
            );

            await Task.CompletedTask;
        }

        public async Task
            DeleteAsync(
                int id
            )
        {
            _repository.Delete(id);

            await Task.CompletedTask;
        }

        public async Task<MovimientoFinanciero?>
            GetByIdAsync(
                int id
            )
        {
            return await _repository
                .GetByIdAsync(id);
        }

        public async Task<IEnumerable<MovimientoFinanciero>>
            GetAllAsync()
        {
            return await _repository
                .GetAllAsync();
        }

        public async Task<IEnumerable<MovimientoFinanciero>>
            GetByEmpresaAsync(
                int idEmpresa
            )
        {
            return await _repository
                .GetAllByExpresionAsync(

                    x =>

                        x.IdEmpresa
                        ==
                        idEmpresa
                );
        }

        public async Task<IEnumerable<MovimientoFinanciero>>
            GetByCuentaAsync(
                int idCuentaFinanciera
            )
        {
            return await _repository
                .GetAllByExpresionAsync(

                    x =>

                        x.IdCuentaOrigen
                        ==
                        idCuentaFinanciera

                        ||

                        x.IdCuentaDestino
                        ==
                        idCuentaFinanciera
                );
        }

        public async Task<IEnumerable<MovimientoFinanciero>>
            GetByFechaAsync(

                int idEmpresa,

                DateTime desde,

                DateTime hasta
            )
        {
            return await _repository
                .GetAllByExpresionAsync(

                    x =>

                        x.IdEmpresa
                        ==
                        idEmpresa

                        &&

                        x.FechaMovimiento.Date
                        >=
                        desde.Date

                        &&

                        x.FechaMovimiento.Date
                        <=
                        hasta.Date
                );
        }

        public async Task RegistrarEntradaAsync(

            int idEmpresa,

            int idUsuario,

            int idCuentaDestino,

            decimal monto,

            string motivo,

            string? observacion,

            string? categoria = null,

            int? referenciaId = null,

            string? referenciaTipo = null,

            string? claveIdempotencia = null
        )
        {
            if (monto <= 0)
                throw new Exception("El monto debe ser mayor a cero.");

            if (await ExisteIdempotenciaAsync(idEmpresa, claveIdempotencia))
                return;

            var cuenta =
                await _cuentaRepository
                .GetByIdAsync(idCuentaDestino);

            if (cuenta == null)
                throw new Exception("La cuenta financiera no existe.");

            var balanceAnterior = cuenta.SaldoDisponible;
            cuenta.SaldoDisponible += monto;

            await _cuentaRepository.UpdateAsync(cuenta);

            var movimiento =
                new MovimientoFinanciero
                {
                    IdEmpresa = idEmpresa,
                    IdUsuario = idUsuario,
                    IdCuentaDestino = idCuentaDestino,
                    TipoMovimiento = "ENTRADA",
                    Categoria = string.IsNullOrWhiteSpace(categoria) ? "AJUSTE" : categoria,
                    ReferenciaId = referenciaId,
                    ReferenciaTipo = referenciaTipo,
                    Monto = monto,
                    Motivo = motivo,
                    Observacion = observacion,
                    BalanceAnteriorDestino = balanceAnterior,
                    BalanceNuevoDestino = cuenta.SaldoDisponible,
                    ClaveIdempotencia = claveIdempotencia,
                    Estado = "CONFIRMADO",
                    FechaMovimiento = DateTime.Now,
                    FechaRegistro = DateTime.Now
                };

            await _repository.Save(movimiento);
        }

        public async Task RegistrarSalidaAsync(

            int idEmpresa,

            int idUsuario,

            int idCuentaOrigen,

            decimal monto,

            string motivo,

            string? observacion,

            string? categoria = null,

            int? referenciaId = null,

            string? referenciaTipo = null,

            string? claveIdempotencia = null
        )
        {
            if (monto <= 0)
                throw new Exception("El monto debe ser mayor a cero.");

            if (await ExisteIdempotenciaAsync(idEmpresa, claveIdempotencia))
                return;

            var cuenta = await _cuentaRepository
                .GetByIdAsync(idCuentaOrigen);

            if (cuenta == null)
                throw new Exception("Cuenta no encontrada");

            if (!cuenta.PermiteSaldoNegativo && cuenta.SaldoDisponible < monto)
                throw new Exception("Fondos insuficientes");

            var balanceAnterior = cuenta.SaldoDisponible;
            cuenta.SaldoDisponible -= monto;

            await _cuentaRepository.UpdateAsync(cuenta);

            var movimiento = new MovimientoFinanciero
            {
                IdEmpresa = idEmpresa,
                IdUsuario = idUsuario,
                IdCuentaOrigen = idCuentaOrigen,
                TipoMovimiento = "SALIDA",
                Categoria = string.IsNullOrWhiteSpace(categoria) ? "AJUSTE" : categoria,
                ReferenciaId = referenciaId,
                ReferenciaTipo = referenciaTipo,
                Monto = monto,
                Motivo = motivo,
                Observacion = observacion,
                BalanceAnteriorOrigen = balanceAnterior,
                BalanceNuevoOrigen = cuenta.SaldoDisponible,
                ClaveIdempotencia = claveIdempotencia,
                Estado = "CONFIRMADO",
                FechaMovimiento = DateTime.Now,
                FechaRegistro = DateTime.Now
            };

            await _repository.Save(movimiento);
        }

        public async Task RegistrarTransferenciaAsync(

            int idEmpresa,

            int idUsuario,

            int idCuentaOrigen,

            int idCuentaDestino,

            decimal monto,

            string motivo,

            string? observacion,

            string? claveIdempotencia = null
        )
        {
            if (monto <= 0)
                throw new Exception("El monto debe ser mayor a cero.");

            if (idCuentaOrigen == idCuentaDestino)
                throw new Exception("La cuenta origen y destino deben ser diferentes.");

            if (await ExisteIdempotenciaAsync(idEmpresa, claveIdempotencia))
                return;

            var cuentaOrigen = await _cuentaRepository.GetByIdAsync(idCuentaOrigen);
            var cuentaDestino = await _cuentaRepository.GetByIdAsync(idCuentaDestino);

            if (cuentaOrigen == null || cuentaDestino == null)
                throw new Exception("Una de las cuentas financieras no existe.");

            if (!cuentaOrigen.PermiteSaldoNegativo && cuentaOrigen.SaldoDisponible < monto)
                throw new Exception("Fondos insuficientes en la cuenta origen.");

            var balanceAnteriorOrigen = cuentaOrigen.SaldoDisponible;
            var balanceAnteriorDestino = cuentaDestino.SaldoDisponible;

            cuentaOrigen.SaldoDisponible -= monto;
            cuentaDestino.SaldoDisponible += monto;

            await _cuentaRepository.UpdateAsync(cuentaOrigen);
            await _cuentaRepository.UpdateAsync(cuentaDestino);

            var movimiento =
                new MovimientoFinanciero
                {
                    IdEmpresa = idEmpresa,
                    IdUsuario = idUsuario,
                    IdCuentaOrigen = idCuentaOrigen,
                    IdCuentaDestino = idCuentaDestino,
                    TipoMovimiento = "TRANSFERENCIA",
                    Categoria = "TRANSFERENCIA",
                    Monto = monto,
                    Motivo = motivo,
                    Observacion = observacion,
                    BalanceAnteriorOrigen = balanceAnteriorOrigen,
                    BalanceNuevoOrigen = cuentaOrigen.SaldoDisponible,
                    BalanceAnteriorDestino = balanceAnteriorDestino,
                    BalanceNuevoDestino = cuentaDestino.SaldoDisponible,
                    ClaveIdempotencia = claveIdempotencia,
                    Estado = "CONFIRMADO",
                    FechaMovimiento = DateTime.Now,
                    FechaRegistro = DateTime.Now
                };

            await _repository.Save(movimiento);
        }

        private async Task<bool> ExisteIdempotenciaAsync(
            int idEmpresa,
            string? claveIdempotencia
        )
        {
            if (string.IsNullOrWhiteSpace(claveIdempotencia))
                return false;

            var existente = await _repository.GetByExpresionAsync(
                x =>
                    x.IdEmpresa == idEmpresa
                    && x.ClaveIdempotencia == claveIdempotencia
            );

            return existente != null;
        }
    }
}
