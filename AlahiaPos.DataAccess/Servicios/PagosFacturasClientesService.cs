using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class PagosFacturasClientesService : IPagosFacturasClientes
    {
        private readonly IRepository<PagosFacturasClientes> _repository;
        private readonly IRepository<FacturaHeaders> _facturaRepository;
        private readonly IRepository<Ingresos> _IngresosServices;
        public PagosFacturasClientesService(
            IRepository<PagosFacturasClientes> repository,
            IRepository<FacturaHeaders> facturaRepository, IRepository<Ingresos> Ingresos)
        {
            _repository = repository;
            _facturaRepository = facturaRepository;
            _IngresosServices=Ingresos; 
        }

        /// <summary>
        /// Obtiene todos los pagos de facturas realizados por los clientes de una empresa.
        /// </summary>
        public async Task<IEnumerable<PagosFacturasClientes>> GetAllPagosFacturasClientes(int IdEmpresa)
        {
            return await _repository.GetAllByExpresionAsync(p => p.IdEmpresa == IdEmpresa);
        }

        /// <summary>
        /// Obtiene un pago específico de factura por su identificador.
        /// </summary>
        public async Task<PagosFacturasClientes> GetPagosFacturasClientesById(int IdPago)
        {
            return await _repository.GetByIdAsync(IdPago);
        }

        /// <summary>
        /// Obtiene todos los pagos asociados a una factura específica.
        /// </summary>
        public async Task<IEnumerable<PagosFacturasClientes>> GetPagosByFacturaId(int IdFactura)
        {
            var pagos = await _repository.GetAllByExpresionAsync(p => p.IdFacturaHeader == IdFactura);
            return pagos.OrderByDescending(p => p.FechaInseccion);
        }

        /// <summary>
        /// Inserta un nuevo registro de pago de factura de cliente.
        /// </summary>
        public async Task InsertPagosFacturasClientes(PagosFacturasClientes pago)
        {
            pago.FechaInseccion = DateTime.Now;
            await _repository.Save(pago);
        }

        /// <summary>
        /// Registra un nuevo pago en una factura y actualiza los montos correspondientes.
        /// </summary>
        public async Task RegistrarPagoFactura(int IdFactura, PagosFacturasClientes pago)
        {
            // 🔹 Buscar la factura
            var factura = await _facturaRepository.GetByIdAsync(IdFactura);
            if (factura == null)
                throw new Exception($"No se encontró la factura con ID {IdFactura}");

            factura.Clientes = null;
            // 🔹 Completar datos del pago
            pago.IDCliente = factura.IDCliente;
            pago.FechaInseccion = DateTime.Now;
            pago.IdEmpresa = factura.IdEmpresa;

            // 1️⃣ Guardar el pago
            await _repository.Save(pago);

            // 2️⃣ Actualizar la factura
            factura.Pagado += pago.Monto;
            factura.Pendiente = factura.Total - factura.Pagado;
            factura.Estado = factura.Pendiente > 0 ? "Pendiente" : "Pagada";

            _facturaRepository.Update(IdFactura, factura);

            // 3️⃣ Determinar el tipo de ingreso
            string categoria = factura.Pendiente == 0 ? "Saldo de Factura de Crédito" : "Abono a Factura";
            string descripcion = factura.Pendiente == 0
                ? $"Pago completo de factura #{factura.IdFacturaHeader}"
                : $"Abono a factura #{factura.IdFacturaHeader}";

            // 4️⃣ Registrar el ingreso contable asociado al pago
            var ingreso = new Ingresos
            {
                IdEmpresa = factura.IdEmpresa,
                FechaRegistro = DateTime.Now,
                Descripcion = descripcion,
                Categoria = categoria,
                Origen = factura.Clientes?.NombreComercial ?? "Cliente desconocido",
                Monto = pago.Monto,
                FormaPago = pago.FormaPago ?? "Efectivo",
                Referencia = $"Factura #{factura.IdFacturaHeader}",
                IdFacturaHeader = factura.IdFacturaHeader,
                IdCliente = factura.IDCliente,
                IdUsuario=factura.IdEmpleados,
                Nota = pago.Nota







            };

            // 5️⃣ Guardar el ingreso
            await _IngresosServices.Save(ingreso);
        }



        /// <summary>
        /// Elimina un pago de factura del registro.
        /// </summary>
        public void DeletePagosFacturasClientes(int IdPago)
        {
            _repository.Delete(IdPago);
        }
    }
}
