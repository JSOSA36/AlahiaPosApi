using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    public class AsientoContableService : IAsientoContableService
    {
        private readonly AlahiaPosContext _context;
        private readonly IRepository<CuentaContable> _cuentaRepository;

        public AsientoContableService(
            AlahiaPosContext context,
            IRepository<CuentaContable> cuentaRepository)
        {
            _context = context;
            _cuentaRepository = cuentaRepository;
        }

        public async Task<IEnumerable<AsientoContable>> GetByEmpresaAsync(
            int idEmpresa,
            DateTime? desde = null,
            DateTime? hasta = null)
        {
            var query = _context.AsientosContables
                .Where(a => a.IdEmpresa == idEmpresa);

            if (desde.HasValue)
                query = query.Where(a => a.Fecha >= desde.Value.Date);

            if (hasta.HasValue)
                query = query.Where(a => a.Fecha <= hasta.Value.Date.AddDays(1).AddTicks(-1));

            return await query
                .OrderByDescending(a => a.Fecha)
                .ThenByDescending(a => a.IdAsientoContable)
                .ToListAsync();
        }

        public async Task<IEnumerable<AsientoContable>> ConsultarAsync(
            int idEmpresa,
            DateTime? desde = null,
            DateTime? hasta = null,
            int? idCuentaContable = null,
            string? numero = null,
            string? concepto = null)
        {
            var query = _context.AsientosContables
                .Where(a => a.IdEmpresa == idEmpresa);

            if (desde.HasValue)
                query = query.Where(a => a.Fecha >= desde.Value.Date);

            if (hasta.HasValue)
                query = query.Where(a => a.Fecha <= hasta.Value.Date.AddDays(1).AddTicks(-1));

            if (!string.IsNullOrWhiteSpace(numero))
                query = query.Where(a => a.Numero.Contains(numero.Trim()));

            if (!string.IsNullOrWhiteSpace(concepto))
                query = query.Where(a => a.Concepto.Contains(concepto.Trim()));

            if (idCuentaContable.HasValue)
            {
                var idsAsientos = _context.AsientosContablesDetalle
                    .Where(d => d.IdCuentaContable == idCuentaContable.Value)
                    .Select(d => d.IdAsientoContable);

                query = query.Where(a => idsAsientos.Contains(a.IdAsientoContable));
            }

            return await query
                .OrderByDescending(a => a.Fecha)
                .ThenByDescending(a => a.IdAsientoContable)
                .ToListAsync();
        }

        public async Task<AsientoContable?> GetByIdAsync(int id, int idEmpresa)
        {
            var asiento = await _context.AsientosContables
                .FirstOrDefaultAsync(a => a.IdAsientoContable == id && a.IdEmpresa == idEmpresa);

            if (asiento == null)
                return null;

            asiento.Detalles = await _context.AsientosContablesDetalle
                .Where(d => d.IdAsientoContable == id)
                .ToListAsync();

            var idsCuentas = asiento.Detalles.Select(d => d.IdCuentaContable).Distinct().ToList();
            var cuentas = await _context.CuentasContables
                .Where(c => idsCuentas.Contains(c.IdCuentaContable))
                .ToListAsync();

            foreach (var detalle in asiento.Detalles)
            {
                var cuenta = cuentas.FirstOrDefault(c => c.IdCuentaContable == detalle.IdCuentaContable);
                if (cuenta != null)
                {
                    detalle.CodigoCuenta = cuenta.Codigo;
                    detalle.NombreCuenta = cuenta.Nombre;
                }
            }

            return asiento;
        }

        public async Task<int> CreateAsync(AsientoContable asiento)
        {
            await ValidarAsientoAsync(asiento, esNuevo: true);

            asiento.Numero = await GenerarNumeroAsync(asiento.IdEmpresa, asiento.Fecha);
            asiento.FechaInseccion = DateTime.Now;
            asiento.EsAutomatico = false;
            asiento.OrigenModulo = ContabilidadConstantes.OrigenManual;

            if (string.IsNullOrWhiteSpace(asiento.Estado))
                asiento.Estado = ContabilidadConstantes.EstadoAsientoConfirmado;

            var detalles = asiento.Detalles ?? new List<AsientoContableDetalle>();
            asiento.Detalles = null;

            _context.AsientosContables.Add(asiento);
            await _context.SaveChangesAsync();

            foreach (var detalle in detalles)
            {
                detalle.IdAsientoContable = asiento.IdAsientoContable;
                _context.AsientosContablesDetalle.Add(detalle);
            }

            await _context.SaveChangesAsync();
            return asiento.IdAsientoContable;
        }

        public async Task<int> CreateAutomaticoAsync(AsientoContable asiento)
        {
            await ValidarAsientoAsync(asiento, esNuevo: true);

            asiento.Numero = await GenerarNumeroAsync(asiento.IdEmpresa, asiento.Fecha);
            asiento.FechaInseccion = DateTime.Now;
            asiento.EsAutomatico = true;

            if (string.IsNullOrWhiteSpace(asiento.TipoOperacion))
                asiento.TipoOperacion = ContabilidadTipoOperacion.Alta;

            if (string.IsNullOrWhiteSpace(asiento.Estado))
                asiento.Estado = ContabilidadConstantes.EstadoAsientoConfirmado;

            var detalles = asiento.Detalles ?? new List<AsientoContableDetalle>();
            asiento.Detalles = null;

            _context.AsientosContables.Add(asiento);
            await _context.SaveChangesAsync();

            foreach (var detalle in detalles)
            {
                detalle.IdAsientoContable = asiento.IdAsientoContable;
                _context.AsientosContablesDetalle.Add(detalle);
            }

            await _context.SaveChangesAsync();
            return asiento.IdAsientoContable;
        }

        public async Task UpdateAsync(AsientoContable asiento)
        {
            var existente = await GetByIdAsync(asiento.IdAsientoContable, asiento.IdEmpresa);
            if (existente == null)
                throw new InvalidOperationException("Asiento contable no encontrado.");

            if (existente.Estado == ContabilidadConstantes.EstadoAsientoAnulado)
                throw new InvalidOperationException("No se puede modificar un asiento anulado.");

            if (existente.EsAutomatico)
                throw new InvalidOperationException("Los asientos automáticos no se modifican manualmente.");

            await ValidarAsientoAsync(asiento, esNuevo: false);

            existente.Fecha = asiento.Fecha;
            existente.Concepto = asiento.Concepto;
            existente.Estado = asiento.Estado;
            existente.IdUsuario = asiento.IdUsuario;

            _context.AsientosContables.Update(existente);

            var detallesActuales = await _context.AsientosContablesDetalle
                .Where(d => d.IdAsientoContable == asiento.IdAsientoContable)
                .ToListAsync();

            _context.AsientosContablesDetalle.RemoveRange(detallesActuales);

            foreach (var detalle in asiento.Detalles ?? new List<AsientoContableDetalle>())
            {
                detalle.IdAsientoContable = asiento.IdAsientoContable;
                detalle.IdAsientoContableDetalle = 0;
                _context.AsientosContablesDetalle.Add(detalle);
            }

            await _context.SaveChangesAsync();
        }

        public async Task AnularAsync(int id, int idEmpresa)
        {
            var asiento = await _context.AsientosContables
                .FirstOrDefaultAsync(a => a.IdAsientoContable == id && a.IdEmpresa == idEmpresa);

            if (asiento == null)
                throw new InvalidOperationException("Asiento contable no encontrado.");

            if (asiento.Estado == ContabilidadConstantes.EstadoAsientoAnulado)
                return;

            asiento.Estado = ContabilidadConstantes.EstadoAsientoAnulado;
            _context.AsientosContables.Update(asiento);
            await _context.SaveChangesAsync();
        }

        private async Task ValidarAsientoAsync(AsientoContable asiento, bool esNuevo)
        {
            if (string.IsNullOrWhiteSpace(asiento.Concepto))
                throw new InvalidOperationException("El concepto del asiento es obligatorio.");

            var periodoCerrado = await _context.PeriodosContables.AsNoTracking()
                .AnyAsync(p =>
                    p.IdEmpresa == asiento.IdEmpresa
                    && p.Anio == asiento.Fecha.Year
                    && p.Mes == asiento.Fecha.Month
                    && p.Estado == ContabilidadConstantes.PeriodoCerrado);

            if (periodoCerrado)
                throw new InvalidOperationException(
                    $"El período contable {asiento.Fecha:yyyy-MM} está cerrado. " +
                    "Use una fecha en un período abierto o reabra el período.");

            var detalles = asiento.Detalles ?? new List<AsientoContableDetalle>();
            if (detalles.Count < 2)
                throw new InvalidOperationException("El asiento debe tener al menos dos líneas.");

            decimal totalDebito = 0;
            decimal totalCredito = 0;

            foreach (var linea in detalles)
            {
                if (linea.Debito < 0 || linea.Credito < 0)
                    throw new InvalidOperationException("Los montos no pueden ser negativos.");

                if (linea.Debito > 0 && linea.Credito > 0)
                    throw new InvalidOperationException("Una línea no puede tener débito y crédito al mismo tiempo.");

                if (linea.Debito == 0 && linea.Credito == 0)
                    throw new InvalidOperationException("Cada línea debe tener débito o crédito.");

                var cuenta = await _cuentaRepository.GetByIdAsync(linea.IdCuentaContable);
                if (cuenta == null || cuenta.IdEmpresa != asiento.IdEmpresa)
                    throw new InvalidOperationException("Cuenta contable no válida para esta empresa.");

                if (!cuenta.Activa)
                    throw new InvalidOperationException($"La cuenta {cuenta.Codigo} está inactiva.");

                if (!cuenta.PermiteMovimiento)
                    throw new InvalidOperationException($"La cuenta {cuenta.Codigo} no permite movimientos.");

                totalDebito += linea.Debito;
                totalCredito += linea.Credito;
            }

            if (totalDebito != totalCredito)
                throw new InvalidOperationException("El asiento no está balanceado. Total débito debe ser igual al total crédito.");

            if (totalDebito == 0)
                throw new InvalidOperationException("El asiento debe tener montos mayores a cero.");
        }

        private async Task<string> GenerarNumeroAsync(int idEmpresa, DateTime fecha)
        {
            var year = fecha.Year;
            var inicio = new DateTime(year, 1, 1);
            var fin = new DateTime(year, 12, 31, 23, 59, 59);

            var count = await _context.AsientosContables
                .CountAsync(a => a.IdEmpresa == idEmpresa && a.Fecha >= inicio && a.Fecha <= fin);

            return $"AS-{year}-{(count + 1):D5}";
        }
    }
}
