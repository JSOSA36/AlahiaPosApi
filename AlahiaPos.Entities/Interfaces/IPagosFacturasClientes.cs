using System.Collections.Generic;
using System.Threading.Tasks;
using AlahiaPos.Entities.Domain;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IPagosFacturasClientes
    {
        /// <summary>
        /// Obtiene todos los pagos de facturas realizados por los clientes de una empresa.
        /// </summary>
        /// <param name="IdEmpresa">Identificador de la empresa.</param>
        /// <returns>Lista de pagos de facturas.</returns>
        Task<IEnumerable<PagosFacturasClientes>> GetAllPagosFacturasClientes(int IdEmpresa);
        public Task RegistrarPagoFactura(int IdFactura, PagosFacturasClientes pago);
        /// <summary>
        /// Obtiene un pago específico de factura por su identificador.
        /// </summary>
        /// <param name="IdPago">Identificador del pago.</param>
        /// <returns>Objeto PagosFacturasClientes.</returns>
        Task<PagosFacturasClientes> GetPagosFacturasClientesById(int IdPago);

        /// <summary>
        /// Obtiene todos los pagos asociados a una factura específica.
        /// </summary>
        /// <param name="IdFactura">Identificador de la factura.</param>
        /// <returns>Lista de pagos realizados a esa factura.</returns>
        Task<IEnumerable<PagosFacturasClientes>> GetPagosByFacturaId(int IdFactura);

        /// <summary>
        /// Inserta un nuevo registro de pago de factura de cliente.
        /// </summary>
        /// <param name="pago">Objeto de pago a insertar.</param>
        Task InsertPagosFacturasClientes(PagosFacturasClientes pago);

        /// <summary>
        /// Actualiza la información de un pago de factura existente.
        /// </summary>
        /// <param name="IdPago">Identificador del pago a actualizar.</param>
        /// <param name="pago">Objeto con los nuevos datos.</param>
      

        /// <summary>
        /// Elimina un pago de factura del registro.
        /// </summary>
        /// <param name="IdPago">Identificador del pago a eliminar.</param>
        void DeletePagosFacturasClientes(int IdPago);
    }
}
