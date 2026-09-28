using AlahiaPos.DataAccess.Data;
using AlahiaPos.DataAccess.Servicios;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AlahiaPosApi.Auth;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacturaDetalleController : ControllerBase
    {

        IMapper _Mapper;
        IFacturaDetalle _IFacturaDetalle;
        IFacturaHeader _IFacturaHeader;
        IProductos _IProducto;
        IValidateIMpuesto validateIMpuesto;
        IProductos _productos;
        readonly IProduccionPosAdapter _produccionPosAdapter;
        readonly AlahiaPosContext _context;
        public int _IdFact = 0;
        public FacturaDetalleController(IMapper mapper, IFacturaDetalle iFacturaDetalle, IFacturaHeader iFacturaHeader,
            IProductos iProducto, IValidateIMpuesto validateIMpuesto, IProductos productos,
            IProduccionPosAdapter produccionPosAdapter, AlahiaPosContext context)
        {
            _Mapper = mapper;
            _IFacturaDetalle = iFacturaDetalle;
            _IFacturaHeader = iFacturaHeader;
            _IProducto = iProducto;
            this.validateIMpuesto = validateIMpuesto;
            _productos = productos;
            _produccionPosAdapter = produccionPosAdapter;
            _context = context;
        }


        // GET: api/<FacturaDetalleController>
        [HttpGet]
        public async Task<IEnumerable<FacturaDetalles>> Get(int id,int IdEmpresa)
        {
            return await _IFacturaDetalle.GetAllFacturaDetalle(id,IdEmpresa);
        }

        // GET api/<FacturaDetalleController>/5
        
        [HttpGet]
        [Route("GetbyId/{IdMesa}")]
        public async Task<IEnumerable<FacturaDetalles>> GetbyId(int IdMesa,int IdEmpresa)
        {
            return await _IFacturaDetalle.GetOrdenesByIdMesa(IdMesa, IdEmpresa);
        }


        // POST api/<FacturaDetalleController>
        // Agrega ítems a UNA orden existente. Todos los renglones deben ir al mismo IdFacturaHeader.
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] FacturaDetallesDto[] value)
        {
            try
            {
            if (value == null || value.Length == 0)
                return BadRequest("No hay artículos para agregar.");

            var idsHeader = value
                .Select(x => x.IdFacturaHeader)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (idsHeader.Count == 0)
                return BadRequest("Indique la orden a la que se agregan los artículos.");

            if (idsHeader.Count > 1)
                return BadRequest("No se pueden agregar artículos a más de una orden en la misma petición.");

            var idHeader = idsHeader[0];
            var headerOrden = _IFacturaHeader.GetById(idHeader);
            if (headerOrden == null)
                return NotFound("La orden no existe.");

            if (!TenantRecurso.EsDeLaSesion(HttpContext, headerOrden.IdEmpresa))
                return NotFound("La orden no existe.");

            // Solo órdenes (10) o cotizaciones (2): nunca montar ítems en una factura cobrada.
            if (headerOrden.IdTipoDocumentos != 10 && headerOrden.IdTipoDocumentos != 2)
                return BadRequest("Solo se pueden agregar artículos a órdenes o cotizaciones abiertas.");

            if (headerOrden.EstaCancelada)
                return BadRequest("No se pueden agregar artículos a una orden cancelada.");

            foreach (var item in value)
            {
                if (item.IdFacturaHeader <= 0 || item.IdProducto <= 0)
                    continue;

                // Forzar el header validado: el cliente no puede mezclar otra orden por renglón.
                item.IdFacturaHeader = idHeader;

                var idEmpresa = headerOrden.IdEmpresa;
                var DetalleUpdate = _IFacturaDetalle
                    .GetDetalleByIdHeader(item.IdFacturaHeader)
                    ?.FirstOrDefault(x => x.IdProducto == item.IdProducto);

                if (DetalleUpdate == null)
                {
                    var _Producto = _productos.GetProductoById(item.IdProducto);
                    if (_Producto == null)
                        continue;

                    FacturaDetalles d = new FacturaDetalles();
                    d.Cantidad = item.Cantidad;
                    d.IdProducto = item.IdProducto;
                    d.IdFacturaHeader = idHeader;
                    d.IdEmpleadoComision = item.IdEmpleadoComision;
                    d.PrecioOferta = item.PrecioOferta;
                    d.IdEmpresa = idEmpresa;
                    d.Comentario = item.Comentario;
                    d.Descuento = item.Descuento;
                    d.Dias = item.Dias;
                    d.EnviadoCocina = item.EnviadoCocina;
                    d.FechaInseccion = DateTime.Now;

                    _IdFact = d.IdFacturaHeader;

                    decimal precioOriginal = _Producto.PrecioVenta;
                    decimal precioFinal = item.PrecioOferta > 0 ? item.PrecioOferta : precioOriginal;

                    d.Itbis = (decimal)this.validateIMpuesto
                        .SetItbis(_Producto.Itbis, Convert.ToDouble(precioOriginal));

                    d.SubTotal = (precioFinal * item.Cantidad) + d.Itbis;

                    d.Descuento = (precioOriginal - precioFinal) * item.Cantidad;
                    if (d.Descuento < 0)
                        d.Descuento = 0;

                    _IFacturaDetalle.InsertFactDNoasync(d);
                }
                else
                {
                    decimal _Cantidad = DetalleUpdate.Cantidad;
                    var _Producto = _IProducto.GetProductoById(item.IdProducto);
                    if (_Producto == null)
                        continue;

                    DetalleUpdate.Cantidad = _Cantidad + item.Cantidad;

                    decimal precioOriginal = _Producto.PrecioVenta;
                    decimal precioFinal = item.PrecioOferta > 0 ? item.PrecioOferta : precioOriginal;

                    decimal nuevaCantidad = DetalleUpdate.Cantidad;

                    DetalleUpdate.Itbis = (decimal)this.validateIMpuesto
                        .SetItbis(_Producto.Itbis, Convert.ToDouble(precioOriginal)) * nuevaCantidad;

                    DetalleUpdate.SubTotal = (precioFinal * nuevaCantidad) + DetalleUpdate.Itbis;
                    _IdFact = DetalleUpdate.IdFacturaHeader;
                    DetalleUpdate.Descuento = (precioOriginal - precioFinal) * nuevaCantidad;
                    if (DetalleUpdate.Descuento < 0)
                        DetalleUpdate.Descuento = 0;

                    _IFacturaDetalle.UpdateFacturaDetalle(DetalleUpdate.IdFacturaDetalle, DetalleUpdate);
                }
            }

            if (_IdFact <= 0)
                return BadRequest("No se pudo agregar el artículo a la orden.");

            var _DetalleUpdate = _IFacturaDetalle.GetDetalleByIdHeader(_IdFact)?.ToList()
                ?? new List<FacturaDetalles>();
            var _UpdateFacta = _IFacturaHeader.GetById(_IdFact);
            if (_UpdateFacta == null)
                return NotFound("La orden no existe.");

            _UpdateFacta.SubTotal = _DetalleUpdate.Sum(c => c.SubTotal - c.Itbis);
            _UpdateFacta.TotalItbis = _DetalleUpdate.Sum(c => c.Itbis);
            _UpdateFacta.PrintPending = false;
            _UpdateFacta.Total = _DetalleUpdate.Sum(c => c.SubTotal);
            _UpdateFacta.Clientes = null;
            _UpdateFacta.FacturaDetalles = null;
            _UpdateFacta.Empleados = null;
            _UpdateFacta.Mesas = null;
            _context.ChangeTracker.Clear();
            _IFacturaHeader.UpdateFacturaHeader(_IdFact, _UpdateFacta);

            try
            {
                _UpdateFacta.FacturaDetalles = _DetalleUpdate;
                await _produccionPosAdapter.PublicarOrdenSiAplicaAsync(_UpdateFacta, _IdFact);
            }
            catch
            {
                /* no tumbar el agregado de ítems */
            }

            return Ok(new
            {
                idFacturaHeader = _IdFact,
                total = _UpdateFacta.Total
            });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.InnerException?.Message ?? ex.Message });
            }
        }
        [HttpPut("cambiar-empleado-detalle")]
        public async Task<IActionResult> CambiarEmpleadoDetalle(int idFacturaDetalle, int idEmpleado)
        {
            var result = await _IFacturaDetalle.CambiarEmpleadoDetalle(idFacturaDetalle, idEmpleado);

            if (!result)
                return NotFound("Detalle no encontrado");

            return Ok();
        }
        [HttpGet]
        [Route("ActualizarCantidad/{idDetalle}/{cantidadNueva}")]
        public IActionResult ActualizarCantidad(int idDetalle, decimal cantidadNueva)
        {
            try
            {
                var det = _IFacturaDetalle.GetDetallesId(idDetalle);
                if (det == null)
                    return NotFound("Detalle no encontrado");

                // 🔒 BLOQUEO SI TIENE INGRESOS
                

                if (cantidadNueva <= 0)
                    return BadRequest("La cantidad debe ser mayor que 0");

                // ===============================
                // 🔥 PRECIO REAL DESDE EL DETALLE
                // ===============================
                decimal precio = 0;

                if (det.Cantidad > 0)
                    precio = (det.SubTotal + det.Descuento - det.Itbis) / det.Cantidad;

                // ===============================
                // 🔥 DESCUENTO UNITARIO
                // ===============================
                decimal descuentoUnitario = 0;

                if (det.Cantidad > 0 && det.Descuento > 0)
                    descuentoUnitario = det.Descuento / det.Cantidad;

                // ===============================
                // ACTUALIZAR CANTIDAD
                // ===============================
                det.Cantidad = cantidadNueva;

                // Nuevo descuento proporcional
                det.Descuento = descuentoUnitario * cantidadNueva;

                // ===============================
                // ITBIS
                // ===============================
                var prod = _IProducto.GetProductoById(det.IdProducto);

                det.Itbis = (decimal)this.validateIMpuesto
                    .SetItbis(prod.Itbis, Convert.ToDouble(precio)) * cantidadNueva;

                // ===============================
                // SUBTOTAL REAL
                // ===============================
                det.SubTotal = (precio * cantidadNueva) - det.Descuento + det.Itbis;

                if (det.SubTotal < 0)
                    det.SubTotal = 0;

                _IFacturaDetalle.UpdateFacturaDetalle(det.IdFacturaDetalle, det);

                // ===============================
                // 🔥 RECALCULAR HEADER
                // ===============================
                var detalles = _IFacturaDetalle.GetDetalleByIdHeader(det.IdFacturaHeader);
                var header = _IFacturaHeader.GetById(det.IdFacturaHeader);

                header.SubTotal = detalles.Sum(c => c.SubTotal);
                header.Total = header.SubTotal;
                header.PrintPending = false;
                header.Clientes = null;

                _IFacturaHeader.UpdateFacturaHeader(header.IdFacturaHeader, header);

                return Ok(new
                {
                    message = "Cantidad actualizada correctamente",
                    subtotalDetalle = det.SubTotal,
                    descuentoDetalle = det.Descuento,
                    totalFactura = header.Total
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPut("precio/{idDetalle}")]
        public async Task<IActionResult> ActualizarPrecioDetalle(
    int idDetalle,
    [FromBody] UpdatePrecioDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest("Datos requeridos");

                if (dto.Precio <= 0)
                    return BadRequest("Precio inválido");

                var det = _IFacturaDetalle.GetDetallesId(idDetalle);
                if (det == null)
                    return NotFound("Detalle no encontrado");

                // 🔒 BLOQUEAR SI TIENE INGRESOS
               
                // ===============================
                // 🔥 ACTUALIZAR PRECIO
                // ===============================
                decimal precio = dto.Precio;

                // Descuento unitario
                decimal descuentoUnitario = 0;
                if (det.Cantidad > 0 && det.Descuento > 0)
                    descuentoUnitario = det.Descuento / det.Cantidad;

                // Recalcular descuento proporcional
                det.Descuento = descuentoUnitario * det.Cantidad;

                // ITBIS
                var prod = _IProducto.GetProductoById(det.IdProducto);

                det.Itbis = (decimal)this.validateIMpuesto
                    .SetItbis(prod.Itbis, Convert.ToDouble(precio)) * det.Cantidad;

                // Subtotal
                det.SubTotal = (precio * det.Cantidad) - det.Descuento + det.Itbis;

                if (det.SubTotal < 0)
                    det.SubTotal = 0;

                // Guardar detalle
                _IFacturaDetalle.UpdateFacturaDetalle(det.IdFacturaDetalle, det);

                // ===============================
                // 🔥 RECALCULAR HEADER
                // ===============================
                var detalles = _IFacturaDetalle.GetDetalleByIdHeader(det.IdFacturaHeader);
                var header = _IFacturaHeader.GetById(det.IdFacturaHeader);

                header.SubTotal = detalles.Sum(c => c.SubTotal);
                header.Total = header.SubTotal;
                header.PrintPending = false;
                header.Clientes = null;

                _IFacturaHeader.UpdateFacturaHeader(header.IdFacturaHeader, header);

                return Ok(new
                {
                    message = "Precio actualizado correctamente",
                    subtotalDetalle = det.SubTotal,
                    totalFactura = header.Total
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        // PUT api/<FacturaDetalleController>/5

        [HttpGet]
        [Route("AumentarCantidad/{IdFactDetalle}")]
        public IActionResult AumentarCantidad(int IdFactDetalle)
        {
            try
            {
                var det = _IFacturaDetalle.GetDetallesId(IdFactDetalle);
                if (det == null)
                    return NotFound("Detalle no encontrado");

                // 🔒 BLOQUEO SI TIENE PAGOS
               

                var prod = _IProducto.GetProductoById(det.IdProducto);
                if (prod == null)
                    return NotFound("Producto no encontrado");

                // ==========================
                // 🔥 PRECIO REAL DESDE DETALLE
                // ==========================
                decimal precio = 0;

                if (det.Cantidad > 0)
                    precio = (det.SubTotal + det.Descuento - det.Itbis) / det.Cantidad;

                // ==========================
                // 🔥 DESCUENTO UNITARIO
                // ==========================
                decimal descuentoUnitario = 0;

                if (det.Cantidad > 0 && det.Descuento > 0)
                    descuentoUnitario = det.Descuento / det.Cantidad;

                // ==========================
                // 🔥 AUMENTAR CANTIDAD
                // ==========================
                det.Cantidad += 1;

                // Recalcular descuento proporcional
                det.Descuento = descuentoUnitario * det.Cantidad;

                det.StatuItem = false;

                // ==========================
                // 🔥 ITBIS
                // ==========================
                det.Itbis = (decimal)this.validateIMpuesto
                    .SetItbis(prod.Itbis, Convert.ToDouble(precio)) * det.Cantidad;

                // ==========================
                // 🔥 SUBTOTAL REAL
                // ==========================
                det.SubTotal = (precio * det.Cantidad) - det.Descuento + det.Itbis;

                if (det.SubTotal < 0)
                    det.SubTotal = 0;

                // ==========================
                // 💾 GUARDAR DETALLE
                // ==========================
                _IFacturaDetalle.UpdateFacturaDetalle(det.IdFacturaDetalle, det);

                // ==========================
                // 🔥 RECALCULAR HEADER
                // ==========================
                var detalles = _IFacturaDetalle.GetDetalleByIdHeader(det.IdFacturaHeader);
                var header = _IFacturaHeader.GetById(det.IdFacturaHeader);

                header.SubTotal = detalles.Sum(c => (c.SubTotal - c.Itbis)); // base sin ITBIS
                header.TotalItbis = detalles.Sum(c => c.Itbis);
                header.Total = detalles.Sum(c => c.SubTotal);

                header.PrintPending = false;
                header.Clientes = null;

                _IFacturaHeader.UpdateFacturaHeader(header.IdFacturaHeader, header);

                return Ok(new
                {
                    message = "Cantidad aumentada correctamente",
                    nuevaCantidad = det.Cantidad,
                    subtotalDetalle = det.SubTotal,
                    totalFactura = header.Total
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        // DELETE api/<FacturaDetalleController>/5
        [HttpDelete("{id}")]
        public async void Delete(int id)
        {
            var _Detalle =  _IFacturaDetalle.GetDetallesId(id);
            _IFacturaDetalle.DeleteFacturaDetalle(id);

            var _DetalleUpdate = _IFacturaDetalle.GetDetalleByIdHeader(_Detalle.IdFacturaHeader);
            var _UpdateFacta = _IFacturaHeader.GetById(_Detalle.IdFacturaHeader);
            _UpdateFacta.Clientes = null;
            _UpdateFacta.SubTotal = _DetalleUpdate.Sum(c => c.SubTotal) - _DetalleUpdate.Sum(c => c.Itbis);
            _UpdateFacta.PrintPending = false;
            _UpdateFacta.TotalItbis = _DetalleUpdate.Sum(c => c.Itbis);
            _UpdateFacta.MontoPropina = _DetalleUpdate.Sum(c => c.SubTotal);
            _UpdateFacta.Total = _DetalleUpdate.Sum(c => c.SubTotal);
            _IFacturaHeader.UpdateFacturaHeader(_IdFact, _UpdateFacta);
        }
    }
}
