using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class CuentaFinancieraService
        : ICuentaFinancieraService
    {
        private readonly IRepository<CuentaFinanciera>
            _repository;

        private readonly IRepository<MovimientoFinanciero>
            _movimientoRepository;

        private readonly IRepository<MetodoPagoCuenta>
            _metodoPagoRepository;

        private readonly IRepository<TesoreriaSubtipoCuenta>
            _subtipoRepository;

        public CuentaFinancieraService(

            IRepository<CuentaFinanciera>
                repository,

            IRepository<MovimientoFinanciero>
                movimientoRepository,

            IRepository<MetodoPagoCuenta>
                metodoPagoRepository,

            IRepository<TesoreriaSubtipoCuenta>
                subtipoRepository
        )
        {
            _repository =
                repository;

            _movimientoRepository =
                movimientoRepository;

            _metodoPagoRepository =
                metodoPagoRepository;

            _subtipoRepository =
                subtipoRepository;
        }

        /* =====================================
        🔥 CREATE
        ===================================== */

        public async Task<int>
            CreateAsync(
                CuentaFinanciera entity
            )
        {
            if (entity.SaldoDisponible == 0 && entity.BalanceInicial != 0)
            {
                entity.SaldoDisponible = entity.BalanceInicial;
            }

            if (string.IsNullOrWhiteSpace(entity.Moneda))
            {
                entity.Moneda = "DOP";
            }

            await _repository.Save(
                entity
            );

            return entity
                .IdCuentaFinanciera;
        }

        /* =====================================
        🔥 UPDATE
        ===================================== */

        public async Task
            UpdateAsync(
                CuentaFinanciera entity
            )
        {
            _repository.Update(

                entity.IdCuentaFinanciera,

                entity
            );

            await Task.CompletedTask;
        }

        /* =====================================
        🔥 DELETE
        ===================================== */

        public async Task
            DeleteAsync(
                int id
            )
        {
            var tieneMovimientos = await _movimientoRepository.GetAny(
                x =>
                    x.IdCuentaOrigen == id
                    || x.IdCuentaDestino == id);

            if (tieneMovimientos)
                throw new InvalidOperationException(
                    "No se puede eliminar la cuenta porque tiene movimientos registrados. Desactívela en su lugar.");

            var tieneMetodoPago = await _metodoPagoRepository.GetAny(
                x => x.IdCuentaFinanciera == id);

            if (tieneMetodoPago)
                throw new InvalidOperationException(
                    "No se puede eliminar la cuenta porque está asignada a un método de pago.");

            _repository.Delete(id);

            await Task.CompletedTask;
        }

        /* =====================================
        🔥 GET BY ID
        ===================================== */

        public async Task<CuentaFinanciera?>
            GetByIdAsync(
                int id
            )
        {
            return await _repository
                .GetByIdAsync(id);
        }

        /* =====================================
        🔥 GET ALL
        ===================================== */

        public async Task<IEnumerable<CuentaFinanciera>>
            GetAllAsync()
        {
            return await _repository
                .GetAllAsync();
        }

        /* =====================================
        🔥 GET BY EMPRESA
        ===================================== */

        public async Task<IEnumerable<CuentaFinanciera>>
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

                        &&

                        x.Activa
                );
        }

        /* =====================================
        🔥 BALANCE
        ===================================== */

        public async Task<decimal>
            GetBalanceAsync(
                int idCuentaFinanciera
            )
        {
            var cuenta =
                await _repository
                .GetByIdAsync(
                    idCuentaFinanciera
                );

            if (cuenta == null)
                return 0;

            var movimientos =
                await _movimientoRepository
                .GetAllByExpresionAsync(

                    x =>

                        x.Estado == "CONFIRMADO"
                        && (
                            x.IdCuentaOrigen == idCuentaFinanciera
                            || x.IdCuentaDestino == idCuentaFinanciera
                        )
                );

            decimal entradas =

                movimientos
                .Where(

                    x =>

                        x.IdCuentaDestino
                        ==
                        idCuentaFinanciera
                )
                .Sum(
                    x => x.Monto
                );

            decimal salidas =

                movimientos
                .Where(

                    x =>

                        x.IdCuentaOrigen
                        ==
                        idCuentaFinanciera
                )
                .Sum(
                    x => x.Monto
                );

            return

                cuenta.BalanceInicial
                +
                entradas
                -
                salidas;
        }

        public async Task<IEnumerable<TesoreriaSaldoResumenDto>>
            GetResumenSaldosAsync(int idEmpresa)
        {
            var cuentas = await _repository.GetAllByExpresionAsync(
                x => x.IdEmpresa == idEmpresa);

            var subtipos = (await _subtipoRepository.GetAllAsync())
                .ToDictionary(x => x.IdTesoreriaSubtipoCuenta);

            var resultado = new List<TesoreriaSaldoResumenDto>();

            foreach (var cuenta in cuentas)
            {
                var saldoCalculado = await GetBalanceAsync(cuenta.IdCuentaFinanciera);
                TesoreriaSubtipoCuenta? subtipo = null;

                if (cuenta.IdTesoreriaSubtipoCuenta.HasValue)
                {
                    subtipos.TryGetValue(
                        cuenta.IdTesoreriaSubtipoCuenta.Value,
                        out subtipo);
                }

                resultado.Add(new TesoreriaSaldoResumenDto
                {
                    IdCuentaFinanciera = cuenta.IdCuentaFinanciera,
                    IdEmpresa = cuenta.IdEmpresa,
                    Nombre = cuenta.Nombre,
                    Codigo = cuenta.Codigo,
                    TipoCuenta = cuenta.TipoCuenta,
                    SubtipoCodigo = subtipo?.Codigo,
                    SubtipoNombre = subtipo?.Nombre,
                    Moneda = cuenta.Moneda,
                    BalanceInicial = cuenta.BalanceInicial,
                    SaldoDisponible = cuenta.SaldoDisponible,
                    SaldoCalculado = saldoCalculado,
                    DiferenciaSaldo = cuenta.SaldoDisponible - saldoCalculado,
                    Activa = cuenta.Activa,
                    EsPrincipal = cuenta.EsPrincipal,
                    IdCuentaContable = cuenta.IdCuentaContable
                });
            }

            return resultado.OrderBy(x => x.TipoCuenta).ThenBy(x => x.Nombre);
        }

        public async Task<int> SincronizarSaldosAsync(int idEmpresa)
        {
            var cuentas = await _repository.GetAllByExpresionAsync(
                x => x.IdEmpresa == idEmpresa);

            var actualizadas = 0;

            foreach (var cuenta in cuentas)
            {
                var saldoCalculado = await GetBalanceAsync(cuenta.IdCuentaFinanciera);

                if (cuenta.SaldoDisponible == saldoCalculado)
                    continue;

                cuenta.SaldoDisponible = saldoCalculado;
                await UpdateAsync(cuenta);
                actualizadas++;
            }

            return actualizadas;
        }
    }
}
