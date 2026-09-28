using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto.Fiscal;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios.FacturacionElectronica
{
    public class NotaCreditoDocumentoResolver : IDocumentoOrigenResolver
    {
        private readonly AlahiaPosContext _ctx;

        public NotaCreditoDocumentoResolver(AlahiaPosContext ctx)
        {
            _ctx = ctx;
        }

        public OrigenDocumento Origen => OrigenDocumento.NotaCredito;

        public async Task<DocumentoOrigenInfo> ObtenerDocumentoAsync(int idOrigen, int idEmpresa)
        {
            var nc = await _ctx.NotasCredito
                .Include(n => n.Detalles)
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.IdNotaCredito == idOrigen)
                ?? throw new InvalidOperationException(
                    $"NotaCredito {idOrigen} no encontrada");

            var info = new DocumentoOrigenInfo
            {
                Origen = OrigenDocumento.NotaCredito,
                IdOrigen = idOrigen,
                IdEmpresa = idEmpresa,
                FechaDocumento = nc.FechaInseccion,
                IdSucursal = nc.IdSucursal,
                RncCliente = nc.RNC,
                NombreCliente = nc.NombreCliente,
                NumeroDocumentoInterno = nc.NumeroDocumento,
                SubTotal = nc.SubTotal,
                TotalItbis = nc.TotalItbis,
                Total = nc.Total,
                NcfModificado = nc.NCFModificado,
                FechaDocumentoModificado = nc.FechaFacturaOrigen,
                CodigoModificacion = (nc.Detalles != null
                    && nc.Detalles.Count > 0
                    && nc.Detalles.All(d => d.IdFacturaDetalle <= 0))
                    ? 3
                    : 1,
                RazonModificacion = string.IsNullOrWhiteSpace(nc.Observacion) ? "Devolucion" : nc.Observacion
            };

            int linea = 1;
            foreach (var d in nc.Detalles)
            {
                // SubTotal en NC es bruto (neto+ITBIS); e-CF requiere MontoItem neto.
                var itbis = Math.Round(d.Itbis != 0 ? d.Itbis : d.ItbisCalculado, 2);
                var montoNeto = Math.Round(d.SubTotal - itbis, 2);
                if (montoNeto < 0) montoNeto = Math.Round(d.PrecioUnitario * d.Cantidad, 2);

                var tasa = d.TasaItbis;
                if (tasa == null && itbis > 0)
                    tasa = 18m;

                info.Lineas.Add(new DocumentoOrigenLinea
                {
                    NumeroLinea = linea++,
                    Descripcion = d.NombreProducto ?? "",
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.Cantidad != 0
                        ? Math.Round(montoNeto / d.Cantidad, 2)
                        : d.PrecioUnitario,
                    MontoItem = montoNeto,
                    TasaItbis = tasa,
                    MontoItbis = itbis,
                    EsBien = true
                });
            }

            if (!info.FormasPago.Any() && info.Total > 0)
            {
                // E34 no incluye TablaFormasPago; el builder las limpia para tipo 34.
                info.FormasPago.Add(new DocumentoOrigenPago
                {
                    FormaPagoDgii = 1,
                    Monto = info.Total
                });
            }

            return info;
        }

        public Task CrearFotografiaAsync(DocumentoOrigenInfo documento)
        {
            return Task.CompletedTask;
        }
    }
}
