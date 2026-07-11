using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class PagosFacturasClientesService : IPagosFacturasClientes
    {
        private readonly IRepository<PagosFacturasClientes> _repository;
        private readonly IRepository<FacturaHeaders> _facturaRepository;
        private readonly IIngresos _ingresosService;
        private readonly IMetodoPagoCuentaService _metodoPagoCuentaService;
        private readonly IMovimientoFinancieroService _movimientoFinancieroService;

        public PagosFacturasClientesService(
            IRepository<PagosFacturasClientes> repository,
            IRepository<FacturaHeaders> facturaRepository,
            IIngresos ingresosService,
            IMetodoPagoCuentaService metodoPagoCuentaService,
            IMovimientoFinancieroService movimientoFinancieroService)
        {
            _repository = repository;
            _facturaRepository = facturaRepository;
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
                    $"Ingreso automático desde cobro a cliente ({formaPago})");
            }
        }

        public void DeletePagosFacturasClientes(int IdPago)
        {
            _repository.Delete(IdPago);
        }
    }
}
