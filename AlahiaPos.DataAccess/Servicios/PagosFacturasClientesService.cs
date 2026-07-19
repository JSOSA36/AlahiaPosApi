using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class PagosFacturasClientesService : IPagosFacturasClientes
    {
        private readonly AlahiaPosContext _context;
        private readonly IRepository<PagosFacturasClientes> _repository;
        private readonly IRepository<FacturaHeaders> _facturaRepository;
        private readonly IRepository<Clientes> _clientesRepository;
        private readonly IIngresos _ingresosService;
        private readonly IMetodoPagoCuentaService _metodoPagoCuentaService;
        private readonly IMovimientoFinancieroService _movimientoFinancieroService;

        public PagosFacturasClientesService(
            AlahiaPosContext context,
            IRepository<PagosFacturasClientes> repository,
            IRepository<FacturaHeaders> facturaRepository,
            IRepository<Clientes> clientesRepository,
            IIngresos ingresosService,
            IMetodoPagoCuentaService metodoPagoCuentaService,
            IMovimientoFinancieroService movimientoFinancieroService)
        {
            _context = context;
            _repository = repository;
            _facturaRepository = facturaRepository;
            _clientesRepository = clientesRepository;
            _ingresosService = ingresosService;
            _metodoPagoCuentaService = metodoPagoCuentaService;
            _movimientoFinancieroService = movimientoFinancieroService;
        }

        public async Task<IEnumerable<PagosFacturasClientes>> GetAllPagosFacturasClientes(int IdEmpresa)
        {
            return await _repository.GetAllByExpresionAsync(p => p.IdEmpresa == IdEmpresa);
        }

        public async Task<PagosFacturasClientes> GetPagosFacturasClientesById(int IdPago)
        {
            return await _repository.GetByIdAsync(IdPago);
        }

        public async Task<IEnumerable<PagosFacturasClientes>> GetPagosByFacturaId(int IdFactura)
        {
            var pagos = await _repository.GetAllByExpresionAsync(p => p.IdFacturaHeader == IdFactura);
            return pagos.OrderByDescending(p => p.FechaInseccion);
        }

        public async Task InsertPagosFacturasClientes(PagosFacturasClientes pago)
        {
            pago.FechaInseccion = DateTime.Now;
            await _repository.Save(pago);
        }

        public async Task RegistrarPagoFactura(int IdFactura, PagosFacturasClientes pago)
        {
            if (pago == null)
                throw new ArgumentException("Datos del pago inválidos");

            if (pago.Monto <= 0)
                throw new ArgumentException("El monto del pago debe ser mayor a cero");

            var factura = await _facturaRepository.GetByIdAsync(IdFactura);
            if (factura == null)
                throw new KeyNotFoundException($"No se encontró la factura con ID {IdFactura}");

            var pendienteActual = factura.Total - factura.Pagado;
            if (pago.Monto > pendienteActual)
                throw new ArgumentException("El monto ingresado excede el pendiente de la factura.");

            var formaPago = string.IsNullOrWhiteSpace(pago.FormaPago)
                ? "Efectivo"
                : pago.FormaPago;

            pago.IDCliente = factura.IDCliente;
            pago.FechaInseccion = DateTime.Now;
            pago.IdEmpresa = factura.IdEmpresa;
            pago.IdFacturaHeader = IdFactura;
            pago.FormaPago = formaPago;

            if (string.IsNullOrWhiteSpace(pago.NumeroDocumento))
                pago.NumeroDocumento = factura.NumeroDocumento ?? string.Empty;

            await _repository.Save(pago);

            factura.Clientes = null;
            factura.Pagado += pago.Monto;
            factura.Pendiente = factura.Total - factura.Pagado;
            factura.Estado = factura.Pendiente > 0 ? "Pendiente" : "Pagada";

            _facturaRepository.Update(IdFactura, factura);

            var categoria = factura.Pendiente == 0
                ? "Saldo de Factura de Crédito"
                : "Abono a Crédito";

            var descripcion = factura.Pendiente == 0
                ? $"Pago completo de factura #{factura.IdFacturaHeader}"
                : $"Abono - Factura #{factura.IdFacturaHeader}";

            await _ingresosService.InsertIngreso(new Ingresos
            {
                IdEmpresa = factura.IdEmpresa,
                FechaRegistro = DateTime.Now,
                Descripcion = descripcion,
                Categoria = categoria,
                Origen = "Cliente crédito",
                Monto = pago.Monto,
                FormaPago = formaPago,
                Referencia = $"Factura #{factura.IdFacturaHeader}",
                IdFacturaHeader = factura.IdFacturaHeader,
                IdCliente = factura.IDCliente,
                IdUsuario = factura.IdEmpleados,
                Nota = pago.Nota
            });

            var metodoConfigurado = await _metodoPagoCuentaService
                .GetByMetodoAsync(factura.IdEmpresa, formaPago);

            if (metodoConfigurado != null &&
                metodoConfigurado.IdCuentaFinanciera > 0)
            {
                await _movimientoFinancieroService.RegistrarEntradaAsync(
                    factura.IdEmpresa,
                    factura.IdEmpleados ?? 0,
                    metodoConfigurado.IdCuentaFinanciera,
                    pago.Monto,
                    $"Factura #{factura.IdFacturaHeader}",
                    $"Ingreso automático desde cobro a cliente ({formaPago})",
                    categoria: "COBRO_CXC",
                    referenciaId: factura.IdFacturaHeader,
                    referenciaTipo: "FACTURA",
                    claveIdempotencia: $"CXC-{factura.IdFacturaHeader}-{formaPago}-{pago.Monto}-{pago.FechaInseccion:yyyyMMddHHmmss}"
                );
            }
        }

        public async Task<RegistrarPagoLoteResult> RegistrarPagoLoteAsync(RegistrarPagoLoteRequest request)
        {
            if (request == null)
                throw new ArgumentException("Datos del pago inválidos");

            if (request.IdEmpresa <= 0)
                throw new ArgumentException("IdEmpresa es requerido.");

            if (request.IdCliente <= 0)
                throw new ArgumentException("Debe seleccionar un cliente.");

            if (request.Aplicaciones == null || request.Aplicaciones.Count == 0)
                throw new ArgumentException("Debe indicar al menos una factura a cobrar.");

            var aplicaciones = request.Aplicaciones
                .Where(a => a != null && a.IdFacturaHeader > 0 && a.Monto > 0)
                .GroupBy(a => a.IdFacturaHeader)
                .Select(g => new AplicacionPagoFacturaDto
                {
                    IdFacturaHeader = g.Key,
                    Monto = g.Sum(x => x.Monto)
                })
                .ToList();

            if (aplicaciones.Count == 0)
                throw new ArgumentException("Los montos aplicados deben ser mayores a cero.");

            var formaPago = string.IsNullOrWhiteSpace(request.FormaPago)
                ? "Efectivo"
                : request.FormaPago.Trim();

            var notaBase = string.IsNullOrWhiteSpace(request.Nota)
                ? "Cobro múltiple"
                : request.Nota.Trim();

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                decimal montoTotal = 0;

                foreach (var app in aplicaciones)
                {
                    var factura = await _facturaRepository.GetByIdAsync(app.IdFacturaHeader)
                        ?? throw new KeyNotFoundException($"No se encontró la factura {app.IdFacturaHeader}");

                    if (factura.IdEmpresa != request.IdEmpresa)
                        throw new ArgumentException($"La factura #{app.IdFacturaHeader} no pertenece a la empresa.");

                    if (factura.IDCliente != request.IdCliente)
                        throw new ArgumentException(
                            $"Todas las facturas deben ser del mismo cliente. Factura #{app.IdFacturaHeader} no coincide.");

                    if (factura.EstaCancelada)
                        throw new ArgumentException($"La factura #{app.IdFacturaHeader} está anulada.");

                    await RegistrarPagoFactura(app.IdFacturaHeader, new PagosFacturasClientes
                    {
                        IdFacturaHeader = app.IdFacturaHeader,
                        IDCliente = request.IdCliente,
                        FormaPago = formaPago,
                        Monto = app.Monto,
                        Nota = $"{notaBase} (lote)",
                        NumeroDocumento = factura.NumeroDocumento ?? string.Empty,
                        IdEmpresa = request.IdEmpresa
                    });

                    montoTotal += app.Monto;
                }

                await tx.CommitAsync();

                return new RegistrarPagoLoteResult
                {
                    FacturasAfectadas = aplicaciones.Count,
                    MontoTotal = montoTotal,
                    Message = $"Se registró cobro en {aplicaciones.Count} factura(s) por {montoTotal:N2}"
                };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public void DeletePagosFacturasClientes(int IdPago)
        {
            _repository.Delete(IdPago);
        }

        public async Task<EstadoCuentaClienteDto> ObtenerEstadoCuentaClienteAsync(
            int idEmpresa,
            int idCliente,
            DateTime desde,
            DateTime hasta)
        {
            if (idEmpresa <= 0)
                throw new ArgumentException("IdEmpresa es requerido.");
            if (idCliente <= 0)
                throw new ArgumentException("Debe seleccionar un cliente.");

            var desdeDia = desde.Date;
            var hastaDia = hasta.Date;
            if (hastaDia < desdeDia)
                throw new ArgumentException("La fecha hasta no puede ser menor que desde.");

            var cliente = await _clientesRepository.GetByExpresionAsync(c =>
                c.IDCliente == idCliente && c.IdEmpresa == idEmpresa)
                ?? throw new KeyNotFoundException("Cliente no encontrado.");

            var facturas = (await _facturaRepository.GetAllByExpresionAsync(h =>
                h.IdEmpresa == idEmpresa
                && h.IDCliente == idCliente
                && h.IdTipoDocumentos == 1
                && h.TipoFactura == "Credito"
                && h.EstaCancelada == false)).ToList();

            var facturaIds = facturas.Select(f => f.IdFacturaHeader).ToList();

            var pagos = facturaIds.Count == 0
                ? new List<PagosFacturasClientes>()
                : (await _repository.GetAllByExpresionAsync(p =>
                    p.IdEmpresa == idEmpresa
                    && (p.IDCliente == idCliente || facturaIds.Contains(p.IdFacturaHeader))))
                  .ToList();

            decimal SumDebitosAntes(DateTime corte) =>
                facturas
                    .Where(h => h.FechaInseccion.Date < corte)
                    .Sum(h => h.Total);

            decimal SumCreditosAntes(DateTime corte) =>
                pagos
                    .Where(p => p.FechaInseccion.Date < corte)
                    .Sum(p => p.Monto);

            var saldoInicial = SumDebitosAntes(desdeDia) - SumCreditosAntes(desdeDia);

            var movimientos = new List<EstadoCuentaClienteMovimientoDto>();
            var balance = saldoInicial;

            movimientos.Add(new EstadoCuentaClienteMovimientoDto
            {
                Fecha = desdeDia,
                Tipo = "SALDO_INICIAL",
                NumeroDocumento = "—",
                Concepto = "Saldo inicial",
                Debito = 0,
                Credito = 0,
                Balance = balance
            });

            var facturasPeriodo = facturas
                .Where(h => h.FechaInseccion.Date >= desdeDia && h.FechaInseccion.Date <= hastaDia)
                .Select(h => new
                {
                    Fecha = h.FechaInseccion.Date,
                    Orden = 1,
                    Id = h.IdFacturaHeader,
                    Tipo = "FACTURA",
                    Numero = !string.IsNullOrWhiteSpace(h.NumeroDocumento)
                        ? h.NumeroDocumento!
                        : $"FACT-{h.IdFacturaHeader}",
                    Concepto = $"Factura a crédito{(string.IsNullOrWhiteSpace(h.NCF) ? "" : $" NCF {h.NCF}")}",
                    Debito = h.Total,
                    Credito = 0m,
                    FormaPago = (string?)null,
                    IdFact = (int?)h.IdFacturaHeader
                });

            var pagosPeriodo = pagos
                .Where(p => p.FechaInseccion.Date >= desdeDia && p.FechaInseccion.Date <= hastaDia)
                .Select(p => new
                {
                    Fecha = p.FechaInseccion.Date,
                    Orden = 2,
                    Id = p.Id,
                    Tipo = "PAGO",
                    Numero = string.IsNullOrWhiteSpace(p.NumeroDocumento)
                        ? $"PAGO-{p.Id}"
                        : p.NumeroDocumento,
                    Concepto = string.IsNullOrWhiteSpace(p.Nota)
                        ? $"Cobro {(p.FormaPago ?? "").Trim()}".Trim()
                        : $"Cobro {(p.FormaPago ?? "").Trim()} — {p.Nota}".Trim(),
                    Debito = 0m,
                    Credito = p.Monto,
                    FormaPago = (string?)p.FormaPago,
                    IdFact = (int?)p.IdFacturaHeader
                });

            foreach (var m in facturasPeriodo.Concat(pagosPeriodo)
                         .OrderBy(x => x.Fecha)
                         .ThenBy(x => x.Orden)
                         .ThenBy(x => x.Id))
            {
                balance += m.Debito - m.Credito;
                movimientos.Add(new EstadoCuentaClienteMovimientoDto
                {
                    Fecha = m.Fecha,
                    Tipo = m.Tipo,
                    IdDocumento = m.Id,
                    NumeroDocumento = m.Numero,
                    Concepto = m.Concepto,
                    Debito = m.Debito,
                    Credito = m.Credito,
                    Balance = balance,
                    FormaPago = m.FormaPago,
                    IdFacturaHeader = m.IdFact
                });
            }

            var totalFacturadoPeriodo = facturasPeriodo.Sum(x => x.Debito);
            var totalCobradoPeriodo = pagosPeriodo.Sum(x => x.Credito);
            var balancePendiente = facturas.Sum(h => h.Pendiente);

            var hoy = DateTime.Today;
            var pendientes = facturas
                .Where(h => h.Pendiente > 0 && h.Estado == "Pendiente")
                .OrderBy(h => h.FechaBencimiento)
                .Select(h =>
                {
                    int? dias = null;
                    if (h.FechaBencimiento != default)
                        dias = (hoy - h.FechaBencimiento.Date).Days;

                    return new EstadoCuentaClienteFacturaPendienteDto
                    {
                        IdFacturaHeader = h.IdFacturaHeader,
                        Fecha = h.FechaInseccion,
                        NumeroDocumento = string.IsNullOrWhiteSpace(h.NumeroDocumento)
                            ? $"#{h.IdFacturaHeader}"
                            : h.NumeroDocumento,
                        FechaVencimiento = h.FechaBencimiento == default ? null : h.FechaBencimiento,
                        MontoOriginal = h.Total,
                        Pagado = h.Pagado,
                        Pendiente = h.Pendiente,
                        DiasVencimiento = dias,
                        Estado = h.Estado ?? ""
                    };
                })
                .ToList();

            return new EstadoCuentaClienteDto
            {
                IdEmpresa = idEmpresa,
                IdCliente = idCliente,
                ClienteNombre = cliente.NombreComercial,
                ClienteDocumento = cliente.CedulaRNC,
                Desde = desdeDia,
                Hasta = hastaDia,
                SaldoInicial = saldoInicial,
                TotalFacturado = totalFacturadoPeriodo,
                TotalCobrado = totalCobradoPeriodo,
                BalancePendiente = balancePendiente,
                Movimientos = movimientos,
                FacturasPendientes = pendientes
            };
        }
    }
}
