using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    public class ContabilidadIntegracionService : IContabilidadIntegracionService
    {
        private readonly AlahiaPosContext _context;
        private readonly IAsientoContableService _asientoService;
        private readonly IContabilidadGatekeeper _gatekeeper;

        public ContabilidadIntegracionService(
            AlahiaPosContext context,
            IAsientoContableService asientoService,
            IContabilidadGatekeeper gatekeeper)
        {
            _context = context;
            _asientoService = asientoService;
            _gatekeeper = gatekeeper;
        }

        public async Task<int> RegistrarAsientoAutomaticoAsync(ContabilidadIntegracionRequest request)
        {
            if (!await _gatekeeper.IntegracionAutomaticaActivaAsync(request.IdEmpresa))
                throw new InvalidOperationException(
                    "La integración automática no está activa para esta empresa.");

            var existente = await BuscarAsientoActivoAsync(
                request.IdEmpresa,
                request.OrigenModulo,
                request.OrigenReferenciaId,
                request.TipoOperacion);

            if (existente != null)
                return existente.IdAsientoContable;

            var asiento = new AsientoContable
            {
                IdEmpresa = request.IdEmpresa,
                Fecha = request.Fecha,
                Concepto = request.Concepto,
                Estado = ContabilidadConstantes.EstadoAsientoConfirmado,
                IdUsuario = request.IdUsuario,
                OrigenModulo = request.OrigenModulo,
                OrigenReferenciaId = request.OrigenReferenciaId,
                EsAutomatico = true,
                TipoOperacion = request.TipoOperacion,
                IdAsientoContableOrigen = request.IdAsientoContableOrigen,
                Detalles = request.Lineas.Select(l => new AsientoContableDetalle
                {
                    IdCuentaContable = l.IdCuentaContable,
                    Debito = l.Debito,
                    Credito = l.Credito,
                    Referencia = l.Referencia
                }).ToList()
            };

            return await _asientoService.CreateAutomaticoAsync(asiento);
        }

        public async Task<int> RevertirAsientoAutomaticoAsync(
            int idEmpresa,
            string origenModulo,
            int origenReferenciaId,
            string tipoOperacion,
            int idUsuario,
            string? motivo = null)
        {
            if (!await _gatekeeper.IntegracionAutomaticaActivaAsync(idEmpresa))
                throw new InvalidOperationException(
                    "La integración automática no está activa para esta empresa.");

            var tipoReverso = TipoReversoDe(tipoOperacion);

            var reversoExistente = await BuscarAsientoActivoAsync(
                idEmpresa,
                origenModulo,
                origenReferenciaId,
                tipoReverso);

            if (reversoExistente != null)
                return reversoExistente.IdAsientoContable;

            var original = await BuscarAsientoActivoAsync(
                idEmpresa,
                origenModulo,
                origenReferenciaId,
                tipoOperacion);

            if (original == null)
                throw new InvalidOperationException("No existe asiento original para revertir.");

            var detalles = await _context.AsientosContablesDetalle
                .AsNoTracking()
                .Where(d => d.IdAsientoContable == original.IdAsientoContable)
                .ToListAsync();

            if (detalles.Count == 0)
                throw new InvalidOperationException("El asiento original no tiene líneas.");

            var lineasReverso = detalles.Select(d => new ContabilidadIntegracionLinea
            {
                IdCuentaContable = d.IdCuentaContable,
                Debito = d.Credito,
                Credito = d.Debito,
                Referencia = d.Referencia
            }).ToList();

            var request = new ContabilidadIntegracionRequest
            {
                IdEmpresa = idEmpresa,
                IdUsuario = idUsuario,
                Fecha = DateTime.Now,
                Concepto = $"Reverso: {original.Concepto}" +
                    (string.IsNullOrWhiteSpace(motivo) ? "" : $" — {motivo}"),
                OrigenModulo = origenModulo,
                OrigenReferenciaId = origenReferenciaId,
                TipoOperacion = tipoReverso,
                IdAsientoContableOrigen = original.IdAsientoContable,
                Lineas = lineasReverso
            };

            return await RegistrarAsientoAutomaticoAsync(request);
        }

        public async Task<IReadOnlyList<int>> RevertirAsientosOrigenAsync(
            int idEmpresa,
            string origenModulo,
            int origenReferenciaId,
            int idUsuario,
            string? motivo = null)
        {
            if (!await _gatekeeper.IntegracionAutomaticaActivaAsync(idEmpresa))
                throw new InvalidOperationException(
                    "La integración automática no está activa para esta empresa.");

            var originales = await _context.AsientosContables
                .AsNoTracking()
                .Where(a =>
                    a.IdEmpresa == idEmpresa &&
                    a.OrigenModulo == origenModulo &&
                    a.OrigenReferenciaId == origenReferenciaId &&
                    a.EsAutomatico &&
                    a.Estado != ContabilidadConstantes.EstadoAsientoAnulado &&
                    a.TipoOperacion != null &&
                    !a.TipoOperacion.StartsWith("REVERSO"))
                .OrderBy(a => a.IdAsientoContable)
                .ToListAsync();

            if (originales.Count == 0)
                throw new InvalidOperationException("No existe asiento original para revertir.");

            var ids = new List<int>();
            foreach (var original in originales)
            {
                var id = await RevertirAsientoAutomaticoAsync(
                    idEmpresa,
                    origenModulo,
                    origenReferenciaId,
                    original.TipoOperacion!,
                    idUsuario,
                    motivo);
                ids.Add(id);
            }

            return ids;
        }

        private async Task<AsientoContable?> BuscarAsientoActivoAsync(
            int idEmpresa,
            string origenModulo,
            int origenReferenciaId,
            string tipoOperacion)
        {
            return await _context.AsientosContables
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.IdEmpresa == idEmpresa &&
                    a.OrigenModulo == origenModulo &&
                    a.OrigenReferenciaId == origenReferenciaId &&
                    a.TipoOperacion == tipoOperacion &&
                    a.EsAutomatico &&
                    a.Estado != ContabilidadConstantes.EstadoAsientoAnulado);
        }

        private static string TipoReversoDe(string tipoOperacion)
        {
            if (string.IsNullOrWhiteSpace(tipoOperacion)
                || string.Equals(tipoOperacion, ContabilidadTipoOperacion.Alta, StringComparison.OrdinalIgnoreCase))
                return ContabilidadTipoOperacion.Reverso;

            if (tipoOperacion.StartsWith("REVERSO", StringComparison.OrdinalIgnoreCase))
                return tipoOperacion;

            return $"REVERSO_{tipoOperacion}";
        }
    }
}
