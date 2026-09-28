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
    public class PosDocumentoResolver : IDocumentoOrigenResolver
    {
        private readonly AlahiaPosContext _ctx;

        public PosDocumentoResolver(AlahiaPosContext ctx)
        {
            _ctx = ctx;
        }

        public OrigenDocumento Origen => OrigenDocumento.Pos;

        public async Task<DocumentoOrigenInfo> ObtenerDocumentoAsync(int idOrigen, int idEmpresa)
        {
            var factura = await _ctx.FacturaHeaders
                .Include(f => f.Clientes)
                .Include(f => f.FacturaDetalles)!
                    .ThenInclude(d => d.Productos)
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.IdFacturaHeader == idOrigen)
                ?? throw new InvalidOperationException(
                    $"FacturaHeader {idOrigen} no encontrada");

            var info = new DocumentoOrigenInfo
            {
                Origen = OrigenDocumento.Pos,
                IdOrigen = idOrigen,
                IdEmpresa = idEmpresa,
                FechaDocumento = factura.FechaInseccion,
                IdSucursal = factura.IdSucursal,
                RncCliente = SanearRnc(!string.IsNullOrWhiteSpace(factura.RNC) ? factura.RNC : factura.Clientes?.CedulaRNC),
                NombreCliente = !string.IsNullOrWhiteSpace(factura.NombreEmpresa) ? factura.NombreEmpresa : factura.Clientes?.NombreComercial,
                DireccionCliente = factura.Clientes?.Direccion,
                CorreoCliente = factura.Clientes?.Email,
                NumeroDocumentoInterno = factura.NumeroDocumento,
                SubTotal = factura.SubTotal,
                TotalItbis = factura.TotalItbis,
                TotalDescuento = factura.TotalDescuento,
                Total = factura.Total
            };

            if (factura.FacturaDetalles != null)
            {
                int linea = 1;
                foreach (var d in factura.FacturaDetalles)
                {
                    // En POS, FacturaDetalles.SubTotal = (precio×cant) + ITBIS (bruto).
                    // e-CF con IndicadorMontoGravado=0 exige MontoItem/Precio SIN ITBIS
                    // (igual que las certificaciones directas a Alahia.eCF.Api).
                    var itbisLinea = Math.Round(d.Itbis, 2);
                    var montoNeto = Math.Round(d.SubTotal - itbisLinea, 2);
                    if (montoNeto < 0) montoNeto = 0;

                    var tasa = d.TasaItbis;
                    if (tasa == null && itbisLinea > 0)
                        tasa = 18m;

                    var precioUnitario = d.Cantidad != 0
                        ? Math.Round(montoNeto / d.Cantidad, 2)
                        : 0m;

                    // Recalcular MontoItem = Cantidad × Precio para coherencia DGII
                    var montoItem = Math.Round(precioUnitario * d.Cantidad, 2);
                    if (montoItem != montoNeto && d.Cantidad == 1)
                        montoItem = montoNeto;

                    info.Lineas.Add(new DocumentoOrigenLinea
                    {
                        NumeroLinea = linea++,
                        Descripcion = d.Productos?.Nombre ?? d.Comentario ?? "",
                        Cantidad = d.Cantidad,
                        PrecioUnitario = precioUnitario,
                        MontoItem = montoItem,
                        TasaItbis = tasa,
                        MontoItbis = itbisLinea,
                        EsBien = true
                    });
                }
            }

            if (factura.MontoCargo > 0.009m)
            {
                var cargos = await _ctx.FacturaCargos
                    .AsNoTracking()
                    .Where(c => c.IdFacturaHeader == factura.IdFacturaHeader && c.Monto > 0)
                    .OrderBy(c => c.IdFacturaCargo)
                    .ToListAsync();

                if (cargos.Count == 0)
                {
                    info.Lineas.Add(new DocumentoOrigenLinea
                    {
                        NumeroLinea = info.Lineas.Count + 1,
                        Descripcion = "Cargo por tarjeta",
                        Cantidad = 1,
                        PrecioUnitario = factura.MontoCargo,
                        MontoItem = factura.MontoCargo,
                        TasaItbis = 0m,
                        MontoItbis = 0m,
                        EsBien = false
                    });
                }
                else
                {
                    foreach (var cargo in cargos)
                    {
                        info.Lineas.Add(new DocumentoOrigenLinea
                        {
                            NumeroLinea = info.Lineas.Count + 1,
                            Descripcion = string.IsNullOrWhiteSpace(cargo.Nombre)
                                ? "Cargo por tarjeta"
                                : cargo.Nombre.Trim(),
                            Cantidad = 1,
                            PrecioUnitario = cargo.Monto,
                            MontoItem = cargo.Monto,
                            TasaItbis = 0m,
                            MontoItbis = 0m,
                            EsBien = false
                        });
                    }
                }
            }

            MapearFormasPago(factura, info);

            // Si no hay desglose de medios, el total pagado cubre el comprobante.
            if (!info.FormasPago.Any() && factura.Total > 0)
            {
                var montoPago = factura.Pagado > 0 ? factura.Pagado : factura.Total;
                info.FormasPago.Add(new DocumentoOrigenPago { FormaPagoDgii = 1, Monto = montoPago });
            }

            return info;
        }

        public Task CrearFotografiaAsync(DocumentoOrigenInfo documento)
        {
            return Task.CompletedTask;
        }

        private static void MapearFormasPago(FacturaHeaders f, DocumentoOrigenInfo info)
        {
            if (f.MontoEfectivo > 0)
                info.FormasPago.Add(new DocumentoOrigenPago { FormaPagoDgii = 1, Monto = f.MontoEfectivo });
            if (f.MontoTarjetaVisa > 0 || f.MontoTarjetaMasterCard > 0)
                info.FormasPago.Add(new DocumentoOrigenPago { FormaPagoDgii = 4, Monto = f.MontoTarjetaVisa + f.MontoTarjetaMasterCard });
            if (f.MontoTransferencia > 0)
                info.FormasPago.Add(new DocumentoOrigenPago { FormaPagoDgii = 6, Monto = f.MontoTransferencia });
            if (f.MontoCheques > 0)
                info.FormasPago.Add(new DocumentoOrigenPago { FormaPagoDgii = 3, Monto = f.MontoCheques });
            if (f.MontoNotaCredito > 0)
                info.FormasPago.Add(new DocumentoOrigenPago { FormaPagoDgii = 7, Monto = f.MontoNotaCredito });
        }

        /// <summary>Omite placeholders inválidos (00000, etc.) que DGII marca en AceptadoCondicional.</summary>
        private static string? SanearRnc(string? rnc)
        {
            if (string.IsNullOrWhiteSpace(rnc)) return null;
            var digits = new string(rnc.Where(char.IsDigit).ToArray());
            if (digits.Length is not (9 or 11)) return null;
            if (digits.All(c => c == '0')) return null;
            return digits;
        }
    }
}
