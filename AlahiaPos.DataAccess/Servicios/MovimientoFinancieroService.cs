using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Events;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    public class MovimientoFinancieroService
      : IMovimientoFinancieroService
    {
        private readonly IRepository<MovimientoFinanciero> _repository;
        private readonly ICuentaFinancieraService _cuentaRepository;
        private readonly IContabilidadEventPublisher _contabilidadEvents;
        private readonly AlahiaPosContext _context;

        private static readonly HashSet<string> CategoriasSinEventoBancario = new(StringComparer.OrdinalIgnoreCase)
        {
            "VENTA", "GASTO", "COMPRA", "COBRO", "PAGO", "PAGO_PROVEEDOR", "PAGO_CLIENTE",
            "INGRESO", "INGRESO_EXTRA", "CIERRE_CAJA"
        };

        private static readonly HashSet<string> CategoriasEventoBancario = new(StringComparer.OrdinalIgnoreCase)
        {
            "AJUSTE", "MANUAL", "CIERRE", "COMISION",
            "COMISION_BANCARIA", "CARGO_BANCARIO", "IMPUESTO_BANCARIO", "INTERES_BANCARIO",
            "DEBITO_AUTOMATICO", "CREDITO_AUTOMATICO", "AJUSTE_BANCARIO",
            "REVERSO_BANCARIO", "OTRO_BANCARIO"
        };

        public MovimientoFinancieroService(
            IRepository<MovimientoFinanciero> repository,
            ICuentaFinancieraService cuentaRepository,
            IContabilidadEventPublisher contabilidadEvents,
            AlahiaPosContext context)
        {
            _repository = repository;
            _cuentaRepository = cuentaRepository;
            _contabilidadEvents = contabilidadEvents;
            _context = context;
        }

        public async Task<int> CreateAsync(MovimientoFinanciero entity)
        {
            await _repository.Save(entity);
            return entity.IdMovimientoFinanciero;
        }

        public async Task UpdateAsync(MovimientoFinanciero entity)
        {
            _repository.Update(entity.IdMovimientoFinanciero, entity);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            _repository.Delete(id);
            await Task.CompletedTask;
        }

        public async Task<MovimientoFinanciero?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<MovimientoFinanciero>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<IEnumerable<MovimientoFinanciero>> GetByEmpresaAsync(int idEmpresa)
        {
            return await _repository.GetAllByExpresionAsync(x => x.IdEmpresa == idEmpresa);
        }

        public async Task<IEnumerable<MovimientoFinanciero>> GetByCuentaAsync(int idCuentaFinanciera)
        {
            return await _repository.GetAllByExpresionAsync(
                x => x.IdCuentaOrigen == idCuentaFinanciera || x.IdCuentaDestino == idCuentaFinanciera);
        }

        public async Task<IEnumerable<MovimientoFinanciero>> GetByFechaAsync(
            int idEmpresa,
            DateTime desde,
            DateTime hasta)
        {
            return await _repository.GetAllByExpresionAsync(
                x =>
                    x.IdEmpresa == idEmpresa
                    && x.FechaMovimiento.Date >= desde.Date
                    && x.FechaMovimiento.Date <= hasta.Date);
        }

        public async Task<IEnumerable<MovimientoFinancieroListadoDto>> ConsultarAsync(
            MovimientoFinancieroFiltroDto filtro)
        {
            if (filtro.IdCuentaFinanciera is not > 0)
                throw new InvalidOperationException("IdCuentaFinanciera es requerido para consulta con saldo acumulado.");

            return await ConstruirListadoConSaldoAsync(
                filtro.IdCuentaFinanciera.Value,
                filtro.IdEmpresa,
                filtro.Desde,
                filtro.Hasta,
                filtro.TipoMovimiento,
                filtro.Categoria,
                filtro.Estado,
                filtro.DocumentoReferencia,
                filtro.EstadoConciliacion);
        }

        public async Task<EstadoCuentaDto> GetEstadoCuentaAsync(
            int idCuentaFinanciera,
            DateTime? desde = null,
            DateTime? hasta = null)
        {
            var cuenta = await _cuentaRepository.GetByIdAsync(idCuentaFinanciera)
                ?? throw new InvalidOperationException("La cuenta financiera no existe.");

            var movimientos = await ConstruirListadoConSaldoAsync(
                idCuentaFinanciera,
                cuenta.IdEmpresa,
                desde,
                hasta,
                estado: "CONFIRMADO");

            var lista = movimientos.ToList();
            var saldoInicial = await CalcularSaldoHastaAsync(idCuentaFinanciera, desde?.Date.AddDays(-1));

            return new EstadoCuentaDto
            {
                IdCuentaFinanciera = idCuentaFinanciera,
                NombreCuenta = cuenta.Nombre,
                SaldoInicial = saldoInicial,
                SaldoFinal = lista.LastOrDefault()?.SaldoAcumulado ?? saldoInicial,
                Desde = desde,
                Hasta = hasta,
                Movimientos = lista
            };
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
            string? claveIdempotencia = null)
        {
            if (monto <= 0)
                throw new Exception("El monto debe ser mayor a cero.");

            if (await ExisteIdempotenciaAsync(idEmpresa, claveIdempotencia))
                return;

            var cuenta = await _cuentaRepository.GetByIdAsync(idCuentaDestino)
                ?? throw new Exception("La cuenta financiera no existe.");

            ValidarMovimientoManual(cuenta, categoria);

            var balanceAnterior = cuenta.SaldoDisponible;
            cuenta.SaldoDisponible += monto;
            await _cuentaRepository.UpdateAsync(cuenta);

            var movimiento = new MovimientoFinanciero
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
                EstadoConciliacion = "PENDIENTE",
                FechaMovimiento = DateTime.Now,
                FechaRegistro = DateTime.Now
            };

            await _repository.Save(movimiento);

            await PublicarAjusteTesoreriaSiAplicaAsync(
                movimiento,
                cuenta.TipoCuenta,
                idCuentaDestino: idCuentaDestino,
                idCuentaOrigen: null);
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
            string? claveIdempotencia = null)
        {
            if (monto <= 0)
                throw new Exception("El monto debe ser mayor a cero.");

            if (await ExisteIdempotenciaAsync(idEmpresa, claveIdempotencia))
                return;

            var cuenta = await _cuentaRepository.GetByIdAsync(idCuentaOrigen)
                ?? throw new Exception("Cuenta no encontrada");

            ValidarMovimientoManual(cuenta, categoria);

            // Los movimientos que provienen de la conciliación bancaria (EXTRACTO)
            // registran hechos que el banco ya ejecutó: el estado de cuenta es la
            // fuente de verdad, así que no se valida saldo en libros.
            var esRegistroDesdeExtracto = string.Equals(
                referenciaTipo, "EXTRACTO", StringComparison.OrdinalIgnoreCase);

            if (!esRegistroDesdeExtracto
                && !cuenta.PermiteSaldoNegativo
                && cuenta.SaldoDisponible < monto)
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
                EstadoConciliacion = "PENDIENTE",
                FechaMovimiento = DateTime.Now,
                FechaRegistro = DateTime.Now
            };

            await _repository.Save(movimiento);

            await PublicarAjusteTesoreriaSiAplicaAsync(
                movimiento,
                cuenta.TipoCuenta,
                idCuentaDestino: null,
                idCuentaOrigen: idCuentaOrigen);
        }

        public async Task RegistrarTransferenciaAsync(
            int idEmpresa,
            int idUsuario,
            int idCuentaOrigen,
            int idCuentaDestino,
            decimal monto,
            string motivo,
            string? observacion,
            string? claveIdempotencia = null)
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

            var movimiento = new MovimientoFinanciero
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
                EstadoConciliacion = "PENDIENTE",
                FechaMovimiento = DateTime.Now,
                FechaRegistro = DateTime.Now
            };

            await _repository.Save(movimiento);

            await _contabilidadEvents.TryPublishAsync(new MovimientoBancarioRegistradoEvent
            {
                IdEmpresa = idEmpresa,
                IdUsuario = idUsuario,
                Fecha = DateTime.Now,
                ReferenciaId = movimiento.IdMovimientoFinanciero,
                ReferenciaTipo = "Transferencia",
                TipoMovimiento = "TRANSFERENCIA",
                Categoria = "TRANSFERENCIA",
                Monto = monto,
                IdCuentaOrigen = idCuentaOrigen,
                IdCuentaDestino = idCuentaDestino,
                TipoCuentaOrigen = cuentaOrigen.TipoCuenta,
                TipoCuentaDestino = cuentaDestino.TipoCuenta,
                Motivo = motivo
            });
        }

        public async Task<int> RegistrarTransferenciaReclasificacionAsync(
            int idEmpresa,
            int idUsuario,
            int idCuentaOrigen,
            int idCuentaDestino,
            decimal monto,
            string motivo,
            string? observacion,
            string claveIdempotencia,
            int? referenciaId,
            DateTime fechaMovimiento)
        {
            if (monto <= 0)
                throw new Exception("El monto debe ser mayor a cero.");

            if (idCuentaOrigen == idCuentaDestino)
                throw new Exception("La cuenta origen y destino deben ser diferentes.");

            if (string.IsNullOrWhiteSpace(claveIdempotencia))
                throw new Exception("La clave de idempotencia es obligatoria para reclasificación.");

            var existente = await _repository.GetAllByExpresionAsync(x =>
                x.IdEmpresa == idEmpresa && x.ClaveIdempotencia == claveIdempotencia);
            var prev = existente.FirstOrDefault();
            if (prev != null)
                return prev.IdMovimientoFinanciero;

            var cuentaOrigen = await _cuentaRepository.GetByIdAsync(idCuentaOrigen);
            var cuentaDestino = await _cuentaRepository.GetByIdAsync(idCuentaDestino);

            if (cuentaOrigen == null || cuentaDestino == null)
                throw new Exception("Una de las cuentas financieras no existe.");

            // Banco es fuente de verdad: se permite dejar Caja en negativo al reclasificar.
            var balanceAnteriorOrigen = cuentaOrigen.SaldoDisponible;
            var balanceAnteriorDestino = cuentaDestino.SaldoDisponible;

            cuentaOrigen.SaldoDisponible -= monto;
            cuentaDestino.SaldoDisponible += monto;

            await _cuentaRepository.UpdateAsync(cuentaOrigen);
            await _cuentaRepository.UpdateAsync(cuentaDestino);

            var movimiento = new MovimientoFinanciero
            {
                IdEmpresa = idEmpresa,
                IdUsuario = idUsuario,
                IdCuentaOrigen = idCuentaOrigen,
                IdCuentaDestino = idCuentaDestino,
                TipoMovimiento = "TRANSFERENCIA",
                Categoria = "RECLASIFICAR_PAGO",
                ReferenciaId = referenciaId,
                ReferenciaTipo = "RECLASIFICAR_PAGO",
                Monto = monto,
                Motivo = motivo,
                Observacion = observacion,
                BalanceAnteriorOrigen = balanceAnteriorOrigen,
                BalanceNuevoOrigen = cuentaOrigen.SaldoDisponible,
                BalanceAnteriorDestino = balanceAnteriorDestino,
                BalanceNuevoDestino = cuentaDestino.SaldoDisponible,
                ClaveIdempotencia = claveIdempotencia,
                Estado = "CONFIRMADO",
                EstadoConciliacion = "PENDIENTE",
                FechaMovimiento = fechaMovimiento,
                FechaRegistro = DateTime.Now
            };

            await _repository.Save(movimiento);

            await _contabilidadEvents.TryPublishAsync(new MovimientoBancarioRegistradoEvent
            {
                IdEmpresa = idEmpresa,
                IdUsuario = idUsuario,
                Fecha = fechaMovimiento,
                ReferenciaId = movimiento.IdMovimientoFinanciero,
                ReferenciaTipo = "RECLASIFICAR_PAGO",
                TipoMovimiento = "TRANSFERENCIA",
                Categoria = "RECLASIFICAR_PAGO",
                Monto = monto,
                IdCuentaOrigen = idCuentaOrigen,
                IdCuentaDestino = idCuentaDestino,
                TipoCuentaOrigen = cuentaOrigen.TipoCuenta,
                TipoCuentaDestino = cuentaDestino.TipoCuenta,
                Motivo = motivo
            });

            return movimiento.IdMovimientoFinanciero;
        }

        public async Task<int> RegistrarAjusteAsync(RegistrarAjusteDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Motivo))
                throw new InvalidOperationException("El motivo es obligatorio para ajustes.");

            var tipo = (dto.TipoMovimiento ?? string.Empty).Trim().ToUpperInvariant();
            if (tipo is not ("ENTRADA" or "SALIDA"))
                throw new InvalidOperationException("TipoMovimiento debe ser ENTRADA o SALIDA.");

            if (tipo == "ENTRADA")
            {
                await RegistrarEntradaAsync(
                    dto.IdEmpresa,
                    dto.IdUsuario,
                    dto.IdCuentaFinanciera,
                    dto.Monto,
                    dto.Motivo,
                    dto.Observacion,
                    categoria: "AJUSTE",
                    claveIdempotencia: dto.ClaveIdempotencia);
            }
            else
            {
                await RegistrarSalidaAsync(
                    dto.IdEmpresa,
                    dto.IdUsuario,
                    dto.IdCuentaFinanciera,
                    dto.Monto,
                    dto.Motivo,
                    dto.Observacion,
                    categoria: "AJUSTE",
                    claveIdempotencia: dto.ClaveIdempotencia);
            }

            if (string.IsNullOrWhiteSpace(dto.ClaveIdempotencia))
            {
                var ultimo = await _context.MovimientoFinanciero
                    .OrderByDescending(x => x.IdMovimientoFinanciero)
                    .FirstOrDefaultAsync(x =>
                        x.IdEmpresa == dto.IdEmpresa
                        && x.Categoria == "AJUSTE"
                        && x.Motivo == dto.Motivo
                        && x.Monto == dto.Monto);

                return ultimo?.IdMovimientoFinanciero ?? 0;
            }

            var mov = await _repository.GetByExpresionAsync(
                x => x.IdEmpresa == dto.IdEmpresa && x.ClaveIdempotencia == dto.ClaveIdempotencia);

            return mov?.IdMovimientoFinanciero ?? 0;
        }

        public async Task AnularMovimientoAsync(AnularMovimientoDto dto)
        {
            var original = await _repository.GetByIdAsync(dto.IdMovimientoFinanciero)
                ?? throw new InvalidOperationException("Movimiento no encontrado.");

            if (original.IdEmpresa != dto.IdEmpresa)
                throw new InvalidOperationException("El movimiento no pertenece a la empresa indicada.");

            if (string.Equals(original.Estado, "ANULADO", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("El movimiento ya está anulado.");

            if (string.IsNullOrWhiteSpace(dto.Motivo))
                throw new InvalidOperationException("El motivo de anulación es obligatorio.");

            original.Estado = "ANULADO";
            original.EstadoConciliacion = "REVERSADO";
            _repository.Update(original.IdMovimientoFinanciero, original);

            var claveReversa = $"ANUL_{original.IdMovimientoFinanciero}";
            var tipo = (original.TipoMovimiento ?? string.Empty).Trim().ToUpperInvariant();
            MovimientoFinanciero? reverso = null;

            switch (tipo)
            {
                case "ENTRADA":
                    reverso = await RegistrarSalidaInternoAsync(
                        dto.IdEmpresa,
                        dto.IdUsuario,
                        original.IdCuentaDestino!.Value,
                        original.Monto,
                        $"Anulación: {dto.Motivo}",
                        original.Observacion,
                        original.Categoria,
                        original.IdMovimientoFinanciero,
                        "ANULACION",
                        claveReversa);
                    break;

                case "SALIDA":
                    reverso = await RegistrarEntradaInternoAsync(
                        dto.IdEmpresa,
                        dto.IdUsuario,
                        original.IdCuentaOrigen!.Value,
                        original.Monto,
                        $"Anulación: {dto.Motivo}",
                        original.Observacion,
                        original.Categoria,
                        original.IdMovimientoFinanciero,
                        "ANULACION",
                        claveReversa);
                    break;

                case "TRANSFERENCIA":
                    reverso = await RegistrarTransferenciaInternoAsync(
                        dto.IdEmpresa,
                        dto.IdUsuario,
                        original.IdCuentaDestino!.Value,
                        original.IdCuentaOrigen!.Value,
                        original.Monto,
                        $"Anulación transferencia: {dto.Motivo}",
                        original.Observacion,
                        claveReversa);
                    break;

                default:
                    throw new InvalidOperationException($"Tipo de movimiento no soportado para anulación: {tipo}");
            }

            if (reverso != null)
            {
                reverso.IdMovimientoPar = original.IdMovimientoFinanciero;
                _repository.Update(reverso.IdMovimientoFinanciero, reverso);
            }

            await PublicarReversaContabilidadSiAplicaAsync(original, dto);
        }

        private async Task<MovimientoFinanciero?> RegistrarEntradaInternoAsync(
            int idEmpresa,
            int idUsuario,
            int idCuentaDestino,
            decimal monto,
            string motivo,
            string? observacion,
            string? categoria,
            int? referenciaId,
            string? referenciaTipo,
            string? claveIdempotencia)
        {
            if (await ExisteIdempotenciaAsync(idEmpresa, claveIdempotencia))
                return await _repository.GetByExpresionAsync(
                    x => x.IdEmpresa == idEmpresa && x.ClaveIdempotencia == claveIdempotencia);

            var cuenta = await _cuentaRepository.GetByIdAsync(idCuentaDestino)
                ?? throw new Exception("La cuenta financiera no existe.");

            var balanceAnterior = cuenta.SaldoDisponible;
            cuenta.SaldoDisponible += monto;
            await _cuentaRepository.UpdateAsync(cuenta);

            var movimiento = new MovimientoFinanciero
            {
                IdEmpresa = idEmpresa,
                IdUsuario = idUsuario,
                IdCuentaDestino = idCuentaDestino,
                TipoMovimiento = "ENTRADA",
                Categoria = categoria,
                ReferenciaId = referenciaId,
                ReferenciaTipo = referenciaTipo,
                Monto = monto,
                Motivo = motivo,
                Observacion = observacion,
                BalanceAnteriorDestino = balanceAnterior,
                BalanceNuevoDestino = cuenta.SaldoDisponible,
                ClaveIdempotencia = claveIdempotencia,
                Estado = "CONFIRMADO",
                EstadoConciliacion = "PENDIENTE",
                FechaMovimiento = DateTime.Now,
                FechaRegistro = DateTime.Now
            };

            await _repository.Save(movimiento);
            return movimiento;
        }

        private async Task<MovimientoFinanciero?> RegistrarSalidaInternoAsync(
            int idEmpresa,
            int idUsuario,
            int idCuentaOrigen,
            decimal monto,
            string motivo,
            string? observacion,
            string? categoria,
            int? referenciaId,
            string? referenciaTipo,
            string? claveIdempotencia)
        {
            if (await ExisteIdempotenciaAsync(idEmpresa, claveIdempotencia))
                return await _repository.GetByExpresionAsync(
                    x => x.IdEmpresa == idEmpresa && x.ClaveIdempotencia == claveIdempotencia);

            var cuenta = await _cuentaRepository.GetByIdAsync(idCuentaOrigen)
                ?? throw new Exception("Cuenta no encontrada");

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
                Categoria = categoria,
                ReferenciaId = referenciaId,
                ReferenciaTipo = referenciaTipo,
                Monto = monto,
                Motivo = motivo,
                Observacion = observacion,
                BalanceAnteriorOrigen = balanceAnterior,
                BalanceNuevoOrigen = cuenta.SaldoDisponible,
                ClaveIdempotencia = claveIdempotencia,
                Estado = "CONFIRMADO",
                EstadoConciliacion = "PENDIENTE",
                FechaMovimiento = DateTime.Now,
                FechaRegistro = DateTime.Now
            };

            await _repository.Save(movimiento);
            return movimiento;
        }

        private async Task<MovimientoFinanciero?> RegistrarTransferenciaInternoAsync(
            int idEmpresa,
            int idUsuario,
            int idCuentaOrigen,
            int idCuentaDestino,
            decimal monto,
            string motivo,
            string? observacion,
            string? claveIdempotencia)
        {
            if (await ExisteIdempotenciaAsync(idEmpresa, claveIdempotencia))
                return await _repository.GetByExpresionAsync(
                    x => x.IdEmpresa == idEmpresa && x.ClaveIdempotencia == claveIdempotencia);

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

            var movimiento = new MovimientoFinanciero
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
                EstadoConciliacion = "PENDIENTE",
                FechaMovimiento = DateTime.Now,
                FechaRegistro = DateTime.Now
            };

            await _repository.Save(movimiento);
            return movimiento;
        }

        private async Task PublicarAjusteTesoreriaSiAplicaAsync(
            MovimientoFinanciero movimiento,
            string? tipoCuenta,
            int? idCuentaDestino,
            int? idCuentaOrigen)
        {
            var categoria = (movimiento.Categoria ?? string.Empty).Trim().ToUpperInvariant();

            if (CategoriasSinEventoBancario.Contains(categoria))
                return;

            if (!string.IsNullOrEmpty(categoria) && !CategoriasEventoBancario.Contains(categoria))
                return;

            await _contabilidadEvents.TryPublishAsync(new MovimientoBancarioRegistradoEvent
            {
                IdEmpresa = movimiento.IdEmpresa,
                IdUsuario = movimiento.IdUsuario,
                Fecha = movimiento.FechaMovimiento == default ? DateTime.Now : movimiento.FechaMovimiento,
                ReferenciaId = movimiento.IdMovimientoFinanciero,
                ReferenciaTipo = movimiento.TipoMovimiento,
                TipoMovimiento = movimiento.TipoMovimiento,
                Categoria = string.IsNullOrWhiteSpace(categoria) ? "AJUSTE" : categoria,
                Monto = movimiento.Monto,
                IdCuentaOrigen = idCuentaOrigen,
                IdCuentaDestino = idCuentaDestino,
                TipoCuentaOrigen = tipoCuenta,
                TipoCuentaDestino = tipoCuenta,
                Motivo = movimiento.Motivo
            });
        }

        private async Task PublicarReversaContabilidadSiAplicaAsync(
            MovimientoFinanciero original,
            AnularMovimientoDto dto)
        {
            var categoria = (original.Categoria ?? string.Empty).Trim().ToUpperInvariant();
            if (CategoriasSinEventoBancario.Contains(categoria))
                return;

            if (string.Equals(original.TipoMovimiento, "TRANSFERENCIA", StringComparison.OrdinalIgnoreCase))
            {
                var cuentaOrigen = await _cuentaRepository.GetByIdAsync(original.IdCuentaOrigen!.Value);
                var cuentaDestino = await _cuentaRepository.GetByIdAsync(original.IdCuentaDestino!.Value);

                await _contabilidadEvents.TryPublishAsync(new MovimientoBancarioRegistradoEvent
                {
                    IdEmpresa = dto.IdEmpresa,
                    IdUsuario = dto.IdUsuario,
                    Fecha = DateTime.Now,
                    ReferenciaId = original.IdMovimientoFinanciero,
                    ReferenciaTipo = "AnulacionTransferencia",
                    TipoMovimiento = "TRANSFERENCIA",
                    Categoria = "TRANSFERENCIA",
                    Monto = original.Monto,
                    IdCuentaOrigen = original.IdCuentaDestino,
                    IdCuentaDestino = original.IdCuentaOrigen,
                    TipoCuentaOrigen = cuentaDestino?.TipoCuenta,
                    TipoCuentaDestino = cuentaOrigen?.TipoCuenta,
                    Motivo = $"Reversa anulación: {dto.Motivo}"
                });
                return;
            }

            var tipoReversa = string.Equals(original.TipoMovimiento, "ENTRADA", StringComparison.OrdinalIgnoreCase)
                ? "SALIDA"
                : "ENTRADA";

            var idCuenta = original.IdCuentaDestino ?? original.IdCuentaOrigen;
            var cuenta = idCuenta.HasValue
                ? await _cuentaRepository.GetByIdAsync(idCuenta.Value)
                : null;

            if (!CategoriasEventoBancario.Contains(categoria))
                return;

            await _contabilidadEvents.TryPublishAsync(new MovimientoBancarioRegistradoEvent
            {
                IdEmpresa = dto.IdEmpresa,
                IdUsuario = dto.IdUsuario,
                Fecha = DateTime.Now,
                ReferenciaId = original.IdMovimientoFinanciero,
                ReferenciaTipo = "Anulacion",
                TipoMovimiento = tipoReversa,
                Categoria = categoria,
                Monto = original.Monto,
                IdCuentaOrigen = tipoReversa == "SALIDA" ? idCuenta : null,
                IdCuentaDestino = tipoReversa == "ENTRADA" ? idCuenta : null,
                TipoCuentaOrigen = cuenta?.TipoCuenta,
                TipoCuentaDestino = cuenta?.TipoCuenta,
                Motivo = $"Reversa anulación: {dto.Motivo}"
            });
        }

        private async Task<IEnumerable<MovimientoFinancieroListadoDto>> ConstruirListadoConSaldoAsync(
            int idCuentaFinanciera,
            int idEmpresa,
            DateTime? desde,
            DateTime? hasta,
            string? tipoMovimiento = null,
            string? categoria = null,
            string? estado = null,
            string? documentoReferencia = null,
            string? estadoConciliacion = null)
        {
            var query = _context.MovimientoFinanciero
                .AsNoTracking()
                .Where(x =>
                    x.IdEmpresa == idEmpresa
                    && (x.IdCuentaOrigen == idCuentaFinanciera || x.IdCuentaDestino == idCuentaFinanciera));

            if (desde.HasValue)
                query = query.Where(x => x.FechaMovimiento.Date >= desde.Value.Date);

            if (hasta.HasValue)
                query = query.Where(x => x.FechaMovimiento.Date <= hasta.Value.Date);

            if (!string.IsNullOrWhiteSpace(tipoMovimiento))
            {
                var tipo = tipoMovimiento.Trim().ToUpperInvariant();
                query = query.Where(x => x.TipoMovimiento == tipo);
            }

            if (!string.IsNullOrWhiteSpace(categoria))
            {
                var cat = categoria.Trim().ToUpperInvariant();
                query = query.Where(x => x.Categoria != null && x.Categoria.ToUpper() == cat);
            }

            if (!string.IsNullOrWhiteSpace(estado))
            {
                var est = estado.Trim().ToUpperInvariant();
                query = query.Where(x => x.Estado == est);
            }
            else
            {
                query = query.Where(x => x.Estado != "ANULADO");
            }

            if (!string.IsNullOrWhiteSpace(estadoConciliacion))
            {
                var estConc = estadoConciliacion.Trim().ToUpperInvariant();
                query = query.Where(x => x.EstadoConciliacion == estConc);
            }

            if (!string.IsNullOrWhiteSpace(documentoReferencia))
            {
                var doc = documentoReferencia.Trim();
                query = query.Where(x =>
                    (x.NumeroComprobante != null && x.NumeroComprobante.Contains(doc))
                    || (x.Motivo != null && x.Motivo.Contains(doc))
                    || (x.ReferenciaTipo != null && x.ReferenciaTipo.Contains(doc))
                    || (x.ReferenciaId != null && x.ReferenciaId.ToString() == doc));
            }

            var movimientos = await query
                .OrderBy(x => x.FechaMovimiento)
                .ThenBy(x => x.IdMovimientoFinanciero)
                .ToListAsync();

            var saldoBase = await CalcularSaldoHastaAsync(idCuentaFinanciera, desde?.Date.AddDays(-1));
            decimal saldo = saldoBase;

            var resultado = new List<MovimientoFinancieroListadoDto>();
            foreach (var mov in movimientos)
            {
                if (string.Equals(mov.Estado, "ANULADO", StringComparison.OrdinalIgnoreCase))
                {
                    resultado.Add(MapListado(mov, saldo));
                    continue;
                }

                saldo += CalcularImpactoEnCuenta(mov, idCuentaFinanciera);
                resultado.Add(MapListado(mov, saldo));
            }

            return resultado;
        }

        private async Task<decimal> CalcularSaldoHastaAsync(int idCuentaFinanciera, DateTime? fechaHasta)
        {
            var cuenta = await _cuentaRepository.GetByIdAsync(idCuentaFinanciera);
            if (cuenta == null)
                return 0;

            var saldo = cuenta.BalanceInicial;

            if (cuenta.FechaSaldoInicial.HasValue
                && (!fechaHasta.HasValue || fechaHasta.Value.Date >= cuenta.FechaSaldoInicial.Value.Date))
            {
                if (fechaHasta.HasValue && fechaHasta.Value.Date < cuenta.FechaSaldoInicial.Value.Date)
                    return saldo;
            }

            var query = _context.MovimientoFinanciero
                .AsNoTracking()
                .Where(x =>
                    x.Estado == "CONFIRMADO"
                    && (x.IdCuentaOrigen == idCuentaFinanciera || x.IdCuentaDestino == idCuentaFinanciera));

            if (fechaHasta.HasValue)
                query = query.Where(x => x.FechaMovimiento.Date <= fechaHasta.Value.Date);

            if (cuenta.FechaSaldoInicial.HasValue)
                query = query.Where(x => x.FechaMovimiento.Date >= cuenta.FechaSaldoInicial.Value.Date);

            var movimientos = await query
                .OrderBy(x => x.FechaMovimiento)
                .ThenBy(x => x.IdMovimientoFinanciero)
                .ToListAsync();

            foreach (var mov in movimientos)
                saldo += CalcularImpactoEnCuenta(mov, idCuentaFinanciera);

            return saldo;
        }

        private static decimal CalcularImpactoEnCuenta(MovimientoFinanciero mov, int idCuentaFinanciera)
        {
            if (mov.IdCuentaDestino == idCuentaFinanciera)
                return mov.Monto;

            if (mov.IdCuentaOrigen == idCuentaFinanciera)
                return -mov.Monto;

            return 0;
        }

        private static MovimientoFinancieroListadoDto MapListado(MovimientoFinanciero mov, decimal saldoAcumulado)
        {
            return new MovimientoFinancieroListadoDto
            {
                IdMovimientoFinanciero = mov.IdMovimientoFinanciero,
                IdEmpresa = mov.IdEmpresa,
                IdUsuario = mov.IdUsuario,
                IdCuentaOrigen = mov.IdCuentaOrigen,
                IdCuentaDestino = mov.IdCuentaDestino,
                TipoMovimiento = mov.TipoMovimiento,
                Categoria = mov.Categoria,
                ReferenciaId = mov.ReferenciaId,
                ReferenciaTipo = mov.ReferenciaTipo,
                Monto = mov.Monto,
                Motivo = mov.Motivo,
                Observacion = mov.Observacion,
                FechaMovimiento = mov.FechaMovimiento,
                FechaRegistro = mov.FechaRegistro,
                Estado = mov.Estado,
                EstadoConciliacion = mov.EstadoConciliacion,
                IdTesoreriaConciliacion = mov.IdTesoreriaConciliacion,
                FechaConciliacion = mov.FechaConciliacion,
                NumeroComprobante = mov.NumeroComprobante,
                ClaveIdempotencia = mov.ClaveIdempotencia,
                SaldoAcumulado = saldoAcumulado
            };
        }

        private static void ValidarMovimientoManual(CuentaFinanciera cuenta, string? categoria)
        {
            var cat = (categoria ?? "AJUSTE").Trim().ToUpperInvariant();
            if (cat is not ("AJUSTE" or "MANUAL"))
                return;

            if (!cuenta.PermiteMovimientosManuales)
                throw new InvalidOperationException("Esta cuenta no permite movimientos manuales.");
        }

        private async Task<bool> ExisteIdempotenciaAsync(int idEmpresa, string? claveIdempotencia)
        {
            if (string.IsNullOrWhiteSpace(claveIdempotencia))
                return false;

            var existente = await _repository.GetByExpresionAsync(
                x => x.IdEmpresa == idEmpresa && x.ClaveIdempotencia == claveIdempotencia);

            return existente != null;
        }
    }
}
