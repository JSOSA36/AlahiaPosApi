using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        /* =====================================
        🔥 CREATE
        ===================================== */

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

        /* =====================================
        🔥 UPDATE
        ===================================== */

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

        public async Task<MovimientoFinanciero?>
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

        public async Task<IEnumerable<MovimientoFinanciero>>
            GetAllAsync()
        {
            return await _repository
                .GetAllAsync();
        }

        /* =====================================
        🔥 GET BY EMPRESA
        ===================================== */

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

        /* =====================================
        🔥 GET BY CUENTA
        ===================================== */

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

        /* =====================================
        🔥 GET BY FECHA
        ===================================== */

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

        /* =====================================
        🔥 ENTRADA
        ===================================== */

        public async Task RegistrarEntradaAsync(

      int idEmpresa,

      int idUsuario,

      int idCuentaDestino,

      decimal monto,

      string motivo,

      string? observacion
  )
        {
            // =====================================
            // 🔥 OBTENER CUENTA
            // =====================================

            var cuenta =
                await _cuentaRepository
                .GetByIdAsync(idCuentaDestino);

            if (cuenta == null)
            {
                throw new Exception(
                    "La cuenta financiera no existe."
                );
            }

            // =====================================
            // 🔥 ACTUALIZAR SALDO
            // =====================================

            cuenta.SaldoDisponible += monto;

            await _cuentaRepository
                .UpdateAsync(cuenta);

            // =====================================
            // 🔥 REGISTRAR MOVIMIENTO
            // =====================================

            var movimiento =
                new MovimientoFinanciero
                {
                    IdEmpresa =
                        idEmpresa,

                    IdUsuario =
                        idUsuario,

                    IdCuentaDestino =
                        idCuentaDestino,

                    TipoMovimiento =
                        "ENTRADA",

                    Categoria =
                        "AJUSTE",

                    Monto =
                        monto,

                    Motivo =
                        motivo,

                    Observacion =
                        observacion
                };

            await _repository
                .Save(movimiento);
        }

        /* =====================================
        🔥 SALIDA
        ===================================== */

        public async Task RegistrarSalidaAsync(

      int idEmpresa,

      int idUsuario,

      int idCuentaOrigen,

      decimal monto,

      string motivo,

      string? observacion
  )
        {
            // =========================================
            // 🔥 CUENTA
            // =========================================

            var cuenta = await _cuentaRepository
                .GetByIdAsync(idCuentaOrigen);

            if (cuenta == null)
            {
                throw new Exception("Cuenta no encontrada");
            }

            // =========================================
            // 🔥 VALIDAR FONDOS
            // =========================================

            if (cuenta.SaldoDisponible < monto)
            {
                throw new Exception("Fondos insuficientes");
            }

            // =========================================
            // 🔥 ACTUALIZAR SALDO
            // =========================================

            cuenta.SaldoDisponible -= monto;

            // =========================================
            // 🔥 GUARDAR CUENTA
            // =========================================

            await _cuentaRepository.UpdateAsync(cuenta);

            // =========================================
            // 🔥 MOVIMIENTO
            // =========================================

            var movimiento = new MovimientoFinanciero
            {
                IdEmpresa = idEmpresa,
                IdUsuario = idUsuario,
                IdCuentaOrigen = idCuentaOrigen,
                TipoMovimiento = "SALIDA",
                Categoria = "AJUSTE",
                Monto = monto,
                Motivo = motivo,
                Observacion = observacion
            };

            // =========================================
            // 🔥 GUARDAR MOVIMIENTO
            // =========================================

            await _repository.Save(movimiento);
        }

        /* =====================================
        🔥 TRANSFERENCIA
        ===================================== */

        public async Task RegistrarTransferenciaAsync(

            int idEmpresa,

            int idUsuario,

            int idCuentaOrigen,

            int idCuentaDestino,

            decimal monto,

            string motivo,

            string? observacion
        )
        {
            var movimiento =
                new MovimientoFinanciero
                {
                    IdEmpresa =
                        idEmpresa,

                    IdUsuario =
                        idUsuario,

                    IdCuentaOrigen =
                        idCuentaOrigen,

                    IdCuentaDestino =
                        idCuentaDestino,

                    TipoMovimiento =
                        "TRANSFERENCIA",

                    Categoria =
                        "TRANSFERENCIA",

                    Monto =
                        monto,

                    Motivo =
                        motivo,

                    Observacion =
                        observacion
                };

            await _repository
                .Save(movimiento);
        }
    }
}
