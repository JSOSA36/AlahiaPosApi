using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AlahiaPos.DataAccess.Servicios.Dgii
{
    public class FiscalDocumentSnapshotService : IFiscalDocumentSnapshotService
    {
        private readonly AlahiaPosContext _context;
        private readonly IFiscalFeatureService _features;
        private readonly ITaxClassificationService _tax;
        private readonly ILogger<FiscalDocumentSnapshotService> _logger;

        public FiscalDocumentSnapshotService(
            AlahiaPosContext context,
            IFiscalFeatureService features,
            ITaxClassificationService tax,
            ILogger<FiscalDocumentSnapshotService> logger)
        {
            _context = context;
            _features = features;
            _tax = tax;
            _logger = logger;
        }

        public async Task ApplyVentaAsync(int idEmpresa, int idFacturaHeader, int idUsuario, bool forceReprocess = false)
        {
            var features = await _features.GetFeaturesAsync(idEmpresa);
            if (!features.FiscalActivo || !features.FotoVentaRelevante)
                return; // no-op: cero UPDATE

            var header = await _context.FacturaHeaders
                .AsTracking()
                .FirstOrDefaultAsync(h => h.IdFacturaHeader == idFacturaHeader && h.IdEmpresa == idEmpresa);
            if (header == null) return;

            if (!forceReprocess && EsFotoValida(header.EstadoFiscalDocumento))
                return;

            var detalles = await _context.FacturaDetalles
                .AsTracking()
                .Where(d => d.IdFacturaHeader == idFacturaHeader)
                .ToListAsync();

            decimal montoGravado = 0;
            decimal montoExento = 0;
            decimal? tasaPrincipal = null;

            foreach (var d in detalles)
            {
                var baseSinItbis = Math.Max(0, d.SubTotal - d.Itbis);
                if (d.Itbis > 0)
                {
                    montoGravado += baseSinItbis;
                    d.MontoGravadoLinea = baseSinItbis;
                    d.MontoExentoLinea = 0;
                    d.ItbisCalculado = d.Itbis;
                    d.IndicadorFacturacion ??= 1;
                    if (baseSinItbis > 0)
                        d.TasaItbis ??= Math.Round(d.Itbis / baseSinItbis * 100m, 2);
                    tasaPrincipal ??= d.TasaItbis;
                }
                else
                {
                    montoExento += Math.Max(0, d.SubTotal);
                    d.MontoExentoLinea = Math.Max(0, d.SubTotal);
                    d.MontoGravadoLinea = 0;
                    d.ItbisCalculado = 0;
                    d.IndicadorFacturacion ??= 4;
                }
            }

            header.MontoGravado = montoGravado;
            header.MontoExento = montoExento;
            header.MontoGravadoI1 = montoGravado;
            header.DescuentoAfectaBase = header.TotalDescuento;
            header.TasaItbisPrincipal = tasaPrincipal;
            header.TipoIngresoDgii ??= _tax.ResolverTipoIngresoDefault();
            header.FormaVentaFiscalDgii = _tax.ResolverFormaVentaFiscal(header.TipoFactura);
            header.FotografiaFiscalVersion = Math.Max(1, header.FotografiaFiscalVersion);
            header.FechaFotografiaFiscal = DateTime.Now;
            header.EstadoFiscalDocumento = EstadoFiscalDocumentoConstantes.Generada;

            await _context.SaveChangesAsync();
            _logger.LogInformation(
                "Foto fiscal venta OK. Empresa={IdEmpresa} Factura={IdFactura}",
                idEmpresa, idFacturaHeader);
        }

        public async Task ApplyNotaCreditoAsync(int idEmpresa, int idNotaCredito, int idUsuario, bool forceReprocess = false)
        {
            var features = await _features.GetFeaturesAsync(idEmpresa);
            if (!features.FiscalActivo || !features.FotoVentaRelevante)
                return;

            var nota = await _context.NotasCredito
                .AsTracking()
                .FirstOrDefaultAsync(n => n.IdNotaCredito == idNotaCredito && n.IdEmpresa == idEmpresa);
            if (nota == null) return;

            if (!forceReprocess && EsFotoValida(nota.EstadoFiscalDocumento))
                return;

            var detalles = await _context.NotasCreditoDetalle
                .AsTracking()
                .Where(d => d.IdNotaCredito == idNotaCredito)
                .ToListAsync();

            decimal montoGravado = 0;
            decimal montoExento = 0;
            decimal? tasa = null;

            foreach (var d in detalles)
            {
                var baseLinea = Math.Max(0, d.SubTotal - d.Itbis);
                if (d.Itbis > 0)
                {
                    montoGravado += baseLinea;
                    d.MontoGravadoLinea = baseLinea;
                    d.MontoExentoLinea = 0;
                    d.ItbisCalculado = d.Itbis;
                    if (baseLinea > 0)
                        d.TasaItbis ??= Math.Round(d.Itbis / baseLinea * 100m, 2);
                    tasa ??= d.TasaItbis;
                }
                else
                {
                    montoExento += Math.Max(0, d.SubTotal);
                    d.MontoExentoLinea = Math.Max(0, d.SubTotal);
                    d.MontoGravadoLinea = 0;
                }
            }

            var factura = await _context.FacturaHeaders.AsNoTracking()
                .FirstOrDefaultAsync(f => f.IdFacturaHeader == nota.IdFacturaHeader);

            nota.MontoGravado = montoGravado;
            nota.MontoExento = montoExento;
            nota.MontoGravadoI1 = montoGravado;
            nota.TasaItbisPrincipal = tasa;
            nota.TipoIngresoDgii ??= factura?.TipoIngresoDgii ?? _tax.ResolverTipoIngresoDefault();
            nota.FechaFacturaOrigen ??= factura?.FechaInseccion;
            nota.FotografiaFiscalVersion = Math.Max(1, nota.FotografiaFiscalVersion);
            nota.EstadoFiscalDocumento = EstadoFiscalDocumentoConstantes.Generada;

            await _context.SaveChangesAsync();
        }

        public async Task ApplyCompraAsync(int idEmpresa, int idOrdenCompraHeader, int idUsuario, bool forceReprocess = false)
        {
            var features = await _features.GetFeaturesAsync(idEmpresa);
            if (!features.FiscalActivo || !features.FotoCompraRelevante)
                return;

            var header = await _context.OrdenCompraHeaders
                .AsTracking()
                .FirstOrDefaultAsync(h =>
                    h.IdOrdenCompraHeader == idOrdenCompraHeader && h.IdEmpresa == idEmpresa);
            if (header == null) return;

            if (!forceReprocess && EsFotoValida(header.EstadoFiscalDocumento))
                return;

            var detalles = await _context.OrdenCompraDetalles
                .AsNoTracking()
                .Where(d => d.IdOrdenCompraHeader == idOrdenCompraHeader)
                .ToListAsync();

            var tieneInventariable = detalles.Any(d =>
                TipoComportamientoConstantes.Normalizar(d.TipoComportamientoLinea)
                == TipoComportamientoConstantes.Inventario);

            header.TasaItbis ??= header.TotalItbis > 0 && (header.Total - header.TotalItbis) > 0
                ? Math.Round(header.TotalItbis / (header.Total - header.TotalItbis) * 100m, 2)
                : null;
            header.FotografiaFiscalVersion = Math.Max(1, header.FotografiaFiscalVersion);

            if (features.It1Activo && header.TotalItbis > 0)
            {
                if (!header.ClasificacionConfirmada)
                {
                    var sugerido = _tax.SugerirDestinoItbisCompra(tieneInventariable, !tieneInventariable);
                    header.DestinoItbisSugerido ??= sugerido;
                    header.EstadoClasificacionItbis = EstadoClasificacionItbisConstantes.PendienteValidar;
                    header.ClasificacionConfirmada = false;
                    header.EstadoFiscalDocumento = EstadoFiscalDocumentoConstantes.PendienteValidar;
                }
                else
                {
                    header.EstadoFiscalDocumento = EstadoFiscalDocumentoConstantes.Generada;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(header.EstadoClasificacionItbis)
                    || header.EstadoClasificacionItbis == EstadoClasificacionItbisConstantes.NoAplica)
                {
                    header.EstadoClasificacionItbis = EstadoClasificacionItbisConstantes.NoAplica;
                }

                header.EstadoFiscalDocumento = EstadoFiscalDocumentoConstantes.Generada;
            }

            await _context.SaveChangesAsync();
        }

        public async Task MarkPendienteGenerarAsync(int idEmpresa, string tipoDocumento, int referenciaId)
        {
            switch (Normalizar(tipoDocumento))
            {
                case "Venta":
                    var v = await _context.FacturaHeaders.AsTracking().FirstOrDefaultAsync(h =>
                        h.IdFacturaHeader == referenciaId && h.IdEmpresa == idEmpresa);
                    if (v == null || EsFotoValida(v.EstadoFiscalDocumento)) return;
                    v.EstadoFiscalDocumento = EstadoFiscalDocumentoConstantes.PendienteGenerar;
                    await _context.SaveChangesAsync();
                    break;
                case "NotaCredito":
                    var n = await _context.NotasCredito.AsTracking().FirstOrDefaultAsync(h =>
                        h.IdNotaCredito == referenciaId && h.IdEmpresa == idEmpresa);
                    if (n == null || EsFotoValida(n.EstadoFiscalDocumento)) return;
                    n.EstadoFiscalDocumento = EstadoFiscalDocumentoConstantes.PendienteGenerar;
                    await _context.SaveChangesAsync();
                    break;
                case "Compra":
                    var c = await _context.OrdenCompraHeaders.AsTracking().FirstOrDefaultAsync(h =>
                        h.IdOrdenCompraHeader == referenciaId && h.IdEmpresa == idEmpresa);
                    if (c == null || EsFotoValida(c.EstadoFiscalDocumento)) return;
                    c.EstadoFiscalDocumento = EstadoFiscalDocumentoConstantes.PendienteGenerar;
                    await _context.SaveChangesAsync();
                    break;
            }
        }

        public async Task MarkErrorAsync(int idEmpresa, string tipoDocumento, int referenciaId, string mensaje)
        {
            _logger.LogError(
                "Error fiscal. Empresa={IdEmpresa} Tipo={Tipo} Ref={Ref} Msg={Msg}",
                idEmpresa, tipoDocumento, referenciaId, mensaje);

            switch (Normalizar(tipoDocumento))
            {
                case "Venta":
                    var v = await _context.FacturaHeaders.AsTracking().FirstOrDefaultAsync(h =>
                        h.IdFacturaHeader == referenciaId && h.IdEmpresa == idEmpresa);
                    if (v != null)
                    {
                        v.EstadoFiscalDocumento = EstadoFiscalDocumentoConstantes.ErrorFiscal;
                        await _context.SaveChangesAsync();
                    }
                    break;
                case "NotaCredito":
                    var n = await _context.NotasCredito.AsTracking().FirstOrDefaultAsync(h =>
                        h.IdNotaCredito == referenciaId && h.IdEmpresa == idEmpresa);
                    if (n != null)
                    {
                        n.EstadoFiscalDocumento = EstadoFiscalDocumentoConstantes.ErrorFiscal;
                        await _context.SaveChangesAsync();
                    }
                    break;
                case "Compra":
                    var c = await _context.OrdenCompraHeaders.AsTracking().FirstOrDefaultAsync(h =>
                        h.IdOrdenCompraHeader == referenciaId && h.IdEmpresa == idEmpresa);
                    if (c != null)
                    {
                        c.EstadoFiscalDocumento = EstadoFiscalDocumentoConstantes.ErrorFiscal;
                        await _context.SaveChangesAsync();
                    }
                    break;
            }
        }

        private static bool EsFotoValida(string? estado)
            => estado == EstadoFiscalDocumentoConstantes.Generada
               || estado == EstadoFiscalDocumentoConstantes.PendienteValidar;

        private static string Normalizar(string? tipo)
        {
            if (string.IsNullOrWhiteSpace(tipo)) return "Venta";
            if (tipo.Equals("NotaCredito", StringComparison.OrdinalIgnoreCase)) return "NotaCredito";
            if (tipo.Equals("Compra", StringComparison.OrdinalIgnoreCase)) return "Compra";
            return "Venta";
        }
    }
}
