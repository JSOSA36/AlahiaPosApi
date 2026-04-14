using AlahiaPos.DataAccess.Servicios;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AutoMapper;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Identity.Client;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacturaHeaderController : ControllerBase
    {
        IMapper _Mapper;
        IFacturaHeader _facturaHeader;
        IFacturaDetalle _facturaDetalle;
        IProductos _productos;
        IEmpresas _Empresas;
        IEmpleados _IEmpleado;
        IMesas IMesas;
        IValidateIMpuesto validateIMpuesto;
        IClientes _Clientes;
        IIngresos _IngresosServices;
        ICitas _ICita;
        IUsuarios _IUsuarios;
        IPrinterTicket _IPrinter;
        IPagosFacturasClientes _PagoFacturaClientes;
        public FacturaHeaderController(IMapper mapper, IFacturaHeader facturaHeader,
            IFacturaDetalle facturaDetalle,
            IProductos Producto, IMesas iMesas, IValidateIMpuesto validateIMpuesto, IClientes Clientes,
            IEmpresas empresas, IIngresos ingresos, ICitas iCita, IUsuarios usuarios,
            IPagosFacturasClientes pagoFacturaClientes, IEmpleados iEmpleado, IPrinterTicket iPrinter)
        {
            _Mapper = mapper;
            _facturaHeader = facturaHeader;
            _facturaDetalle = facturaDetalle;
            _productos = Producto;
            IMesas = iMesas;
            this.validateIMpuesto = validateIMpuesto;
            this._Clientes = Clientes;
            this._Empresas = empresas;
            this._IngresosServices = ingresos;
            _ICita = iCita;
            _IUsuarios = usuarios;
            _PagoFacturaClientes = pagoFacturaClientes;
            _IEmpleado = iEmpleado;
            _IPrinter = iPrinter;
        }

        // GET: api/<FacturaHeaderController>
        [HttpGet]
        public async Task<IEnumerable<FacturaHeaders>> Get(int IdEmpresa)
        {
            return await _facturaHeader.GetAllFacturaFacturaHeader(IdEmpresa);
        }
        [HttpGet]
        [Route("GetAllOrdenes")]
        public async Task<IEnumerable<FacturaHeaderDto>> GetAllOrdenes(int IdEmpresa)
        {
            var listaReturn = new List<FacturaHeaders>();

            var headers = await _facturaHeader.GetAllOrdenes(IdEmpresa);

            if (headers == null || !headers.Any())
                return new List<FacturaHeaderDto>();

            foreach (var item in headers)
            {
                var detalles = await _facturaDetalle.GetOrdenesByHeader(item.IdFacturaHeader);

                if (detalles == null || !detalles.Any())
                    continue;

                var listaDetalles = new List<FacturaDetalles>();

                foreach (var d in detalles)
                {
                    if (d.StatuItem == false)
                    {
                        // ⭐ cargar producto
                        var producto = await _productos.GetAllProductosById(d.IdProducto);
                        d.Productos = producto;

                        // ⭐ cargar nombre lavador comisión
                        if (d.IdEmpleadoComision != null)
                        {
                            var emp = await _IEmpleado.GetEmpleadoById((int)d.IdEmpleadoComision);
                            d.NombreEmpleadoComision = emp?.Nombre;
                        }
                        else
                        {
                            d.NombreEmpleadoComision = "No asignado";
                        }

                        listaDetalles.Add(d);
                    }
                }

                if (listaDetalles.Any())
                {
                    item.FacturaDetalles = listaDetalles;
                    listaReturn.Add(item);
                }
            }

            return _Mapper.Map<FacturaHeaderDto[]>(listaReturn);
        }
        [HttpGet()]
        [Route("GetAllFacturas")]
        public async Task<IEnumerable<FacturaHeaderDto>> GetAllFacturas(int IdEmpresa)
        {
            var listaReturn = new List<FacturaHeaders>();

            var headers = await _facturaHeader.GetAllFacturas(IdEmpresa);

            if (headers == null || !headers.Any())
                return new List<FacturaHeaderDto>();

            foreach (var item in headers)
            {
                var detalles = await _facturaDetalle.GetOrdenesByHeader(item.IdFacturaHeader);

                if (detalles == null || !detalles.Any())
                    continue;

                var listaDetalles = new List<FacturaDetalles>();

                foreach (var d in detalles)
                {
                    if (d.StatuItem == false)
                    {
                        // ⭐ cargar producto
                        var producto = await _productos.GetAllProductosById(d.IdProducto);
                        d.Productos = producto;

                        // ⭐ cargar nombre lavador comisión
                        if (d.IdEmpleadoComision != null)
                        {
                            var emp = await _IEmpleado.GetEmpleadoById((int)d.IdEmpleadoComision);
                            d.NombreEmpleadoComision = emp?.Nombre;
                        }
                        else
                        {
                            d.NombreEmpleadoComision = "No asignado";
                        }

                        listaDetalles.Add(d);
                    }
                }

                if (listaDetalles.Any())
                {
                    item.FacturaDetalles = listaDetalles;
                    listaReturn.Add(item);
                }
            }

            return _Mapper.Map<FacturaHeaderDto[]>(listaReturn);
        }
        [Route("GetAllFacturaPendiente/{IdCliente}/{IdEmpresa}")]
        [HttpGet]
        public async Task<IEnumerable<FacturaHeaderDto>> GetAllFacturaPendiente(int IdCliente, int IdEmpresa)
        {
            var facturasPendientes = await _facturaHeader.GetAllFacturaPendientes(IdCliente, IdEmpresa);
            var resultado = new List<FacturaHeaders>();

            foreach (var factura in facturasPendientes)
            {
                // 🔹 Solo facturas a crédito pendientes o parciales

                factura.Clientes = await _Clientes.GetAllClientesById((int)factura.IDCliente);
                // 🔹 Cargar detalles de factura
                var detalles = await _facturaDetalle.GetOrdenesByHeader(factura.IdFacturaHeader);
                if (detalles == null || !detalles.Any()) continue;

                var detallesFiltrados = new List<FacturaDetalles>();

                foreach (var det in detalles)
                {
                    if (det.StatuItem == false)
                    {
                        var producto = await _productos.GetAllProductosById(det.IdProducto);
                        det.Productos = producto;
                        detallesFiltrados.Add(det);
                    }
                }

                // 🔹 Solo agregar facturas con items válidos
                if (detallesFiltrados.Count > 0)
                {
                    factura.FacturaDetalles = detallesFiltrados;
                    resultado.Add(factura);
                }
            }

            return _Mapper.Map<FacturaHeaderDto[]>(resultado);
        }

        [HttpGet]
        [Route("GetFactura/{IdFact}")]
        public async Task<IEnumerable<FacturaHeaderDto>> GetFactura(int IdFact)
        {
            List<FacturaHeaders> ReturnLista = new List<FacturaHeaders>();


            var Header = await _facturaHeader.GetAllFactById(IdFact);



            foreach (var item in Header)
            {
                item.Clientes = null;
                item.Clientes = await _Clientes.GetAllClientesById((int)item.IDCliente);
                item.FacturaDetalles = null;
                item.FacturaDetalles = await _facturaDetalle.GetDetalleByIdHeaderAsync(item.IdFacturaHeader);
                List<FacturaDetalles> ListaDetalles = new List<FacturaDetalles>();
                if (item.FacturaDetalles.Count() > 0 && item.FacturaDetalles != null)
                {

                    foreach (var d in item.FacturaDetalles)
                    {

                        var _producto = await _productos.GetAllProductosById(d.IdProducto);
                        d.Productos = _producto;

                        ListaDetalles.Add(d);
                    }

                    item.FacturaDetalles = null;
                    if (ListaDetalles.Count > 0)
                    {
                        item.FacturaDetalles = ListaDetalles;

                        ReturnLista.Add(item);
                    }



                }




            }




            return _Mapper.Map<FacturaHeaderDto[]>(ReturnLista);

        }


        [HttpGet]
        [Route("GetFacturaPdf/{IdFact}")]
        public async Task<IActionResult> GetFacturaPdf(int IdFact)
        {
            var header = await _facturaHeader.GetAllFactById(IdFact);
            var factura = header.FirstOrDefault();

            if (factura == null)
                return NotFound("Factura no encontrada");

            factura.Clientes = await _Clientes.GetAllClientesById((int)factura.IDCliente);
            factura.FacturaDetalles = await _facturaDetalle.GetDetalleByIdHeaderAsync(factura.IdFacturaHeader);

            foreach (var d in factura.FacturaDetalles)
                d.Productos = await _productos.GetAllProductosById(d.IdProducto);

            using (var ms = new MemoryStream())
            {
                var document = new Document(PageSize.A4, 40, 40, 40, 40);
                PdfWriter.GetInstance(document, ms);
                document.Open();

                var fontTitle = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                var fontSubTitle = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                var fontHeader = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE);
                var fontNormal = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                var fontBold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);

                var empresa = await _Empresas.GetEmpresaById(factura.IdEmpresa);

                // ============================
                // ENCABEZADO
                // ============================
                document.Add(new Paragraph(empresa.NombreComercial, fontTitle));
                document.Add(new Paragraph(empresa.Direccion, fontSubTitle));
                document.Add(new Paragraph(empresa.Telefono, fontSubTitle));
                document.Add(new Paragraph(" "));
                document.Add(new LineSeparator());

                // ============================
                // DATOS FACTURA
                // ============================
                document.Add(new Paragraph($"Factura #{factura.IdFacturaHeader}", fontSubTitle));
                document.Add(new Paragraph($"Cliente: {factura.NombreCuenta ?? "Consumidor Final"}", fontSubTitle));
                document.Add(new Paragraph($"Fecha: {factura.FechaInseccion:dd/MM/yyyy HH:mm}", fontSubTitle));
                document.Add(new Paragraph(" "));

                // ============================
                // TABLA DETALLES
                // ============================
                PdfPTable table = new PdfPTable(4) { WidthPercentage = 100 };
                table.SetWidths(new float[] { 4f, 1f, 2f, 2f });

                void AddHeader(string text)
                {
                    var cell = new PdfPCell(new Phrase(text, fontHeader))
                    {
                        BackgroundColor = BaseColor.DARK_GRAY,
                        HorizontalAlignment = Element.ALIGN_CENTER
                    };
                    table.AddCell(cell);
                }

                AddHeader("Producto");
                AddHeader("Cant.");
                AddHeader("Precio");
                AddHeader("Descuento");

                decimal totalDescuentos = 0;

                foreach (var det in factura.FacturaDetalles)
                {
                    decimal precioUnitario = det.PrecioOferta > 0
                        ? det.PrecioOferta
                        : det.Productos.PrecioVenta;

                    decimal descuentoUnit = det.Descuento;              // UNITARIO
                    decimal descuentoTotalItem = descuentoUnit * det.Cantidad;

                    totalDescuentos += descuentoTotalItem;

                    table.AddCell(new Phrase(det.Productos?.Nombre ?? "", fontNormal));
                    table.AddCell(new Phrase(det.Cantidad.ToString("N0"), fontNormal));
                    table.AddCell(new Phrase(precioUnitario.ToString("C"), fontNormal));
                    table.AddCell(new Phrase(
                        descuentoTotalItem > 0 ? descuentoTotalItem.ToString("C") : "-",
                        fontBold
                    ));
                }

                document.Add(table);

                // ============================
                // TOTALES
                // ============================
                document.Add(new Paragraph(" "));

                PdfPTable totales = new PdfPTable(2)
                {
                    WidthPercentage = 50,
                    HorizontalAlignment = Element.ALIGN_RIGHT
                };
                totales.SetWidths(new float[] { 2f, 2f });

                void AddRow(string label, string value)
                {
                    totales.AddCell(new PdfPCell(new Phrase(label, fontBold))
                    {
                        Border = Rectangle.NO_BORDER
                    });

                    totales.AddCell(new PdfPCell(new Phrase(value, fontNormal))
                    {
                        Border = Rectangle.NO_BORDER,
                        HorizontalAlignment = Element.ALIGN_RIGHT
                    });
                }

                AddRow("Subtotal:", factura.SubTotal.ToString("C"));
                AddRow("Descuentos:", totalDescuentos.ToString("C"));
                AddRow("ITBIS:", factura.TotalItbis.ToString("C"));
                AddRow("TOTAL:", factura.Total.ToString("C"));

                document.Add(totales);

                // ============================
                // FOOTER
                // ============================
                document.Add(new Paragraph(" "));
                var footer = new Paragraph("Gracias por su visita", fontSubTitle)
                {
                    Alignment = Element.ALIGN_CENTER
                };
                document.Add(footer);

                document.Close();

                return File(ms.ToArray(), "application/pdf", $"Factura_{IdFact}.pdf");
            }
        }



        // GET api/<FacturaHeaderController>/5
        [HttpGet]
        [Route("GetOrdenes/{IdEmpresa}")]
        public async Task<IEnumerable<FacturaHeaderDto>>
        GetOrdenes(int IdEmpresa)
        {
            var Header =
            await _facturaHeader
            .GetAllFacturaFacturaHeaderByIdMesa(IdEmpresa);

            foreach (var itemheader in Header)
            {
                var _DetalleFact =
                await _facturaDetalle
                .GetOrdenesByHeader(
                    itemheader.IdFacturaHeader,
                    IdEmpresa);

                foreach (var itemdetalle in _DetalleFact)
                {
                    var _Producto =
                    await _productos
                    .GetAllProductosById(
                        itemdetalle.IdProducto);

                    itemdetalle.Productos = _Producto;
                }

                itemheader.FacturaDetalles = _DetalleFact;
            }

            var dto =
            _Mapper.Map<List<FacturaHeaderDto>>(Header);

            foreach (var h in dto)
            {
                foreach (var d in h.FacturaDetalles)
                {
                    if (d.IdEmpleadoComision > 0)
                    {
                        var emp =
                        await _IEmpleado
                        .GetEmpleadoById((int)
                            d.IdEmpleadoComision);

                        d.NombreEmpleadoComision =
                        emp?.Nombre;
                    }
                }
            }

            return dto;
        }

        // POST api/<FacturaHeaderController>
        [HttpPost()]
        public async Task Post([FromBody] FacturaHeaderDto value)
        {
            try

            {
                List<FacturaDetalles> lista = new List<FacturaDetalles>();
                var Header = _Mapper.Map<FacturaHeaders>(value);
                Header.Clientes = null;
                var _GetEmpleado = await _IUsuarios.ObtenerPorId((int)value.IdMoso);
                Header.IdMesa = 1;
                Header.Estado_Orden = "Pendiente";
                Header.FechaInseccion = DateTime.Now.Date;
                Header.IDCliente = value.IDCliente;
                Header.Hora = DateTime.Now.ToString("hh:mm tt");
                Header.PrintPending = false;
                Header.IdEmpleadoComision = 1;
                Header.IdEmpresa = value.IdEmpresa;
                Header.IdEmpleados = _GetEmpleado.IdEmpleado;
                Header.PrintLavador = false;
                Header.IdTipoDocumentos = 10;
                foreach (var item in Header.FacturaDetalles)
                {
                    var _Producto = _productos.GetProductoById(item.IdProducto);

                    item.FechaInseccion = DateTime.Now.Date;
                    item.StatuItem = false;

                    if (item.IdEmpleadoComision == null)
                        item.IdEmpleadoComision = 0;

                    // ============================
                    // 🔥 PRECIOS
                    // ============================
                    decimal precioOriginal = _Producto.PrecioVenta;
                    decimal precioFinal = item.PrecioOferta > 0
                                            ? item.PrecioOferta
                                            : _Producto.PrecioVenta;

                    // ============================
                    // 🔥 DESCUENTO REAL
                    // ============================
                    item.Descuento = (precioOriginal - precioFinal);
                    if (item.Descuento < 0)
                        item.Descuento = 0;

                    // ============================
                    // 🔥 SUBTOTAL FINAL
                    // ============================
                    item.SubTotal = (precioFinal * item.Cantidad) + item.Itbis;

                    item.Productos = null;
                    lista.Add(item);
                }

                Header.FacturaDetalles = lista;

                decimal Total = lista.Sum(c => c.SubTotal);
                decimal TotalIbits = lista.Sum(c => c.Itbis);

                Header.SubTotal = Total - TotalIbits;
                Header.Total = Total;
                Header.PrintAcount = true;
                Header.TotalItbis = TotalIbits;

                await _facturaHeader.InsertFacturaHeader(Header);
            }
            catch (Exception ex)
            {
                // log si quieres
            }
        }
        [HttpPost]
        [Route("ProcesarFactura")]
        public async Task<IActionResult> ProcesarFactura([FromBody] FacturaDirectaDTO dto)
        {
            try
            {
                if (dto == null || dto.Header == null)
                    return BadRequest("Header vacío");

                FacturaHeaders header;

                // ============================================
                // 🔹 CREAR O USAR FACTURA
                // ============================================
                if (dto.Header.IdFacturaHeader == 0)
                {
                    // 🔥 MAPEAR DTO → ENTIDAD
                    header = _Mapper.Map<FacturaHeaders>(dto.Header);

                    // 🔥 evitar validaciones innecesarias
                    header.Empleados = null;
                    header.Clientes = null;

                    header.FechaInseccion = DateTime.Now;
                    header.Estado = "Pendiente";

                    decimal total = 0;
                    decimal totalItbis = 0;

                    foreach (var item in header.FacturaDetalles)
                    {
                        var prod = _productos.GetProductoById(item.IdProducto);

                        decimal precio = item.PrecioOferta > 0
                            ? item.PrecioOferta
                            : prod.PrecioVenta;

                        item.SubTotal = (precio * item.Cantidad) + item.Itbis;
                        if (!item.IdEmpleadoComision.HasValue)
                            item.IdEmpleadoComision = 0;
                        total += item.SubTotal;
                        totalItbis += item.Itbis;

                        item.Productos = null;
                    }

                    header.Total = total;
                    header.TotalItbis = totalItbis;
                    header.SubTotal = total - totalItbis;
                    header.Clientes = null;
                    header.FechaBencimiento = DateTime.Now;
                    header.FechaInseccion= DateTime.Now;
                    header.IdEmpleadoComision = dto.Header.IdMoso;
                    header.IdEmpleados = dto.Header.IdMoso;
                    header.PrintAcount = false;
                    header.IDCliente = 819;
                    header.IdMesa = 1;
                    header.IdTipoDocumentos = 1;
                    await _facturaHeader.InsertFacturaHeader(header);
                    header.IdFacturaHeader = header.IdFacturaHeader;
                }
                else
                {
                    // 🔥 BUSCAR FACTURA EXISTENTE (ORDEN)
                    header = _facturaHeader.GetById(dto.Header.IdFacturaHeader);

                    if (header == null)
                        return NotFound("Factura no encontrada");
                }

                // ============================================
                // 🔹 PROCESAR PAGOS
                // ============================================
                decimal totalPagadoAhora = 0;

                var pagos = dto.Pagos ?? new List<PagoDTO>();

                var pagosAgrupados = pagos
                    .Where(x => x.Monto > 0)
                    .GroupBy(x => x.Metodo)
                    .Select(g => new
                    {
                        Metodo = g.Key,
                        Monto = g.Sum(x => x.Monto)
                    });

                foreach (var pago in pagosAgrupados)
                {
                    totalPagadoAhora += pago.Monto;

                    // 🔥 INGRESOS
                    var existeIngreso = await _IngresosServices.ExisteIngreso(
                        header.IdFacturaHeader,
                        pago.Metodo
                    );

                    if (!existeIngreso)
                    {
                        await _IngresosServices.InsertIngreso(new Ingresos
                        {
                            IdEmpresa = header.IdEmpresa,
                            FechaRegistro = DateTime.Now,
                            Descripcion = $"Factura #{header.IdFacturaHeader}",
                            Categoria = header.TipoFactura == "Contado"
                                ? "Venta de Contado"
                                : "Abono a Crédito",
                            Origen = "Sistema",
                            Monto = pago.Monto,
                            FormaPago = pago.Metodo,
                            Referencia = $"Factura #{header.IdFacturaHeader}",
                            IdFacturaHeader = header.IdFacturaHeader,
                            IdCliente = header.IDCliente
                        });
                    }

                    // 🔥 REGISTRO PAGO CLIENTE
                    await _PagoFacturaClientes.InsertPagosFacturasClientes(
                        new PagosFacturasClientes
                        {
                            IdFacturaHeader = header.IdFacturaHeader,
                            IDCliente = header.IDCliente,
                            FormaPago = pago.Metodo,
                            Monto = pago.Monto
                        });
                }

                // ============================================
                // 🔹 ACTUALIZAR ESTADO
                // ============================================
                header.Pagado += totalPagadoAhora;
                header.Pendiente = header.Total - header.Pagado;

                header.Estado = header.Pendiente > 0
                    ? "Pendiente"
                    : "Pagada";

                 _facturaHeader.UpdateFacturaHeader(header.IdFacturaHeader, header);

                return Ok(new
                {
                    message = "Factura procesada correctamente",
                    idFactura = header.IdFacturaHeader
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost()]
        [Route("InsertFactura")]
        public async Task InsertFactura([FromBody] FacturaHeaderDto value)
        {

            try
            {
                value.FacturaDetalles.ForEach(c =>
                {
                    c.Productos = null;
                });
                var Header = _Mapper.Map<FacturaHeaders>(value);
                Header.PrintPending = true;
                Header.IdEmpresa = value.IdEmpresa;
                await _facturaHeader.InsertFacturaHeader(Header);



            }
            catch (Exception ex)
            {

            }
        }
        [HttpPost()]
        [Route("DesdeCita/{idCita}")]
       
        public async Task<IActionResult> CrearOrdenDesdeCita(int idCita)
        {
            var cita = await _ICita.GetCitaById(idCita);

            if (cita == null)
                return NotFound("Cita no encontrada");

            

            if (cita.IdProducto <= 0)
                return BadRequest("La cita no tiene un servicio válido");

            if (cita.IdEmpleado <= 0)
                return BadRequest("La cita no tiene empleado asignado");

            if (cita.IdCliente == null)
                return BadRequest("La cita no tiene cliente asignado");

            var producto = _productos.GetProductoById(cita.IdProducto);

            if (producto == null)
                return BadRequest("Producto no encontrado");

            // ============================
            // HEADER
            // ============================
            var header = new FacturaHeaders
            {
                IdEmpresa = cita.IdEmpresa,
                IDCliente = cita.IdCliente.Value,
                IdMesa = 1,
                Estado_Orden = "Pendiente",
                FechaInseccion = DateTime.Now.Date,
                Hora = DateTime.Now.ToString("hh:mm tt"),
                PrintPending = false,
                PrintAcount = true,
                IdEmpleadoComision = cita.IdEmpleado,
                NombreCuenta=cita.NombreCliente


            };
            header.Clientes = null;
            // ============================
            // DETALLE
            // ============================
            var detalle = new FacturaDetalles
            {
                IdProducto = producto.IdProducto,
                Cantidad = 1,
                PrecioOferta = 0,
                Itbis = 0,
                FechaInseccion = DateTime.Now.Date,
                IdEmpleadoComision = cita.IdEmpleado,
                StatuItem = false,
                Productos = null
            };

            header.IdMesa = 1;
            header.Estado_Orden = "Pendiente";
            decimal precioOriginal = producto.PrecioVenta;
            decimal precioFinal = detalle.PrecioOferta > 0
                ? detalle.PrecioOferta
                : precioOriginal;

            detalle.Descuento = Math.Max(0, precioOriginal - precioFinal);
            detalle.SubTotal = (precioFinal * detalle.Cantidad) + detalle.Itbis;

            header.FacturaDetalles = new List<FacturaDetalles> { detalle };

            header.TotalItbis = detalle.Itbis;
            header.SubTotal = detalle.SubTotal - detalle.Itbis;
            header.Total = detalle.SubTotal;
            header.Pagado = (decimal)cita.Abono;
            header.Pendiente = header.Total - header.Pagado;
            header.FechaInseccion = DateTime.Now;
            header.FechaBencimiento = DateTime.Now;
            header.IdEmpleados = cita.IdEmpleado;
            header.IdMoso = cita.IdEmpleado;
            header.IdTipoDocumentos = 10;
            // ============================
            // GUARDAR ORDEN
            // ============================
            await _facturaHeader.InsertFacturaHeader(header);

            // ============================
            // ACTUALIZAR CITA
            // ============================
            cita.IdFacturaHeader = header.IdFacturaHeader;
            cita.Estado = "En Curso";

            _ICita.UpdateCita(cita);

            return Ok(new
            {
                message = "Orden creada desde la cita",
                idFactura = header.IdFacturaHeader
            });
        }


        // PUT api/<FacturaHeaderController>/5
        [HttpPut()]
        public async Task Put([FromBody] FacturaHeaderDto facturaheader)
        {
            var result = _Mapper.Map<FacturaHeaders>(facturaheader);

            List<int> IndexDetalle = new List<int>();

            if (result.Estado_Orden != "Preparando")
            {
                foreach (var item in result.FacturaDetalles)
                {
                    var UpdateDetalle = await _facturaDetalle.GetAllFacturaDetalleById(item.IdFacturaDetalle);
                    UpdateDetalle.StatuItem = true;

                    _facturaDetalle.UpdateFacturaDetalle(UpdateDetalle.IdFacturaDetalle, UpdateDetalle);
                }

            }
            var _HeaderUpdate = await _facturaHeader.GetAllFacturaFacturaHeaderById(result.IdFacturaHeader, facturaheader.IdEmpresa);
            _HeaderUpdate.Estado_Orden = result.Estado_Orden;
            //_HeaderUpdate.PrintAcount = true;
            //_HeaderUpdate.FechaInseccion = System.DateTime.Now.Date;
            _facturaHeader.UpdateFacturaHeader(facturaheader.IdFacturaHeader, _HeaderUpdate);
        }
        private void LogPagoIncorrecto(object logData)
        {
            try
            {
                var ruta = Path.Combine(Directory.GetCurrentDirectory(), "logs");

                if (!Directory.Exists(ruta))
                    Directory.CreateDirectory(ruta);

                var archivo = Path.Combine(ruta, $"pagos_error_{DateTime.Now:yyyyMMdd}.txt");

                var contenido = JsonSerializer.Serialize(logData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                System.IO.File.AppendAllText(archivo, contenido + Environment.NewLine + "----------------------" + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error escribiendo log: " + ex.Message);
            }
        }
        [HttpPost]
        [Route("GenerateFacts")]
        public async Task<IActionResult> GenerateFacts([FromBody] FacturaCierreDTO dto)
        {
            decimal totalPagadoAhora = 0;
            var header = _facturaHeader.GetById(dto.IdFactura);
            if (header == null)
                return NotFound($"Factura {dto.IdFactura} no existe");

            // 🔥 Blindaje: evitar reprocesar factura contado ya pagada
            if (header.Estado == "Pagada" && header.TipoFactura == "Contado")
                return BadRequest("La factura ya fue procesada anteriormente.");

            header.Clientes = null;
            header.PrintPending = dto.imprimirFactura;

            // ============================================
            // 🔹 CONTADO
            // ============================================
            if (dto.TipoFactura == "Contado")
            {
               

                if (dto.DetallePagos != null && dto.DetallePagos.Any())
                {
                    var pagosAgrupados = dto.DetallePagos
                        .Where(x => x.Monto > 0)
                        .GroupBy(x => x.Metodo)
                        .Select(g => new
                        {
                            Metodo = g.Key,
                            Monto = g.Sum(x => x.Monto)
                        });

                    foreach (var pago in pagosAgrupados)
                    {
                        totalPagadoAhora += pago.Monto;

                        var ingreso = new Ingresos
                        {
                            IdEmpresa = header.IdEmpresa,
                            FechaRegistro = DateTime.Now,
                            Descripcion = $"Venta - Factura #{header.IdFacturaHeader}",
                            Categoria = "Venta de Contado",
                            Origen = "Venta mostrador",
                            Monto = pago.Monto,
                            FormaPago = pago.Metodo,
                            Referencia = $"Factura #{header.IdFacturaHeader}",
                            IdFacturaHeader = header.IdFacturaHeader,
                            IdCliente = header.IDCliente
                        };


                        var existeIngreso = await _IngresosServices.ExisteIngreso(
                        header.IdFacturaHeader,
                        pago.Metodo
                       
                        );

                        if (!existeIngreso)
                        {
                            await _IngresosServices.InsertIngreso(ingreso);
                        }
                        

                        var pagoCliente = new PagosFacturasClientes
                        {
                            IdFacturaHeader = header.IdFacturaHeader,
                            NumeroDocumento = header.NumeroDocumento,
                            IDCliente = header.IDCliente,
                            FormaPago = pago.Metodo,
                            Monto = pago.Monto,
                            Nota = "Pago Inicial a Factura"
                        };

                        await _PagoFacturaClientes.InsertPagosFacturasClientes(pagoCliente);
                    }
                }

                if (totalPagadoAhora > 0)
                {
                    header.Pagado = totalPagadoAhora;
                    header.Pendiente = 0;
                    header.Estado = "Pagada";
                }
                else
                {
                    header.Pagado = 0;
                    header.Pendiente = header.Total;
                    header.Estado = "Pendiente";
                }

                header.TipoFactura = "Contado";
            }

            // ============================================
            // 🔹 CRÉDITO
            // ============================================
            else if (dto.TipoFactura == "Credito")
            {
                decimal totalAbonadoAhora = 0;

                if (dto.DetalleAbono != null && dto.DetalleAbono.Any())
                {
                    var pagosAgrupados = dto.DetalleAbono
                        .Where(x => x.Monto > 0)
                        .GroupBy(x => x.Metodo)
                        .Select(g => new
                        {
                            Metodo = g.Key,
                            Monto = g.Sum(x => x.Monto)
                        });

                    foreach (var pago in pagosAgrupados)
                    {
                        if (pago.Monto > header.Pendiente)
                            return BadRequest("El monto ingresado excede el pendiente de la factura.");

                        totalAbonadoAhora += pago.Monto;

                        var ingreso = new Ingresos
                        {
                            IdEmpresa = header.IdEmpresa,
                            FechaRegistro = DateTime.Now,
                            Descripcion = $"Abono - Factura #{header.IdFacturaHeader}",
                            Categoria = "Abono a Crédito",
                            Origen = "Cliente crédito",
                            Monto = pago.Monto,
                            FormaPago = pago.Metodo,
                            Referencia = $"Factura #{header.IdFacturaHeader}",
                            IdFacturaHeader = header.IdFacturaHeader,
                            IdCliente = header.IDCliente
                        };

                        await _IngresosServices.InsertIngreso(ingreso);

                        var pagoCliente = new PagosFacturasClientes
                        {
                            IdFacturaHeader = header.IdFacturaHeader,
                            NumeroDocumento = header.NumeroDocumento,
                            IDCliente = header.IDCliente,
                            FormaPago = pago.Metodo,
                            Monto = pago.Monto,
                            Nota = "Abono a Factura"
                        };

                        await _PagoFacturaClientes.InsertPagosFacturasClientes(pagoCliente);
                    }
                    // 🚨 LOG INTELIGENTE
                    if (dto.TipoFactura == "Contado" && totalPagadoAhora < header.Total)
                    {
                        var log = new
                        {
                            Fecha = DateTime.Now,
                            Factura = header.IdFacturaHeader,
                            TotalFactura = header.Total,
                            TotalRecibido = totalPagadoAhora,
                            Diferencia = header.Total - totalPagadoAhora,
                            Pagos = dto.DetallePagos,
                            Cliente = header.IDCliente,
                            Nota = "🚨 Pago menor al total detectado"
                        };

                        LogPagoIncorrecto(log);
                    }
                }

                header.Pagado += totalAbonadoAhora;
                header.Pendiente = header.Total - header.Pagado;
                header.TipoFactura = "Credito";
                header.Estado = header.Pendiente > 0 ? "Pendiente" : "Pagada";
            }

            header.FechaInseccion = DateTime.Now;
            header.IdTipoDocumentos = 1;

            _facturaHeader.UpdateFacturaHeader(header.IdFacturaHeader, header);

            // ============================================
            // 🔹 COMPLETAR CITA
            // ============================================
            //var cita = await _ICita.GetCitaByIdFactHeader(header.IdFacturaHeader);
            //if (cita != null)
            //{
            //    cita.Estado = "Completada";
            //    _ICita.UpdateCita(cita);
            //}

            //await _IPrinter.GenerateTicketLavador(header.IdFacturaHeader);

            //if (header.PrintPending == true)
            //    await _IPrinter.GenerateTicketFacturaCliente(header.IdFacturaHeader);

            return Ok(new { message = "Factura generada correctamente." });
        }
        [HttpGet("ticket-lavador/{idFactura}")]
        public async Task<IActionResult> GetTicketLavador(int idFactura)
        {
            var tickets = await _facturaHeader.GetTicketsLavadorByFactura(idFactura);

            if (tickets == null || !tickets.Any())
                return NotFound("No hay tickets para esta factura");

            return Ok(tickets);
        }
        [HttpGet("factura-cliente/{idFactura}")]
        public async Task<IActionResult> GetFacturaCliente(int idFactura)
        {
            var factura = await _facturaHeader.GetFacturaClienteById(idFactura);

            if (factura == null)
                return NotFound("Factura no encontrada");

            return Ok(factura);
        }
        // DELETE api/<FacturaHeaderController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarFactura(int id)
        {
            var ok = await _facturaHeader.EliminarFacturaCompleta(id);

            if (!ok)
                return NotFound();

            return NoContent();
        }
        [HttpGet]
        [Route("CuentaxCobrar")]
        public async Task<decimal> CuentaxCobrar(int IdEmpresa)
        {
            return await _facturaHeader.GetTotalFacturaPorCobrar(IdEmpresa);
        }
        [HttpPost]
        [Route("AnularFactura")]
        public async Task<IActionResult> AnularFactura(int IdFact, int idEmpresa)
        {
            var factura = await _facturaHeader.GetFacturaHeaderById(IdFact, idEmpresa);

            if (factura == null)
                return NotFound("Factura no existe");

            if (factura.EstaCancelada)
                return BadRequest("Factura ya está anulada");

            // 🔴 marcar cancelada
            factura.EstaCancelada = true;
            factura.FechaInseccion = DateTime.Now;
            factura.Clientes = null;
            _facturaHeader.UpdateFacturaHeader(IdFact, factura);

            // 🔴 revertir ingresos
            await _IngresosServices.RevertirIngresoPorFactura(IdFact, idEmpresa);

            return Ok();
        }
        [HttpGet]
        [Route("TotalVentaDia")]
        public async Task<decimal> TotalVentaDia(int IdEmpresa)
        {
            return await _facturaHeader.GetVentaDelDia(IdEmpresa);
        }
        [HttpGet]
        [Route("CierreCaja/{_Desde}/{_Hasta}/{IdEmpresa}")]
        public List<CierreCajaDto> CierreCaja(DateTime _Desde, DateTime _Hasta, int IdEmpresa)
        {
            List<CierreCajaDto> _Listado = new List<CierreCajaDto>();
            CierreCajaDto c = new CierreCajaDto();
            c.FormaPago = "Efectivo";
            c.Total = _facturaHeader.GetMontoEfectivo(_Desde, _Hasta, IdEmpresa);
            _Listado.Add(c);
            c = new CierreCajaDto();
            c.FormaPago = "Tarjeta";
            c.Total = _facturaHeader.GetMontoTarjeta(_Desde, _Hasta, IdEmpresa);
            _Listado.Add(c);
            c = new CierreCajaDto();
            c.FormaPago = "Transferencia";
            c.Total = _facturaHeader.GetMontoTransferencia(_Desde, _Hasta, IdEmpresa);
            _Listado.Add(c);

            return _Listado;
        }
        [HttpGet]
        [Route("GetSumFactura")]
        public async Task<List<HistoricoVentaDto>> GetSumFactura(int IdEmpresa)
        {
            var result = await _facturaHeader.GetSumFactura(IdEmpresa);
            return result;
        }
        [HttpGet]
        [Route("RePrintLavador/{IdFact}")]
        public async Task<IActionResult> RePrintLavador(int IdFact)
        {
            await  _IPrinter.GenerateTicketLavador(IdFact);

            return Ok("Ticket lavador enviado nuevamente a cola");
        }
        [HttpGet()]
        [Route("GetFacturasXCobrar")]
        public async Task<IEnumerable<FacturaHeaderDto>> GetFacturasXCobrar(DateTime? Desde,
            DateTime? Hasta, int IdCliente, int IdEmpresa)
        {
            var result = _Mapper.Map<FacturaHeaderDto[]>(await
                _facturaHeader.GetFacturasXCobrar(Desde,
            Hasta, IdCliente, IdEmpresa));


            return result;
        }
        [HttpGet]
        [Route("TotalVentaMes")]
        public async Task<decimal> TotalVentaMes(int IdEmpresa)
        {
            return await _facturaHeader.GetTotalFactura(IdEmpresa);
        }
        [HttpGet]
        [Route("GetComisiones/{_Desde}/{_Hasta}/{IdEmpresa}")]
        public IEnumerable<ComisionesResultDto> GetComisiones(DateTime _Desde, DateTime _Hasta, int IdEmpresa)
        {
            IEnumerable<ComisionesResultDto> Lista = new List<ComisionesResultDto>();

            try
            {
                Lista = _facturaHeader.GetComisionesDetalle(_Desde, _Hasta, IdEmpresa);
            }
            catch (Exception ex)
            {

            }
            
            return Lista;
        }
        /// <summary>
        /// Obtiene los servicios realizados por empleados en un rango de fechas.
        /// </summary>
        /// <param name="desde">Fecha inicial (yyyy-MM-dd)</param>
        /// <param name="hasta">Fecha final (yyyy-MM-dd)</param>
        /// <param name="idEmpresa">Id de la empresa</param>
        [HttpGet("GetServiciosPorEmpleado")]
        public ActionResult<IEnumerable<ServicioEmpleadoDto>> GetServiciosPorEmpleado(
            DateTime desde,
            DateTime hasta,
            int idEmpresa)
        {
            if (desde > hasta)
                return BadRequest("La fecha 'desde' no puede ser mayor que 'hasta'.");

            var result = _facturaHeader.GetServicioByEMpleados(desde, hasta, idEmpresa);

            if (result == null)
                return NotFound("No se encontraron registros en el rango indicado.");

            return Ok(result);
        }

        [HttpGet("TopServicios/{idEmpresa}")]
        public async Task<IActionResult> GetTopServiciosDelMes(int idEmpresa)
        {
            try
            {
                var top = await _facturaHeader.GetTopServiciosDelMes(idEmpresa);
                return Ok(top);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Error al obtener el Top de servicios del mes",
                    error = ex.Message
                });
            }
        }

     
    [HttpGet("GetCuentasPorCobrar/{idEmpresa}")]
        public async Task<IActionResult> GetCuentasPorCobrar(int idEmpresa)
        {
            try
            {
                var result = await _facturaHeader.GetCuentasPorCobrar(idEmpresa);

                if (result == null || !result.Any())
                    return NotFound("No hay cuentas por cobrar registradas para esta empresa.");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Error al obtener las cuentas por cobrar",
                    error = ex.Message
                });
            }
        }

    } 
}

