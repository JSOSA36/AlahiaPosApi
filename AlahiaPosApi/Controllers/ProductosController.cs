using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using PrinterLibrary;
using System.Linq;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AlahiaPosApi.Controllers
{


    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : ControllerBase
    {


        IMapper _mapper;
        IProductos services;


        public ProductosController(IMapper mapper, IProductos services)
        {
            _mapper = mapper;
            this.services = services;
        }



        // GET: api/<ProductosController>
        [HttpGet()]
        [Route("GetListadoProductos/{IdEmpresa}")]
        public async Task<IEnumerable<Productos>> GetListadoProductos(int IdEmpresa)
        {
            return await services.GetAllProductos(IdEmpresa);
        }
        [HttpGet()]
        [Route("GetListadoProductosVenta/{IdEmpresa}")]
        public async Task<IEnumerable<Productos>> GetListadoProductosVenta(int IdEmpresa)
        {
            return await services.GetAllProductosVenta(IdEmpresa);
        }

        // GET api/<ProductosController>/5
        [HttpGet("{id}/{IdEmpresa}")]
        
        public async Task<IEnumerable<Productos>> Get(int id, int IdEmpresa)
        {
            return await services.GetAllProductosByIdCategoria(id, IdEmpresa);
        }
        [HttpGet]
        [Route("GetProductByBarCode")]
        public async Task<Productos> Get( string BarCode, int IdEmpresa)
        {
            return await services.GetProductByBarcCode(BarCode, IdEmpresa);
        }

        [HttpGet]
        [Route("BuscarCompra/{idEmpresa}")]
        public async Task<ProductoBusquedaCompraResultDto> BuscarCompra(
            int idEmpresa,
            [FromQuery] string? q = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] int? idAlmacen = null)
        {
            return await services.BuscarProductosCompra(idEmpresa, q, page, pageSize, idAlmacen);
        }

        // POST api/<ProductosController>
        [HttpPost]
        public async Task<IActionResult> GuardarProducto([FromForm] ProductosDto value)
        {
            try
            {
                Productos p;

                // 🔥 SI VIENE ID → UPDATE
                if (value.idProducto > 0)
                {
                    p =  services.GetProductoById(value.idProducto);

                    if (p == null)
                        return NotFound("Producto no encontrado");
                }
                else
                {
                    // 🔥 INSERT
                    p = new Productos();
                    p.FechaVencimiento = DateTime.Now.ToLongDateString();
                    p.IdAlmacen = 1;
                    p.IdProveedor = 1;
                    p.IdUnidadMedida = 1;
                }

                // 🔥 CAMPOS COMUNES (INSERT Y UPDATE)
                p.Nombre = value.nombre;
                p.Descripcion = value.nombre;

                p.Cantidad = value.cantidad ?? 0;
                p.Stock = 1;

                p.PrecioVenta = value.precio ?? 0;
                p.PrecioCompra = value.costo ?? 0;

                p.IdArea = value.IdArea ?? 0;
                p.IdCategoria = value.idcategoria ?? 0;
                p.TipoOperacion = value.TipoOperacion;
                p.DuracionServicio = value.DuracionServicio ?? 0;
                p.DisponibleEnCitas = value.DisponibleEnCitas ?? true;

                p.CodigoBarra = string.IsNullOrEmpty(value.CodigoBarra) ? "N/A" : value.CodigoBarra;

                p.Precio1 = 0;
                p.Precio2 = 0;
                p.Precio3 = 0;
                p.PorcientoGanancia = 0;
                p.PorcientoDescuento = 0;

                p.Itbis = value.Itbis ?? false;

                TipoComportamientoConstantes.AplicarComportamientoErp(
                    p,
                    value.TipoComportamiento);

                p.EsServicio = value.EsServicio ?? false;

                // Servicio ofrecido: nunca controla existencia.
                if (p.EsServicio)
                {
                    p.ControlarStock = false;
                    p.Cantidad = 0;
                    if (TipoComportamientoConstantes.Normalizar(p.TipoComportamiento)
                        == TipoComportamientoConstantes.Inventario
                        || TipoComportamientoConstantes.Normalizar(p.TipoComportamiento)
                        == TipoComportamientoConstantes.ActivoFijo)
                    {
                        p.TipoComportamiento = TipoComportamientoConstantes.Servicio;
                    }
                }

                p.Descuento = 0;
                p.IsActivo = true;

                if (!p.EsServicio
                    && string.IsNullOrWhiteSpace(value.TipoComportamiento)
                    && TipoComportamientoConstantes.Normalizar(p.TipoComportamiento) == TipoComportamientoConstantes.Inventario)
                {
                    p.ControlarStock = value.ControlarStock;
                }

                p.Ganancia = 0;
                p.Rentado = 0;

                p.IdEmpresa = value.IdEmpresa;
                p.EsProductoBelleza = value.isproductobelleza ?? false;

                // 🔥 IMAGEN — nombre único (evita choque FTP/caché entre productos)
                if (value.Imagen != null)
                {
                    using var ms = new MemoryStream();
                    await value.Imagen.CopyToAsync(ms);
                    byte[] imagenBytes = ms.ToArray();

                    var idEmpresa = value.IdEmpresa > 0 ? value.IdEmpresa : p.IdEmpresa;
                    var idProd = value.idProducto > 0 ? value.idProducto : 0;
                    var fileName = $"{idEmpresa}_{idProd}_{Guid.NewGuid():N}.jpg";

                    var url = Utility.UploadFileFtp(imagenBytes, fileName);
                    if (!string.IsNullOrWhiteSpace(url))
                        p.Imagen1 = url;
                    else
                        return BadRequest("No se pudo subir la imagen. Intente de nuevo.");
                }

                // 🔥 GUARDAR SEGÚN CASO
                if (value.idProducto > 0)
                {
                     services.UpdateProductos(p.IdProducto, p);
                   
                    return Ok(new
                    {
                        message = "Producto actualizado ✅"
                    });
                }
                else
                {
                    await services.InsertProductos(p);
                    
                    return Ok(new
                    {
                        message = "Producto creado ✅"
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT api/<ProductosController>/5
        [HttpPut()]
        public async Task<IActionResult> Put([FromForm] ProductosDto value)
        {
            var Producto =  services.GetProductoById(value.idProducto);
            if (Producto == null)
                return NotFound("Producto no encontrado");
           
            if (value.Imagen != null)
            {
                using var ms = new MemoryStream();
                await value.Imagen.CopyToAsync(ms);
                byte[] imagenBytes = ms.ToArray();
                var fileName = $"{Producto.IdEmpresa}_{Producto.IdProducto}_{Guid.NewGuid():N}.jpg";
                var url = Utility.UploadFileFtp(imagenBytes, fileName);
                if (!string.IsNullOrWhiteSpace(url))
                    Producto.Imagen1 = url;
                else
                    return BadRequest("No se pudo subir la imagen. Intente de nuevo.");
            }




            Producto.Nombre = value.nombre;
            Producto.Descripcion = value.nombre;
            Producto.Cantidad = (decimal)value.cantidad;
            Producto.Stock = (decimal)value.stockminimo;
            Producto.PrecioVenta = (decimal)value.precio;
            Producto.PrecioCompra = (decimal)value.costo;
            Producto.ControlarStock = value.ControlarStock;
            Producto.IdCategoria = value.idcategoria;
            Producto.TipoOperacion = value.TipoOperacion;
            Producto.IdArea = value.IdArea;
            Producto.EsProductoBelleza = value.isproductobelleza;
            Producto.DuracionServicio = (int)value.DuracionServicio;
            Producto.DisponibleEnCitas = (bool)value.DisponibleEnCitas;
            Producto.CodigoBarra = value.CodigoBarra;
            Producto.Itbis = (bool)value.Itbis;
            Producto.EsServicio = (bool)value.EsServicio;

            TipoComportamientoConstantes.AplicarComportamientoErp(
                Producto,
                value.TipoComportamiento);

            if (Producto.EsServicio)
            {
                Producto.ControlarStock = false;
                Producto.Cantidad = 0;
                var tipoPut = TipoComportamientoConstantes.Normalizar(Producto.TipoComportamiento);
                if (tipoPut == TipoComportamientoConstantes.Inventario
                    || tipoPut == TipoComportamientoConstantes.ActivoFijo)
                {
                    Producto.TipoComportamiento = TipoComportamientoConstantes.Servicio;
                }
            }

            services.UpdateProductos(value.idProducto,Producto);
            return Ok(new { message = "Producto actualizado ✅" });
        }
        [HttpGet("ProductosLite/{IdEmpresa}")]
        public async Task<IEnumerable<ProductoLiteDto>> GetLite(int IdEmpresa)
        {
            return await services.GetLite(IdEmpresa);
        }

        // GET api/<ProductosController>/GetServiciosByArea/5/1
        [HttpGet("GetServiciosByArea/{idArea}/{idEmpresa}")]
        public async Task<IEnumerable<Productos>> GetServiciosByArea(int idArea, int idEmpresa)
        {
            return await services.GetServiciosByArea(idArea, idEmpresa);
        }

        // DELETE api/<ProductosController>/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                services.DeleteProductos(id);
                return Ok();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
                when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx
                      && (sqlEx.Number == 547))
            {
                return Conflict(new { message = "No se puede eliminar este producto porque tiene registros asociados (facturas, órdenes, inventario, etc.)." });
            }
        }
    }
}
