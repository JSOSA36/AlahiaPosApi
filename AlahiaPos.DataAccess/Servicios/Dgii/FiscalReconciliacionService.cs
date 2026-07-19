using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios.Dgii
{
    public class FiscalReconciliacionService : IFiscalReconciliacionService
    {
        private readonly AlahiaPosContext _context;
        private readonly IFiscalFeatureService _features;

        public FiscalReconciliacionService(AlahiaPosContext context, IFiscalFeatureService features)
        {
            _context = context;
            _features = features;
        }

        public async Task<IReadOnlyList<FiscalReconciliacionItemDto>> ListarPendientesAsync(int idEmpresa, int top = 100)
        {
            var features = await _features.GetFeaturesAsync(idEmpresa);
            if (!features.FiscalActivo)
                return Array.Empty<FiscalReconciliacionItemDto>();

            top = Math.Clamp(top, 1, 500);
            var estados = new[]
            {
                EstadoFiscalDocumentoConstantes.PendienteGenerar,
                EstadoFiscalDocumentoConstantes.ErrorFiscal
            };

            var ventas = await _context.FacturaHeaders.AsNoTracking()
                .Where(h => h.IdEmpresa == idEmpresa && h.EstadoFiscalDocumento != null && estados.Contains(h.EstadoFiscalDocumento))
                .OrderByDescending(h => h.IdFacturaHeader)
                .Take(top)
                .Select(h => new FiscalReconciliacionItemDto
                {
                    TipoDocumento = "Venta",
                    ReferenciaId = h.IdFacturaHeader,
                    IdEmpresa = h.IdEmpresa,
                    EstadoFiscalDocumento = h.EstadoFiscalDocumento!,
                    Fecha = h.FechaInseccion,
                    NumeroDocumento = h.NumeroDocumento
                })
                .ToListAsync();

            var ncs = await _context.NotasCredito.AsNoTracking()
                .Where(h => h.IdEmpresa == idEmpresa && h.EstadoFiscalDocumento != null && estados.Contains(h.EstadoFiscalDocumento))
                .OrderByDescending(h => h.IdNotaCredito)
                .Take(top)
                .Select(h => new FiscalReconciliacionItemDto
                {
                    TipoDocumento = "NotaCredito",
                    ReferenciaId = h.IdNotaCredito,
                    IdEmpresa = h.IdEmpresa,
                    EstadoFiscalDocumento = h.EstadoFiscalDocumento!,
                    Fecha = null,
                    NumeroDocumento = h.NumeroDocumento
                })
                .ToListAsync();

            var compras = await _context.OrdenCompraHeaders.AsNoTracking()
                .Where(h => h.IdEmpresa == idEmpresa && h.EstadoFiscalDocumento != null && estados.Contains(h.EstadoFiscalDocumento))
                .OrderByDescending(h => h.IdOrdenCompraHeader)
                .Take(top)
                .Select(h => new FiscalReconciliacionItemDto
                {
                    TipoDocumento = "Compra",
                    ReferenciaId = h.IdOrdenCompraHeader,
                    IdEmpresa = h.IdEmpresa,
                    EstadoFiscalDocumento = h.EstadoFiscalDocumento!,
                    Fecha = h.FechaInseccion,
                    NumeroDocumento = h.NumeroDocumento
                })
                .ToListAsync();

            return ventas.Concat(ncs).Concat(compras)
                .Take(top)
                .ToList();
        }
    }
}
