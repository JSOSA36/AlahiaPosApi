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

        public CuentaFinancieraService(

            IRepository<CuentaFinanciera>
                repository,

            IRepository<MovimientoFinanciero>
                movimientoRepository
        )
        {
            _repository =
                repository;

            _movimientoRepository =
                movimientoRepository;
        }

        /* =====================================
        🔥 CREATE
        ===================================== */

        public async Task<int>
            CreateAsync(
                CuentaFinanciera entity
            )
        {
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

                        x.IdCuentaOrigen
                        ==
                        idCuentaFinanciera

                        ||

                        x.IdCuentaDestino
                        ==
                        idCuentaFinanciera
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
    }
}
