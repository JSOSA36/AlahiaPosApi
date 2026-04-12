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

        // POST api/<ProductosController>
        [HttpPost]
        public async Task Post([FromForm] ProductosDto value)
        {

            try
            {


              
                Productos p = new Productos();
                p.Nombre = value.nombre;
                p.Descripcion = value.nombre;
                p.Cantidad = value.cantidad;
                p.Stock = value.stockminimo;
                p.PrecioVenta = value.precio;
                p.PrecioCompra = value.costo;
                p.IdArea = value.IdArea;
                p.DuracionServicio = value.DuracionServicio;
                p.DisponibleEnCitas = value.DisponibleEnCitas;

                p.CodigoBarra = value.CodigoBarra;


                p.Precio1 = 0;
                p.Precio2 = 0;
                p.Precio3 = 0;
                p.PorcientoGanancia = 0;
                p.PorcientoDescuento = 0;
                p.EsServicio = value.EsServicio;
                p.Itbis = value.Itbis ;
                p.Descuento = 0;
                p.IsActivo = true;

                p.SeVende = true;
                p.SeCompra = true;
                p.ControlarStock = true;
                p.FechaVencimiento = DateTime.Now.ToLongDateString();
                p.Ganancia = 0;
                p.IdAlmacen = 1;
                p.IdProveedor = 1;
                p.IdCategoria = value.idcategoria;
                p.IdUnidadMedida = 1;
                p.Rentado = 0;
                p.IdEmpresa = value.IdEmpresa;
                p.ControlarStock = value.ControlarStock;
               
                p.EsProductoBelleza = value.isproductobelleza;
                if (value.Imagen != null)
                {

                    using var ms = new MemoryStream();
                    await value.Imagen.CopyToAsync(ms);
                    byte[] imagenBytes = ms.ToArray();
                    p.Imagen1 = Utility.UploadFileFtp(imagenBytes,
                    +GlobalParamter.IdEmpresa + value.nombre + ".jpg");
                }


                await services.InsertProductos(p);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

            }
        }

        // PUT api/<ProductosController>/5
        [HttpPut()]
        public async Task Put([FromForm] ProductosDto value)
        {
            var Producto =  services.GetProductoById(value.idProducto);
           
            if (value.Imagen != null)
            {

                using var ms = new MemoryStream();
                await value.Imagen.CopyToAsync(ms);
                byte[] imagenBytes = ms.ToArray();
                Producto.Imagen1 = Utility.UploadFileFtp(imagenBytes,
                Guid.NewGuid().ToString()+ ".jpg");
            }




            Producto.Nombre = value.nombre;
            Producto.Descripcion = value.nombre;
            Producto.Cantidad = value.cantidad;
            Producto.Stock = value.stockminimo;
            Producto.PrecioVenta = value.precio;
            Producto.PrecioCompra = value.costo;
            Producto.ControlarStock = value.ControlarStock;
            Producto.IdCategoria = value.idcategoria;
            Producto.IdArea = value.IdArea;
            Producto.EsProductoBelleza = value.isproductobelleza;
            Producto.DuracionServicio = value.DuracionServicio;
            Producto.DisponibleEnCitas = value.DisponibleEnCitas;
            Producto.CodigoBarra = value.CodigoBarra;
            Producto.Itbis = value.Itbis;
            Producto.EsServicio = value.EsServicio;

            services.UpdateProductos(value.idProducto,Producto);
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
        public void Delete(int id)
        {
            services.DeleteProductos(id);
        }
    }
}
