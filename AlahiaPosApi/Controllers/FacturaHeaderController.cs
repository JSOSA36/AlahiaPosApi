using AlahiaPos.DataAccess.Data;
using AlahiaPos.DataAccess.Servicios;
using AlahiaPos.DataAccess.Servicios.Ventas;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Dto.Invoice;
using AlahiaPos.Entities.Interfaces;
using AlahiaPosApi.Auth;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Google.Apis.Util;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

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
        
        IEmpresas _Empresas;
        IEmpleados _IEmpleado;
        IMesas IMesas;
        IValidateIMpuesto validateIMpuesto;
        IClientes _Clientes;
        IIngresos _IngresosServices;
        ICitas _ICita;
        IUsuarios _IUsuarios;
        IPrinterTicket _IPrinter;
        private readonly INCF_Secuencias _INCF_Secuencias;
        IProductos _Productos;
        IPagosFacturasClientes _PagoFacturaClientes;
        IMetodoPagoCuentaService _MetodoPagoCuentaService;
        IMovimientoFinancieroService _MovimientoFinancieroService;
        private readonly ISecuenciaDocumentoService _secuenciaDocumentoService;
        IMovimientosInventarioService _movimientosInventario;
        private readonly IFiscalWorkEnqueueService _fiscalEnqueue;
        private readonly IProduccionPosAdapter _produccionPosAdapter;
        private readonly IContabilidadEventPublisher _contabilidadEvents;
        private readonly INotasCredito _notasCredito;
        private readonly IReporte607Service _reporte607;
        private readonly AlahiaPosContext _context;
        private readonly ILogger<FacturaHeaderController> _logger;
        private readonly ISucursalService _sucursales;
        private readonly ISesionTokenResolver _tokens;
        private readonly ICargoPagoService _cargosPago;
        private readonly IArsAseguradoraService _arsAseguradora;
        public FacturaHeaderController(IMapper mapper, IFacturaHeader facturaHeader,
            IFacturaDetalle facturaDetalle,IProductos productos,
            IProductos Producto, IMesas iMesas, IValidateIMpuesto validateIMpuesto, IClientes Clientes,
            IEmpresas empresas, IIngresos ingresos, ICitas iCita, IUsuarios usuarios,
            IPagosFacturasClientes pagoFacturaClientes,
            INCF_Secuencias INCF_Secuencias,
            IEmpleados iEmpleado, 
            IPrinterTicket iPrinter,
            ISecuenciaDocumentoService
            secuenciaDocumentoService,
            
            IMetodoPagoCuentaService MetodoPagoCuentaService,
            IMovimientoFinancieroService MovimientoFinancieroService,
            IMovimientosInventarioService movimientosInventario,
            IFiscalWorkEnqueueService fiscalEnqueue,
            IProduccionPosAdapter produccionPosAdapter,
            IContabilidadEventPublisher contabilidadEvents,
            INotasCredito notasCredito,
            IReporte607Service reporte607,
            AlahiaPosContext context,
            ILogger<FacturaHeaderController> logger,
            ISucursalService sucursales,
            ISesionTokenResolver tokens,
            ICargoPagoService cargosPago,
            IArsAseguradoraService arsAseguradora
          

            )
        {

            _MetodoPagoCuentaService = MetodoPagoCuentaService;
            _MovimientoFinancieroService= MovimientoFinancieroService;
            _Mapper = mapper;
            _facturaHeader = facturaHeader;
            _facturaDetalle = facturaDetalle;
            _Productos = Producto;
            _INCF_Secuencias = INCF_Secuencias;
             IMesas = iMesas;
            this.validateIMpuesto = validateIMpuesto;
            this._Clientes = Clientes;
            _movimientosInventario = movimientosInventario;
            this._Empresas = empresas;
            this._IngresosServices = ingresos;
            _ICita = iCita;
            _IUsuarios = usuarios;
            _PagoFacturaClientes = pagoFacturaClientes;
            _IEmpleado = iEmpleado;
            _IPrinter = iPrinter;
            _secuenciaDocumentoService = secuenciaDocumentoService;
            _fiscalEnqueue = fiscalEnqueue;
            _produccionPosAdapter = produccionPosAdapter;
            _contabilidadEvents = contabilidadEvents;
            _notasCredito = notasCredito;
            _reporte607 = reporte607;
            _context = context;
            _logger = logger;
            _sucursales = sucursales;
            _tokens = tokens;
            _cargosPago = cargosPago;
            _arsAseguradora = arsAseguradora;
        }

        // GET: api/<FacturaHeaderController>
        [HttpGet]
        public async Task<IEnumerable<FacturaHeaders>> Get(int IdEmpresa)
        {
            return await _facturaHeader.GetAllFacturaFacturaHeader(IdEmpresa);
        }
        [HttpGet]
        [Route("GetAllOrdenes")]
        public async Task<IActionResult> GetAllOrdenes(int IdEmpresa, int? idSucursalFiltro = null)
        {
            try
            {
                var (scope, error) = await SucursalConsultaHttp.ResolverAsync(
                    HttpContext, _tokens, _sucursales, IdEmpresa, idSucursalFiltro);
                if (error != null)
                    return error;

                var headers = await _facturaHeader.GetAllOrdenes(IdEmpresa);
                headers = (headers ?? Enumerable.Empty<FacturaHeaders>())
                    .Where(h => scope.Incluye(h.IdSucursal))
                    .ToList();
                var mapped = (await MapListadoOrdenesAsync(headers, IdEmpresa)).ToList();
                AsignarNombresSucursal(mapped, scope);
                return Ok(mapped);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllOrdenes empresa {Empresa}", IdEmpresa);
                return StatusCode(500, new { message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpGet]
        [Route("GetAllCotizaciones")]
        public async Task<IActionResult> GetAllCotizaciones(int IdEmpresa, int? idSucursalFiltro = null)
        {
            var (scope, error) = await SucursalConsultaHttp.ResolverAsync(
                HttpContext, _tokens, _sucursales, IdEmpresa, idSucursalFiltro);
            if (error != null)
                return error;

            var headers = await _facturaHeader.GetAllCotizaciones(IdEmpresa);
            headers = (headers ?? Enumerable.Empty<FacturaHeaders>())
                .Where(h => scope.Incluye(h.IdSucursal))
                .ToList();
            var mapped = (await MapListadoOrdenesAsync(headers, IdEmpresa)).ToList();
            AsignarNombresSucursal(mapped, scope);
            return Ok(mapped);
        }

        /// <summary>
        /// Estado de secuencias internas (Fact-, PFACT-, COT-) para el POS local.
        /// No incrementa; el POS reserva el siguiente número offline.
        /// </summary>
        [HttpGet]
        [Route("secuencias-documento/{idEmpresa}")]
        public async Task<IActionResult> GetSecuenciasDocumento(int idEmpresa)
        {
            if (idEmpresa <= 0)
                return BadRequest("IdEmpresa inválido");

            var lista = await _secuenciaDocumentoService.ListarPorEmpresaAsync(idEmpresa);
            return Ok(lista);
        }

        /// <summary>
        /// Arma el listado con productos ya incluidos y nombres de empleado en un solo pase.
        /// Evita N+1 (detalle/producto/empleado por cada línea).
        /// </summary>
        private async Task<IEnumerable<FacturaHeaderDto>> MapListadoOrdenesAsync(
            IEnumerable<FacturaHeaders> headers,
            int idEmpresa)
        {
            var lista = headers?.ToList() ?? new List<FacturaHeaders>();
            if (lista.Count == 0)
                return Array.Empty<FacturaHeaderDto>();

            var empleados = (await _IEmpleado.GetAllEmpleados(idEmpresa) ?? Enumerable.Empty<Empleados>())
                .GroupBy(e => e.IdEmpleados)
                .ToDictionary(g => g.Key, g => g.First().Nombre);

            var prodIds = lista
                .SelectMany(h => h.FacturaDetalles ?? Enumerable.Empty<FacturaDetalles>())
                .Select(d => d.IdProducto)
                .Where(id => id > 0)
                .Distinct()
                .ToHashSet();

            var productos = new Dictionary<int, Productos>();
            if (prodIds.Count > 0)
            {
                productos = (await _Productos.GetAllProductos(idEmpresa) ?? Enumerable.Empty<Productos>())
                    .Where(p => prodIds.Contains(p.IdProducto))
                    .GroupBy(p => p.IdProducto)
                    .ToDictionary(g => g.Key, g => g.First());
            }

            var resultado = new List<FacturaHeaders>(lista.Count);
            foreach (var item in lista)
            {
                try
                {
                    var detalles = (item.FacturaDetalles ?? Enumerable.Empty<FacturaDetalles>())
                        .Where(d => d.StatuItem == false)
                        .ToList();

                    foreach (var d in detalles)
                    {
                        d.FacturaHeader = null;
                        if (d.Productos == null && productos.TryGetValue(d.IdProducto, out var prod))
                        {
                            // Listado: no adjuntar el producto completo (imágenes / colecciones).
                            // Una orden del celular con foto grande o grafo circular tumbaba GetAllOrdenes.
                            d.Productos = new Productos
                            {
                                IdProducto = prod.IdProducto,
                                IdEmpresa = prod.IdEmpresa,
                                Nombre = prod.Nombre,
                                PrecioVenta = prod.PrecioVenta,
                                Itbis = prod.Itbis
                            };
                        }
                        else if (d.Productos != null)
                        {
                            d.Productos.MovimientosInventarioDetalle = new List<MovimientosInventarioDetalle>();
                            d.Productos.Imagen1 = null;
                            d.Productos.Imagen2 = null;
                            d.Productos.Imagen3 = null;
                        }

                        if (d.IdEmpleadoComision is int idEmp &&
                            empleados.TryGetValue(idEmp, out var nombre) &&
                            !string.IsNullOrWhiteSpace(nombre))
                        {
                            d.NombreEmpleadoComision = nombre;
                        }
                        else
                        {
                            d.NombreEmpleadoComision = "No asignado";
                        }
                    }

                    item.FacturaDetalles = detalles;
                    item.Empleados = null;
                    item.Mesas = null;
                    resultado.Add(item);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo armar la orden {Id} de empresa {Empresa}",
                        item.IdFacturaHeader, idEmpresa);
                    item.FacturaDetalles ??= new List<FacturaDetalles>();
                    item.Empleados = null;
                    item.Mesas = null;
                    resultado.Add(item);
                }
            }

            try
            {
                return _Mapper.Map<FacturaHeaderDto[]>(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MapListadoOrdenes batch falló empresa {Empresa}", idEmpresa);
                var dtos = new List<FacturaHeaderDto>(resultado.Count);
                foreach (var item in resultado)
                {
                    try
                    {
                        dtos.Add(_Mapper.Map<FacturaHeaderDto>(item));
                    }
                    catch (Exception exItem)
                    {
                        _logger.LogWarning(exItem, "Orden {Id} no se pudo mapear en empresa {Empresa}",
                            item.IdFacturaHeader, idEmpresa);
                    }
                }
                return dtos;
            }
        }

        private static void AsignarNombresSucursal(
            IEnumerable<FacturaHeaderDto> mapped,
            SucursalConsultaScope scope)
        {
            foreach (var f in mapped)
            {
                var id = f.IdSucursal is > 0 ? f.IdSucursal.Value : scope.IdPrincipal;
                f.NombreSucursal = scope.Sucursales
                    .FirstOrDefault(s => s.IdSucursal == id)?.Nombre;
            }
        }

        private async Task AsignarNombresUsuarioAsync(
            IEnumerable<FacturaHeaderDto> mapped,
            int idEmpresa)
        {
            var lista = mapped?.ToList() ?? new List<FacturaHeaderDto>();
            if (lista.Count == 0)
                return;

            var ids = lista
                .Where(f => f.IdUsuario is > 0)
                .Select(f => f.IdUsuario!.Value)
                .Distinct()
                .ToList();
            if (ids.Count == 0)
                return;

            var usuarios = await _context.Usuarios.AsNoTracking()
                .Where(u => u.IdEmpresa == idEmpresa && ids.Contains(u.IdUsuario))
                .Select(u => new { u.IdUsuario, u.UserName, u.Correo })
                .ToListAsync();

            var dict = usuarios.ToDictionary(
                u => u.IdUsuario,
                u => !string.IsNullOrWhiteSpace(u.Correo)
                    ? u.Correo!.Trim()
                    : (u.UserName ?? "").Trim());

            foreach (var f in lista)
            {
                if (f.IdUsuario is > 0 && dict.TryGetValue(f.IdUsuario.Value, out var nombre)
                    && !string.IsNullOrWhiteSpace(nombre))
                {
                    f.NombreUsuario = nombre;
                }
            }
        }

        [HttpGet()]
        [Route("GetAllFacturas")]
        public async Task<IActionResult> GetAllFacturas(int IdEmpresa, int? idSucursalFiltro = null)
        {
            var (scope, error) = await SucursalConsultaHttp.ResolverAsync(
                HttpContext, _tokens, _sucursales, IdEmpresa, idSucursalFiltro);
            if (error != null)
                return error;

            var listaReturn = new List<FacturaHeaders>();

            var headers = (await _facturaHeader.GetAllFacturas(IdEmpresa)
                ?? Enumerable.Empty<FacturaHeaders>())
                .Where(h => scope.Incluye(h.IdSucursal))
                .ToList();

            if (headers.Count == 0)
                return Ok(new List<FacturaHeaderDto>());

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
                        var producto = await _Productos.GetAllProductosById(d.IdProducto);
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

            var mapped = _Mapper.Map<FacturaHeaderDto[]>(listaReturn);
            await AsignarNombresUsuarioAsync(mapped, IdEmpresa);
            AsignarNombresSucursal(mapped, scope);
            return Ok(mapped);
        }
        [Route("GetAllFacturaPendiente/{IdCliente}/{IdEmpresa}")]
        [HttpGet]
        public async Task<IActionResult> GetAllFacturaPendiente(
            int IdCliente,
            int IdEmpresa,
            int? idSucursalFiltro = null)
        {
            var (scope, error) = await SucursalConsultaHttp.ResolverAsync(
                HttpContext, _tokens, _sucursales, IdEmpresa, idSucursalFiltro);
            if (error != null)
                return error;

            var facturasPendientes = (await _facturaHeader.GetAllFacturaPendientes(IdCliente, IdEmpresa)
                ?? Enumerable.Empty<FacturaHeaders>())
                .Where(h => scope.Incluye(h.IdSucursal))
                .ToList();
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
                        var producto = await _Productos.GetAllProductosById(det.IdProducto);
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

            var mappedPendiente = _Mapper.Map<FacturaHeaderDto[]>(resultado);
            AsignarNombresSucursal(mappedPendiente, scope);
            return Ok(mappedPendiente);
        }

        [HttpGet]
        [Route("GetFactura/{IdFact}")]
        public async Task<IActionResult> GetFactura(int IdFact)
        {
            List<FacturaHeaders> ReturnLista = new List<FacturaHeaders>();

            var Header = (await _facturaHeader.GetAllFactById(IdFact))?.ToList()
                         ?? new List<FacturaHeaders>();
            var primero = Header.FirstOrDefault();
            if (primero == null)
                return NotFound();
            if (!TenantRecurso.EsDeLaSesion(HttpContext, primero.IdEmpresa))
                return NotFound();

            foreach (var item in Header)
            {
                item.Clientes = null;
                if (item.IDCliente.HasValue && item.IDCliente.Value > 0)
                {
                    try
                    {
                        item.Clientes = await _Clientes.GetAllClientesById(item.IDCliente.Value);
                    }
                    catch
                    {
                        item.Clientes = null;
                    }
                }

                item.FacturaDetalles = null;
                var detalles = await _facturaDetalle.GetDetalleByIdHeaderAsync(item.IdFacturaHeader);
                var listaDetalles = new List<FacturaDetalles>();

                if (detalles != null)
                {
                    foreach (var d in detalles)
                    {
                        try
                        {
                            d.Productos = await _Productos.GetAllProductosById(d.IdProducto);
                        }
                        catch
                        {
                            d.Productos = null;
                        }

                        listaDetalles.Add(d);
                    }
                }

                item.FacturaDetalles = listaDetalles;
                ReturnLista.Add(item);
            }

            var dtos = _Mapper.Map<FacturaHeaderDto[]>(ReturnLista);
            foreach (var dto in dtos)
                await AplicarEmisorSucursalAsync(dto);

            if (primero.IdEmpresa > 0)
                await AsignarNombresUsuarioAsync(dtos, primero.IdEmpresa);

            return Ok(dtos);
        }


        [HttpGet]
        [Route("GetFacturaPdf/{IdFact}")]
        public async Task<IActionResult> GetFacturaPdf(int IdFact)
        {
            var header = await _facturaHeader.GetAllFactById(IdFact);
            var factura = header.FirstOrDefault();

            if (factura == null)
                return NotFound("Factura no encontrada");
            if (!TenantRecurso.EsDeLaSesion(HttpContext, factura.IdEmpresa))
                return NotFound("Factura no encontrada");

            factura.Clientes = await _Clientes.GetAllClientesById((int)factura.IDCliente);
            factura.FacturaDetalles = await _facturaDetalle.GetDetalleByIdHeaderAsync(factura.IdFacturaHeader);

            foreach (var d in factura.FacturaDetalles)
                d.Productos = await _Productos.GetAllProductosById(d.IdProducto);

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
                Sucursal? sucursal = null;
                if (factura.IdSucursal is > 0)
                {
                    sucursal = await _context.Sucursales.AsNoTracking()
                        .FirstOrDefaultAsync(s =>
                            s.IdSucursal == factura.IdSucursal.Value
                            && s.IdEmpresa == factura.IdEmpresa);
                }

                // ============================
                // ENCABEZADO
                // ============================
                document.Add(new Paragraph(empresa?.NombreComercial ?? "", fontTitle));
                var nombreSucursal = DocumentoSucursalContacto.Nombre(sucursal);
                if (!string.IsNullOrWhiteSpace(nombreSucursal))
                    document.Add(new Paragraph(nombreSucursal, fontSubTitle));
                var direccionEmisor = DocumentoSucursalContacto.FormatearDireccion(sucursal, empresa);
                if (!string.IsNullOrWhiteSpace(direccionEmisor))
                    document.Add(new Paragraph(direccionEmisor, fontSubTitle));
                var telefonoEmisor = DocumentoSucursalContacto.Telefono(sucursal, empresa);
                if (!string.IsNullOrWhiteSpace(telefonoEmisor))
                    document.Add(new Paragraph($"Tel: {telefonoEmisor}", fontSubTitle));
                if (!string.IsNullOrWhiteSpace(empresa?.RNC))
                    document.Add(new Paragraph($"RNC: {empresa.RNC}", fontSubTitle));
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
                    await _Productos
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
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] FacturaHeaderDto value)
        {
            try
            {
                // Editar orden/cotización: actualizar en el mismo Id.
                // No borrar el documento: se pierde el número y los artículos que no vengan en el payload.
                if (value.IdFacturaHeader > 0)
                {
                    var existente = _facturaHeader.GetById(value.IdFacturaHeader);
                    if (existente == null)
                        return NotFound("La orden no existe.");
                    if (value.IdEmpresa > 0 && existente.IdEmpresa != value.IdEmpresa)
                        return BadRequest("La orden no pertenece a esta empresa.");
                    if (!TenantRecurso.EsDeLaSesion(HttpContext, existente.IdEmpresa))
                        return NotFound("La orden no existe.");
                    if (existente.IdTipoDocumentos != 2 && existente.IdTipoDocumentos != 10)
                        return BadRequest("Solo se pueden editar órdenes o cotizaciones.");
                    if (existente.EstaCancelada)
                        return BadRequest("No se puede editar una orden cancelada.");

                    return await ActualizarOrdenExistenteAsync(value, existente);
                }

                int? origenIdAnterior = null;

                // =========================================
                // 🔥 MAPEAR HEADER
                // =========================================

                List<FacturaDetalles> lista =
                    new List<FacturaDetalles>();

                var Header =
                    _Mapper.Map<FacturaHeaders>(value);

                Header.IdFacturaHeader = 0;

                Header.Clientes = null;
                Header.Empleados = null;

                var idUsuarioSesion = await ResolverIdUsuarioCobroAsync(value.IdUsuario);
                if (idUsuarioSesion <= 0)
                    return BadRequest(new { message = "No se pudo identificar el usuario de la orden." });

                var idMoso = value.IdMoso is > 0 && value.IdMoso != 1
                    ? value.IdMoso.Value
                    : idUsuarioSesion;
                var _GetEmpleado = idMoso > 0
                    ? await _IUsuarios.ObtenerPorId(idMoso)
                    : null;

                Header.IdMesa = value.IdMesa > 0 ? value.IdMesa : 1;
                Header.Comensales = value.Comensales is > 0 ? value.Comensales : null;

                Header.Estado_Orden =
                    "Pendiente";

                Header.TipoOrden =
                    string.IsNullOrWhiteSpace(value.TipoOrden)
                        ? ""
                        : value.TipoOrden.Trim();

                Header.FechaInseccion =
                    DateTime.Now.Date;

                if (Header.FechaBencimiento.Year < 1753)
                    Header.FechaBencimiento = DateTime.Now.Date;

                Header.IDCliente =
                    value.IDCliente is > 0 ? value.IDCliente : null;

                Header.Hora =
                    DateTime.Now.ToString(
                        "hh:mm tt"
                    );

                Header.PrintPending = false;
                Header.EstaCerrada = false;
                Header.EstaCancelada = false;

                Header.IdEmpleadoComision = 1;

                Header.IdEmpresa =
                    value.IdEmpresa;

                AplicarSucursalDeSesion(Header, value.IdSucursal);

                Header.IdEmpleados =
                    _GetEmpleado?.IdEmpleado ?? 1;

                Header.PrintLavador = false;
                Header.IdUsuario = idUsuarioSesion;
                Header.IdMoso = idMoso > 0 ? idMoso : idUsuarioSesion;
                Header.IdTipoDocumentos =
                    value.IdTipoDocumentos is 2 or 10
                        ? value.IdTipoDocumentos
                        : 10;

                // =========================================
                // 🔥 DETALLES
                // =========================================

                Header.FacturaDetalles ??= new List<FacturaDetalles>();

                foreach (var item in Header.FacturaDetalles)
                {
                    if (item.IdProducto <= 0)
                        continue;

                    var _Producto =
                        _Productos.GetProductoById(
                            item.IdProducto
                        );

                    if (_Producto == null)
                        continue;

                    item.IdFacturaDetalle = 0;

                    item.FechaInseccion =
                        DateTime.Now.Date;

                    item.StatuItem = false;

                    if (item.IdEmpleadoComision == null)
                        item.IdEmpleadoComision = 0;

                    // ============================
                    // 🔥 PRECIOS
                    // ============================

                    decimal precioOriginal =
                        _Producto.PrecioVenta;

                    decimal precioFinal =
                        item.PrecioOferta > 0
                            ? item.PrecioOferta
                            : _Producto.PrecioVenta;

                    // ============================
                    // 🔥 DESCUENTO REAL
                    // ============================

                    item.Descuento =
                        (precioOriginal - precioFinal);

                    if (item.Descuento < 0)
                        item.Descuento = 0;

                    // ============================
                    // 🔥 SUBTOTAL FINAL
                    // ============================

                    var itbisLinea = ItbisPosLinea.ExtenderSiEsUnitario(
                        item.Itbis,
                        (precioFinal * item.Cantidad) + item.Itbis,
                        item.Cantidad);
                    item.Itbis = itbisLinea;
                    item.SubTotal =
                        (precioFinal * item.Cantidad)
                        + itbisLinea;

                    item.Productos = null;

                    lista.Add(item);
                }

                Header.FacturaDetalles = null;

                // =========================================
                // 🔥 TOTALES
                // =========================================

                decimal Total =
                    lista.Sum(c => c.SubTotal);

                decimal TotalIbits =
                    lista.Sum(c => c.Itbis);

                Header.SubTotal =
                    Total - TotalIbits;

                Header.PrintAcount = true;

                Header.TotalItbis =
                    TotalIbits;

                Header.TotalDescuento =
                    value.TotalDescuento < 0
                        ? 0
                        : value.TotalDescuento;

                if (Header.TotalDescuento > Total)
                    Header.TotalDescuento = Total;

                Header.Total =
                    Total - Header.TotalDescuento;

                Header.NumeroDocumento =
                    await AsignarNumeroDocumentoAsync(
                            Header.IdEmpresa,
                            (int)Header.IdTipoDocumentos,
                            value.NumeroDocumento);

                // =========================================
                // 🔥 INSERTAR
                // =========================================

                await _facturaHeader
                    .InsertFacturaHeader(Header);

                _context.ChangeTracker.Clear();

                foreach (var item in lista)
                {
                    item.IdFacturaHeader = Header.IdFacturaHeader;
                    item.IdEmpresa = Header.IdEmpresa;
                    item.FacturaHeader = null;
                    item.Productos = null;
                    _facturaDetalle.InsertFactDNoasync(item);
                }

                Header.FacturaDetalles = lista;

                // Centro de Producción: post-commit aislado (nunca tumba la orden)
                try
                {
                    await _produccionPosAdapter.PublicarOrdenSiAplicaAsync(Header, origenIdAnterior);
                }
                catch
                {
                    // Aislamiento extra; el adapter ya captura internamente
                }

                return Ok(new
                {
                    idFacturaHeader = Header.IdFacturaHeader,
                    numeroDocumento = Header.NumeroDocumento,
                    totalDescuento = Header.TotalDescuento,
                    total = Header.Total
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar orden empresa {Empresa}", value?.IdEmpresa);
                return StatusCode(500, new { message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        /// <summary>
        /// Edita una orden/cotización en el mismo IdFacturaHeader.
        /// Conserva número de documento, fecha original y líneas que el cliente ya tenía
        /// cuando el payload trae el carrito completo (POS).
        /// </summary>
        private async Task<IActionResult> ActualizarOrdenExistenteAsync(
            FacturaHeaderDto value,
            FacturaHeaders existente)
        {
            var lista = PrepararDetallesOrden(value);

            var viejos = _facturaDetalle
                .GetDetalleByIdHeader(existente.IdFacturaHeader)
                ?.ToList() ?? new List<FacturaDetalles>();

            foreach (var d in viejos)
                _facturaDetalle.DeleteFacturaDetalle(d.IdFacturaDetalle);

            foreach (var item in lista)
            {
                item.IdFacturaDetalle = 0;
                item.IdFacturaHeader = existente.IdFacturaHeader;
                item.IdEmpresa = existente.IdEmpresa;
                item.FacturaHeader = null;
                item.Productos = null;
                _facturaDetalle.InsertFactDNoasync(item);
            }

            var nuevos = _facturaDetalle
                .GetDetalleByIdHeader(existente.IdFacturaHeader)
                ?.ToList() ?? new List<FacturaDetalles>();

            decimal total = nuevos.Sum(c => c.SubTotal);
            decimal totalItbis = nuevos.Sum(c => c.Itbis);

            existente.SubTotal = total - totalItbis;
            existente.TotalItbis = totalItbis;
            existente.TotalDescuento = value.TotalDescuento < 0 ? 0 : value.TotalDescuento;
            if (existente.TotalDescuento > total)
                existente.TotalDescuento = total;
            existente.Total = total - existente.TotalDescuento;
            existente.PrintPending = false;
            existente.PrintAcount = true;

            if (value.IDCliente is > 0)
                existente.IDCliente = value.IDCliente;
            if (!string.IsNullOrWhiteSpace(value.NombreCuenta))
            {
                existente.NombreCuenta = value.NombreCuenta;
                existente.Nota = value.NombreCuenta;
            }
            if (!string.IsNullOrWhiteSpace(value.TipoOrden))
                existente.TipoOrden = value.TipoOrden.Trim();
            if (value.IdMesa > 0)
                existente.IdMesa = value.IdMesa;

            existente.Clientes = null;
            existente.FacturaDetalles = null;
            existente.Empleados = null;
            existente.Mesas = null;

            if (TryIdUsuarioSesion(out var idUsuarioSesion))
            {
                existente.IdUsuario = idUsuarioSesion;
                existente.IdMoso = idUsuarioSesion;
            }

            _facturaHeader.UpdateFacturaHeader(existente.IdFacturaHeader, existente);

            existente.FacturaDetalles = nuevos;
            try
            {
                await _produccionPosAdapter.PublicarOrdenSiAplicaAsync(
                    existente,
                    existente.IdFacturaHeader);
            }
            catch
            {
                // Aislado: no tumbar la edición de la orden
            }

            return Ok(new
            {
                idFacturaHeader = existente.IdFacturaHeader,
                numeroDocumento = existente.NumeroDocumento,
                totalDescuento = existente.TotalDescuento,
                total = existente.Total
            });
        }

        private List<FacturaDetalles> PrepararDetallesOrden(FacturaHeaderDto value)
        {
            var mapped = _Mapper.Map<FacturaHeaders>(value);
            mapped.FacturaDetalles ??= new List<FacturaDetalles>();
            var lista = new List<FacturaDetalles>();

            foreach (var item in mapped.FacturaDetalles)
            {
                if (item.IdProducto <= 0)
                    continue;

                var producto = _Productos.GetProductoById(item.IdProducto);
                if (producto == null)
                    continue;

                item.IdFacturaDetalle = 0;
                item.FechaInseccion = DateTime.Now.Date;
                item.StatuItem = false;
                if (item.IdEmpleadoComision == null)
                    item.IdEmpleadoComision = 0;

                decimal precioOriginal = producto.PrecioVenta;
                decimal precioFinal = item.PrecioOferta > 0
                    ? item.PrecioOferta
                    : producto.PrecioVenta;

                item.Descuento = precioOriginal - precioFinal;
                if (item.Descuento < 0)
                    item.Descuento = 0;

                var itbisLinea = ItbisPosLinea.ExtenderSiEsUnitario(
                    item.Itbis,
                    (precioFinal * item.Cantidad) + item.Itbis,
                    item.Cantidad);
                item.Itbis = itbisLinea;
                item.SubTotal = (precioFinal * item.Cantidad) + itbisLinea;
                item.Productos = null;
                item.FacturaHeader = null;
                lista.Add(item);
            }

            return lista;
        }

        int GetIdTipoDocumento(string tipo)
        {
            switch (tipo?.ToLower())
            {
                case "factura": return 1;
                case "cotizacion": return 2;
                case "orden": return 3;   // 🔥 Pedido en BD = Orden en front
                default: return 1;
            }
        }

        private static string NormalizarTipoFactura(string? tipoFactura, string? tipoPago)
        {
            var raw = $"{tipoFactura}|{tipoPago}".ToUpperInvariant();
            if (raw.Contains("CREDITO"))
                return "Credito";
            return "Contado";
        }

        /// <summary>
        /// Documentos e-CF no aplican cargo de cobro (ej. Sabor Urbano / María).
        /// </summary>
        private static bool EsDocumentoEcfParaCargo(FacturaHeaderDto header)
        {
            var tipoComprobante = (header.TipoComprobante ?? string.Empty).Trim();
            return header.EsComprobanteElectronico
                   || tipoComprobante.Equals("Crédito Fiscal", StringComparison.OrdinalIgnoreCase)
                   || tipoComprobante.Equals("Consumidor Final", StringComparison.OrdinalIgnoreCase)
                   || tipoComprobante.Equals("Gubernamental", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Crédito: usa FechaBencimiento enviada o calcula desde Plazo (días).
        /// Contado: vencimiento = fecha de factura.
        /// </summary>
        private static void AplicarPlazoYVencimiento(FacturaHeaders header, FacturaHeaderDto dto)
        {
            var esCredito = string.Equals(header.TipoFactura, "Credito", StringComparison.OrdinalIgnoreCase);
            var baseFecha = header.FechaInseccion == default ? DateTime.Today : header.FechaInseccion.Date;

            if (!esCredito)
            {
                header.FechaBencimiento = baseFecha;
                header.Plazo = string.IsNullOrWhiteSpace(dto.Plazo) ? "0 días" : dto.Plazo;
                return;
            }

            if (!string.IsNullOrWhiteSpace(dto.Plazo))
                header.Plazo = dto.Plazo.Trim();

            if (dto.FechaBencimiento != default && dto.FechaBencimiento.Year > 2000)
            {
                header.FechaBencimiento = dto.FechaBencimiento.Date;
                return;
            }

            var dias = ExtraerDiasPlazo(dto.Plazo);
            header.FechaBencimiento = baseFecha.AddDays(dias);
            if (string.IsNullOrWhiteSpace(header.Plazo))
                header.Plazo = $"{dias} día{(dias == 1 ? "" : "s")}";
        }

        private static int ExtraerDiasPlazo(string? plazo)
        {
            if (string.IsNullOrWhiteSpace(plazo))
                return 0;

            var digits = new string(plazo.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out var dias) && dias >= 0 ? dias : 0;
        }

        /// <summary>
        /// Conserva el número ya impreso en el POS si no está ocupado; si no, genera el siguiente.
        /// </summary>
        private async Task<string> AsignarNumeroDocumentoAsync(
            int idEmpresa,
            int idTipoDocumento,
            string? propuesto)
        {
            var n = (propuesto ?? "").Trim();
            var esReservado = n.Length > 0
                && !n.StartsWith("LOC-", StringComparison.OrdinalIgnoreCase);

            if (esReservado)
            {
                var existe = await _context.FacturaHeaders.AsNoTracking()
                    .AnyAsync(x =>
                        x.IdEmpresa == idEmpresa
                        && x.NumeroDocumento == n);
                if (!existe)
                {
                    return await _secuenciaDocumentoService.ConfirmarReservadoAsync(
                        idEmpresa,
                        idTipoDocumento,
                        n);
                }
            }

            return await _secuenciaDocumentoService.GenerarDocumentoAsync(
                idEmpresa,
                idTipoDocumento);
        }

        [HttpPost]
        [Route("ProcesarFactura")]
        public async Task<IActionResult> ProcesarFactura([FromBody] FacturaDirectaDTO dto)
        {
            FacturaHeaders? header = null;
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                if (dto == null || dto.Header == null)
                    return BadRequest("Header vacío");

                var idUsuarioCobro = await ResolverIdUsuarioCobroAsync(dto.Header.IdUsuario);
                if (idUsuarioCobro <= 0)
                    return BadRequest(new { message = "No se pudo identificar el usuario del cobro." });

                var claveIdempotencia = ProcesarFacturaIntegridad.NormalizarClaveIdempotencia(dto.IdempotencyKey);
                if (claveIdempotencia.Length > 0)
                {
                    var idPrevio = await ProcesarFacturaIntegridad.BuscarFacturaPorClaveIdempotenciaAsync(
                        _context, dto.Header.IdEmpresa, claveIdempotencia);
                    if (idPrevio is > 0)
                    {
                        header = _facturaHeader.GetById(idPrevio.Value)
                            ?? throw new Exception("Documento no encontrado.");
                        var printIdempotente = await ConstruirFacturaPrintAsync(header);
                        await tx.CommitAsync();
                        return Ok(new
                        {
                            message = "Factura procesada correctamente",
                            idFactura = header.IdFacturaHeader,
                            factura = printIdempotente,
                            idempotente = true
                        });
                    }
                }

                if (dto.Header.IdFacturaHeader == 0)
                {
                    header = _Mapper.Map<FacturaHeaders>(dto.Header);

                    header.Empleados = null;
                    header.Clientes = null;
                    header.FechaInseccion = DateTime.Now;
                    header.Estado = "Pendiente";
                    header.IDCliente = dto.Header.IDCliente;
                    header.TotalDescuento = dto.Header.TotalDescuento < 0 ? 0 : dto.Header.TotalDescuento;
                    header.TipoFactura = NormalizarTipoFactura(
                        dto.Header.TipoFactura,
                        dto.Header.TipoPago);
                    header.IdUsuario = idUsuarioCobro;
                    header.IdMoso = idUsuarioCobro;
                    // Cobro siempre genera factura (tipo 1), nunca orden/cotización.
                    header.IdTipoDocumentos = 1;
                    header.TipoOrden = dto.Header.TipoOrden;
                    AplicarPlazoYVencimiento(header, dto.Header);

                    // =====================================================
                    // 🔥 NCF / eNCF
                    // =====================================================
                    var esEcf = dto.Header.TipoComprobante == "Crédito Fiscal"
                             || dto.Header.TipoComprobante == "Consumidor Final"
                             || dto.Header.TipoComprobante == "Gubernamental";

                    if (!esEcf && dto.Header.TipoComprobante != "FACT")
                    {
                        header.NCF = await 
                            _INCF_Secuencias.GenerarNCF(dto.Header.IdEmpresa, dto.Header.TipoComprobante);
                    }
                    else
                    {
                        header.NCF = "";
                    }

                    // Número interno: si el POS ya lo imprimió (offline), se conserva.
                    header.NumeroDocumento = await AsignarNumeroDocumentoAsync(
                        header.IdEmpresa,
                        1,
                        dto.Header.NumeroDocumento);

                    header.IdEmpleadoComision = dto.Header.IdMoso;
                    header.IdEmpleados = dto.Header.IdMoso;
                    header.IdEmpleadoConsumo = dto.Header.IdEmpleadoConsumo is > 0
                        ? dto.Header.IdEmpleadoConsumo
                        : null;
                    header.PorcentajeDescuentoEmpleado = dto.Header.PorcentajeDescuentoEmpleado;
                    header.CargarConsumoNomina = dto.Header.CargarConsumoNomina
                        && header.IdEmpleadoConsumo is > 0;
                    header.IdUsuario = idUsuarioCobro;
                    header.IdMoso = idUsuarioCobro;
                    AplicarSucursalDeSesion(header, dto.Header.IdSucursal);
                    header.PrintAcount = false;
                    header.IdMesa = 1;
                    header.RNC = dto.Header.RNC;
                    header.NombreEmpresa = dto.Header.NombreEmpresa;
                    header.NombreCuenta = !string.IsNullOrWhiteSpace(dto.Header.NombreCuenta)
                        ? dto.Header.NombreCuenta
                        : dto.Header.NombreEmpresa;
                    if (claveIdempotencia.Length > 0)
                        header.ClaveIdempotenciaVenta = claveIdempotencia;

                    decimal total = 0;
                    decimal totalItbis = 0;

                    foreach (var item in header.FacturaDetalles)
                    {
                        var prod = _Productos.GetProductoById(item.IdProducto);
                        if (prod == null)
                            return BadRequest($"Producto {item.IdProducto} no encontrado.");

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

                    if (header.TotalDescuento > 0)
                    {
                        header.SubTotal = Math.Max(0, header.SubTotal - header.TotalDescuento);
                        header.Total = header.SubTotal + header.TotalItbis;
                    }

                    await _facturaHeader.InsertFacturaHeader(header);
                }
                else
                {
                    // Cotización / orden existente → convertir a factura sincronizando el carrito actual
                    header = _facturaHeader.GetById(dto.Header.IdFacturaHeader)
                        ?? throw new Exception("Documento no encontrado.");

                    await ProcesarFacturaIntegridad.BloquearFacturaAsync(
                        _context, header.IdFacturaHeader);

                    if (await ProcesarFacturaIntegridad.VentaYaConfirmadaAsync(
                        _context, header.IdFacturaHeader))
                    {
                        var printExistente = await ConstruirFacturaPrintAsync(header);
                        await tx.CommitAsync();
                        return Ok(new
                        {
                            message = "Factura procesada correctamente",
                            idFactura = header.IdFacturaHeader,
                            factura = printExistente,
                            idempotente = true
                        });
                    }

                    if (dto.Header.FacturaDetalles == null || dto.Header.FacturaDetalles.Count == 0)
                        return BadRequest("Sin líneas para facturar. Agrega productos al carrito.");

                    switch (dto.Header.IdTipoDocumentos)
                    {
                        case 1: // Ya es factura
                        case 2: // Cotización → factura
                        case 10: // Orden → factura
                            header.IdTipoDocumentos = 1;
                            break;

                        default:
                            throw new Exception(
                                "Este documento no puede convertirse en factura.");
                    }

                    header.FechaInseccion = DateTime.Now;
                    header.IdUsuario = idUsuarioCobro;
                    header.IdMoso = idUsuarioCobro;
                    AplicarSucursalDeSesion(header, dto.Header.IdSucursal);
                    header.TipoFactura = NormalizarTipoFactura(
                        dto.Header.TipoFactura,
                        dto.Header.TipoPago);
                    AplicarPlazoYVencimiento(header, dto.Header);

                    header.TotalDescuento = dto.Header.TotalDescuento < 0 ? 0 : dto.Header.TotalDescuento;
                    header.RNC = dto.Header.RNC;
                    header.NombreEmpresa = dto.Header.NombreEmpresa;
                    header.NombreCuenta = !string.IsNullOrWhiteSpace(dto.Header.NombreCuenta)
                        ? dto.Header.NombreCuenta
                        : dto.Header.NombreEmpresa;
                    if (dto.Header.IDCliente.HasValue && dto.Header.IDCliente.Value > 0)
                        header.IDCliente = dto.Header.IDCliente;
                    header.IdEmpleadoConsumo = dto.Header.IdEmpleadoConsumo is > 0
                        ? dto.Header.IdEmpleadoConsumo
                        : header.IdEmpleadoConsumo;
                    header.PorcentajeDescuentoEmpleado = dto.Header.PorcentajeDescuentoEmpleado
                        ?? header.PorcentajeDescuentoEmpleado;
                    header.CargarConsumoNomina = dto.Header.CargarConsumoNomina
                        && (header.IdEmpleadoConsumo is > 0);
                    if (claveIdempotencia.Length > 0)
                        header.ClaveIdempotenciaVenta = claveIdempotencia;

                    var esEcf2 = dto.Header.TipoComprobante == "Crédito Fiscal"
                              || dto.Header.TipoComprobante == "Consumidor Final"
                              || dto.Header.TipoComprobante == "Gubernamental";

                    if (!esEcf2 && dto.Header.TipoComprobante != "FACT")
                    {
                        header.NCF =
                            await _INCF_Secuencias.GenerarNCF(
                                header.IdEmpresa,
                                dto.Header.TipoComprobante);
                    }

                    header.NumeroDocumento =
                        await _secuenciaDocumentoService
                            .GenerarDocumentoAsync(
                                header.IdEmpresa,
                                1);

                    // =====================================================
                    // Sync detalles: reemplazar líneas BD con el carrito POS
                    // =====================================================
                    var detallesPrevios = _facturaDetalle
                        .GetDetalleByIdHeader(header.IdFacturaHeader)
                        ?.ToList() ?? new List<FacturaDetalles>();

                    foreach (var prev in detallesPrevios)
                        _facturaDetalle.DeleteFacturaDetalle(prev.IdFacturaDetalle);

                    decimal total = 0;
                    decimal totalItbis = 0;
                    var nuevosDetalles = new List<FacturaDetalles>();

                    foreach (var itemDto in dto.Header.FacturaDetalles)
                    {
                        var prod = _Productos.GetProductoById(itemDto.IdProducto);
                        if (prod == null)
                            return BadRequest($"Producto {itemDto.IdProducto} no encontrado.");

                        decimal precio = itemDto.PrecioOferta > 0
                            ? itemDto.PrecioOferta
                            : prod.PrecioVenta;

                        var itbisUnitario = itemDto.Itbis < 0 ? 0 : itemDto.Itbis;
                        var itbis = ItbisPosLinea.ExtenderSiEsUnitario(
                            itbisUnitario,
                            (precio * itemDto.Cantidad) + itbisUnitario,
                            itemDto.Cantidad);
                        var detalle = new FacturaDetalles
                        {
                            IdFacturaDetalle = 0,
                            IdFacturaHeader = header.IdFacturaHeader,
                            IdProducto = itemDto.IdProducto,
                            Cantidad = itemDto.Cantidad,
                            PrecioOferta = precio,
                            Descuento = itemDto.Descuento < 0 ? 0 : itemDto.Descuento,
                            Itbis = itbis,
                            IdEmpleadoComision = itemDto.IdEmpleadoComision ?? 0,
                            IdEmpresa = header.IdEmpresa,
                            FechaInseccion = DateTime.Now,
                            SubTotal = (precio * itemDto.Cantidad) + itbis,
                            Comentario = itemDto.Comentario ?? "",
                            TipoMasa = itemDto.TipoMasa,
                            TipoRelleno = itemDto.TipoRelleno,
                            Libras = itemDto.Libras,
                            Productos = null,
                            FacturaHeader = null
                        };

                        total += detalle.SubTotal;
                        totalItbis += detalle.Itbis;
                        nuevosDetalles.Add(detalle);
                    }

                    await _facturaDetalle.InsertFactDetalleRange(nuevosDetalles);

                    header.Total = total;
                    header.TotalItbis = totalItbis;
                    header.SubTotal = total - totalItbis;

                    if (header.TotalDescuento > 0)
                    {
                        header.SubTotal = Math.Max(0, header.SubTotal - header.TotalDescuento);
                        header.Total = header.SubTotal + header.TotalItbis;
                    }

                    // Pendiente se recalcula tras pagos; estado provisional
                    header.Pendiente = header.Total - header.Pagado;
                    header.Estado = header.Pendiente > 0 ? "Pendiente" : "Pagada";
                    header.FacturaDetalles = null;
                }

                // Para el comportamiento de María:
                // - Si el documento es e-CF (Crédito Fiscal / Consumidor Final / Gubernamental),
                //   el cargo NO aplica.
                // - En el resto de casos, se aplica por método de pago según reglas.
                var omitirCargoPorEcf = EsDocumentoEcfParaCargo(dto.Header);

                var metodosParaCargo = omitirCargoPorEcf
                    ? Enumerable.Empty<string?>()
                    : (dto.Pagos ?? new List<PagoDTO>())
                        .Where(x => x.Monto > 0 && !FormaPagoArs.EsArs(x.Metodo))
                        .Select(x => x.Metodo);

                await _cargosPago.AplicarEnFacturaAsync(header, metodosParaCargo);
                

                // =====================================================
                // 🔥 PAGOS
                // =====================================================

                decimal totalPagadoAhora = 0;

                var pagos = dto.Pagos ?? new List<PagoDTO>();

                var pagosNc = pagos
                    .Where(x => x.Monto > 0 && FormaPagoNotaCredito.EsNotaCredito(x.Metodo))
                    .ToList();

                var pagosArs = pagos
                    .Where(x => x.Monto > 0 && FormaPagoArs.EsArs(x.Metodo))
                    .ToList();

                var pagosAgrupados = pagos
                    .Where(x => x.Monto > 0
                        && !FormaPagoNotaCredito.EsNotaCredito(x.Metodo)
                        && !FormaPagoArs.EsArs(x.Metodo))
                    .GroupBy(x => x.Metodo)
                    .Select(g => new
                    {
                        Metodo = g.Key,
                        Monto = g.Sum(x => x.Monto)
                    })
                    .ToList();

                var metodosPago = pagosAgrupados
                    .Select(x => x.Metodo)
                    .Concat(pagosNc.Select(_ => FormaPagoNotaCredito.Metodo))
                    .Concat(pagosArs.Select(_ => FormaPagoArs.Metodo))
                    .Distinct()
                    .ToList();

                header.FormaPago = metodosPago.Count == 1
                    ? metodosPago.First()
                    : (metodosPago.Count > 1 ? "Mixto" : header.FormaPago);

                var coberturaArsProyectada = pagosArs.Sum(p => p.Monto);
                var proyectadoPagadoAhora =
                    pagosAgrupados.Sum(p => p.Monto) + pagosNc.Sum(p => p.Monto);
                var pendienteProyectado =
                    header.Total - header.Pagado - proyectadoPagadoAhora - coberturaArsProyectada;

                // Contado subpagado → crédito antes de registrar ingresos/histórico
                if (string.Equals(header.TipoFactura, "Contado", StringComparison.OrdinalIgnoreCase)
                    && pendienteProyectado > 0.02m)
                {
                    if ((header.IDCliente == null || header.IDCliente <= 0)
                        && header.IdEmpleadoConsumo is not > 0)
                    {
                        return BadRequest(
                            "Para dejar saldo pendiente debe indicar un cliente o un colaborador (la factura pasa a crédito).");
                    }

                    header.TipoFactura = "Credito";
                    var diasPlazo = ExtraerDiasPlazo(header.Plazo);
                    if (diasPlazo <= 0)
                        diasPlazo = 30;
                    header.Plazo = $"{diasPlazo} día{(diasPlazo == 1 ? "" : "s")}";
                    header.FechaBencimiento = header.FechaInseccion.Date.AddDays(diasPlazo);
                }

                var esAbonoInicialCredito =
                    string.Equals(header.TipoFactura, "Credito", StringComparison.OrdinalIgnoreCase)
                    && proyectadoPagadoAhora > 0.009m;

                static string NotaPrimerAbono(decimal monto, string metodo) =>
                    $"Primer abono de RD$ {monto:N2} — {metodo}";

                // Nota de crédito / saldo a favor (sin entrada de banco)
                foreach (var pagoNc in pagosNc)
                {
                    await _notasCredito.ConsumirSaldoAFavorEnVentaAsync(
                        header.IdEmpresa,
                        header.IdFacturaHeader,
                        header.IDCliente ?? 0,
                        pagoNc.Monto,
                        pagoNc.IdSaldoAFavor,
                        pagoNc.IdNotaCredito,
                        pagoNc.NcfNotaCredito,
                        header.IdUsuario);

                    totalPagadoAhora += pagoNc.Monto;
                    header.MontoNotaCredito = Math.Round(header.MontoNotaCredito + pagoNc.Monto, 2);

                    var notaNc = esAbonoInicialCredito
                        ? NotaPrimerAbono(pagoNc.Monto, FormaPagoNotaCredito.Metodo)
                        : $"Aplicación NC {pagoNc.NcfNotaCredito}";

                    var existeIngresoNc = await _IngresosServices.ExisteIngreso(
                        header.IdFacturaHeader,
                        FormaPagoNotaCredito.Metodo);
                    if (!existeIngresoNc)
                    {
                        await _IngresosServices.InsertIngreso(new Ingresos
                        {
                            IdEmpresa = header.IdEmpresa,
                            FechaRegistro = DateTime.Now,
                            Descripcion = esAbonoInicialCredito
                                ? $"{NotaPrimerAbono(pagoNc.Monto, FormaPagoNotaCredito.Metodo)} — Factura #{header.IdFacturaHeader}"
                                : $"Factura #{header.IdFacturaHeader} — NC {pagoNc.NcfNotaCredito}",
                            Categoria = esAbonoInicialCredito
                                ? "Abono a Crédito"
                                : "Aplicación Nota de Crédito",
                            Origen = "Sistema",
                            Monto = pagoNc.Monto,
                            FormaPago = FormaPagoNotaCredito.Metodo,
                            Referencia = pagoNc.NcfNotaCredito ?? $"NC-{pagoNc.IdNotaCredito}",
                            IdFacturaHeader = header.IdFacturaHeader,
                            IdCliente = header.IDCliente,
                            Nota = notaNc
                        });
                    }

                    await _PagoFacturaClientes.InsertPagosFacturasClientes(
                        new PagosFacturasClientes
                        {
                            IdFacturaHeader = header.IdFacturaHeader,
                            NumeroDocumento = header.NumeroDocumento ?? "",
                            IDCliente = header.IDCliente,
                            FormaPago = FormaPagoNotaCredito.Metodo,
                            Monto = pagoNc.Monto,
                            Nota = notaNc
                        });
                }

                await _arsAseguradora.AplicarCoberturaEnVentaAsync(
                    header,
                    pagos,
                    esAbonoInicialCredito);

                foreach (var pago in pagosAgrupados)
                {
                    totalPagadoAhora += pago.Monto;

                    var notaPago = esAbonoInicialCredito
                        ? NotaPrimerAbono(pago.Monto, pago.Metodo)
                        : null;

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
                            Descripcion = esAbonoInicialCredito
                                ? $"{NotaPrimerAbono(pago.Monto, pago.Metodo)} — Factura #{header.IdFacturaHeader}"
                                : $"Factura #{header.IdFacturaHeader}",
                            Categoria = header.TipoFactura == "Contado"
                                ? "Venta de Contado"
                                : "Abono a Crédito",
                            Origen = "Sistema",
                            Monto = pago.Monto,
                            FormaPago = pago.Metodo,
                            Referencia = $"Factura #{header.IdFacturaHeader}",
                            IdFacturaHeader = header.IdFacturaHeader,
                            IdCliente = header.IDCliente,
                            Nota = notaPago
                        });
                    }

                    var metodoConfigurado = await _MetodoPagoCuentaService
                        .GetByMetodoAsync(header.IdEmpresa, pago.Metodo);

                    if (metodoConfigurado != null &&
                        metodoConfigurado.IdCuentaFinanciera > 0)
                    {
                        await _MovimientoFinancieroService.RegistrarEntradaAsync(
                            header.IdEmpresa,
                            header.IdUsuario ?? header.IdEmpleados ?? 0,
                            metodoConfigurado.IdCuentaFinanciera,
                            pago.Monto,
                            $"Factura #{header.IdFacturaHeader}",
                            esAbonoInicialCredito
                                ? $"Primer abono ({pago.Metodo}) — Factura #{header.IdFacturaHeader}"
                                : $"Ingreso automático desde ventas ({pago.Metodo})",
                            categoria: esAbonoInicialCredito ? "COBRO_CXC" : "VENTA",
                            referenciaId: header.IdFacturaHeader,
                            referenciaTipo: "FACTURA",
                            claveIdempotencia: ProcesarFacturaIntegridad.ClaveTesoreria(
                                header.IdFacturaHeader, pago.Metodo)
                        );
                    }

                    await _PagoFacturaClientes.InsertPagosFacturasClientes(
                        new PagosFacturasClientes
                        {
                            IdFacturaHeader = header.IdFacturaHeader,
                            NumeroDocumento = header.NumeroDocumento ?? "",
                            IDCliente = header.IDCliente,
                            FormaPago = pago.Metodo,
                            Monto = pago.Monto,
                            Nota = notaPago
                        });
                }

                header.Pagado += totalPagadoAhora;
                FormaPagoArs.RecalcularSaldos(header);

                // Defensa: Contado con saldo no debe quedar como Contado
                if (string.Equals(header.TipoFactura, "Contado", StringComparison.OrdinalIgnoreCase)
                    && header.Pendiente > 0.02m)
                {
                    if ((header.IDCliente == null || header.IDCliente <= 0)
                        && header.IdEmpleadoConsumo is not > 0)
                    {
                        return BadRequest(
                            "Para dejar saldo pendiente debe indicar un cliente o un colaborador (la factura pasa a crédito).");
                    }

                    header.TipoFactura = "Credito";
                    var diasPlazo = ExtraerDiasPlazo(header.Plazo);
                    if (diasPlazo <= 0)
                        diasPlazo = 30;
                    header.Plazo = $"{diasPlazo} día{(diasPlazo == 1 ? "" : "s")}";
                    header.FechaBencimiento = header.FechaInseccion.Date.AddDays(diasPlazo);
                    FormaPagoArs.RecalcularSaldos(header);
                }
                header.Clientes = null;
                header.TipoOrden = "";

                _facturaHeader.UpdateFacturaHeader(header.IdFacturaHeader, header);

                // =====================================================
                // 🔥 MOVIMIENTO INVENTARIO
                // =====================================================

                var movimientoInventario = new MovimientosInventario
                {
                    TipoMovimiento = "SALIDA",
                    Motivo = "VENTA",
                    Referencia = ProcesarFacturaIntegridad.ReferenciaInventario(header.IdFacturaHeader),
                    Observacion = "Salida automática por venta",
                    Fecha = DateTime.Now,
                    IdEmpresa = header.IdEmpresa,
                    IdSucursal = header.IdSucursal,
                    IdUsuario = header.IdUsuario,
                    Activo = true,
                    Detalles = new List<MovimientosInventarioDetalle>()
                };

                var detallesFactura = _facturaDetalle
                    .GetDetalleByIdHeader(header.IdFacturaHeader);

                foreach (var det in detallesFactura)
                {
                    var producto = _Productos.GetProductoById(det.IdProducto);

                    if (producto == null)
                        continue;

                    if (producto.EsServicio)
                        continue;

                    if (!producto.ControlarStock)
                        continue;

                    movimientoInventario.Detalles.Add(
                        new MovimientosInventarioDetalle
                        {
                            IdProducto = producto.IdProducto,
                            Cantidad = det.Cantidad,
                            Precio = 0,
                            SubTotal = 0,
                            Observacion = $"Venta factura #{header.IdFacturaHeader}"
                        });
                }

                if (movimientoInventario.Detalles.Any())
                {
                    await _movimientosInventario.GuardarMovimiento(movimientoInventario);
                }

                var facturaPrint = await ConstruirFacturaPrintAsync(header);

                // Outbox fiscal dentro de la transacción. La transmisión HTTP la hace el worker después del COMMIT.
                try
                {
                    await _fiscalEnqueue.EnqueueFotografiaSiActivoAsync(new FiscalDocumentoRequest
                    {
                        IdEmpresa = header.IdEmpresa,
                        IdUsuario = header.IdUsuario ?? dto.Header.IdUsuario ?? 0,
                        ReferenciaId = header.IdFacturaHeader,
                        TipoDocumento = "Venta"
                    });
                }
                catch (Exception exFiscal)
                {
                    _logger.LogError(exFiscal,
                        "No se pudo persistir el outbox fiscal de la venta {IdFactura}. Se revierte la venta.",
                        header.IdFacturaHeader);
                    throw;
                }

                await tx.CommitAsync();

                // Contabilidad automática (no-op si Contabilidad apagada). No revierte la venta.
                string? contabilidadAdvertencia = null;
                try
                {
                    decimal costoInventario = 0;
                    foreach (var d in detallesFactura)
                    {
                        var producto = _Productos.GetProductoById(d.IdProducto);
                        // Servicios no generan COGS; productos con costo sí (aunque no controlen stock).
                        if (producto == null || producto.EsServicio || producto.PrecioCompra <= 0)
                            continue;
                        costoInventario += producto.PrecioCompra * d.Cantidad;
                    }

                    var montoCobrado = header.Pagado;
                    var montoCredito = header.Pendiente + header.PendienteArs;
                    if (montoCobrado <= 0 && montoCredito <= 0)
                        montoCobrado = header.Total;

                    var metodoPrincipal = pagosAgrupados.OrderByDescending(p => p.Monto).FirstOrDefault()?.Metodo;

                    var pagosContab = new List<AlahiaPos.Entities.Events.VentaPagoParte>();
                    foreach (var p in pagosAgrupados.Where(x => x.Monto > 0))
                    {
                        int? idCuentaFin = null;
                        try
                        {
                            var cfg = await _MetodoPagoCuentaService.GetByMetodoAsync(header.IdEmpresa, p.Metodo);
                            if (cfg != null && cfg.IdCuentaFinanciera > 0)
                                idCuentaFin = cfg.IdCuentaFinanciera;
                        }
                        catch
                        {
                            // Contabilidad usa fallback por nombre de método
                        }

                        pagosContab.Add(new AlahiaPos.Entities.Events.VentaPagoParte
                        {
                            Metodo = p.Metodo,
                            Monto = p.Monto,
                            IdCuentaFinanciera = idCuentaFin
                        });
                    }

                    var contab = await _contabilidadEvents.TryPublishAsync(new AlahiaPos.Entities.Events.VentaConfirmadaEvent
                    {
                        IdEmpresa = header.IdEmpresa,
                        IdUsuario = header.IdUsuario ?? dto.Header.IdUsuario ?? 0,
                        Fecha = header.FechaInseccion == default ? DateTime.Now : header.FechaInseccion,
                        ReferenciaId = header.IdFacturaHeader,
                        ReferenciaTipo = "Venta",
                        NumeroFactura = header.NumeroDocumento ?? header.IdFacturaHeader.ToString(),
                        TipoFactura = header.TipoFactura ?? string.Empty,
                        Subtotal = header.SubTotal,
                        Itbis = header.TotalItbis,
                        Total = header.Total,
                        MontoCobrado = montoCobrado,
                        MontoCredito = montoCredito,
                        CostoInventario = costoInventario,
                        MetodoPago = metodoPrincipal,
                        Pagos = pagosContab
                    });
                    contabilidadAdvertencia = contab.Advertencia;
                }
                catch
                {
                    // Nunca tumbar venta
                }

                return Ok(new
                {
                    message = "Factura procesada correctamente",
                    idFactura = header.IdFacturaHeader,
                    factura = facturaPrint,
                    contabilidadAdvertencia
                });
            }
            catch (DbUpdateException ex)
            {
                try
                {
                    await tx.RollbackAsync();
                }
                catch
                {
                    // Transacción ya abortada por SQL Server.
                }

                _context.ChangeTracker.Clear();

                var clave = ProcesarFacturaIntegridad.NormalizarClaveIdempotencia(dto?.IdempotencyKey);
                if (clave.Length > 0 && dto?.Header != null)
                {
                    var idPrevio = await ProcesarFacturaIntegridad.BuscarFacturaPorClaveIdempotenciaAsync(
                        _context, dto.Header.IdEmpresa, clave);
                    if (idPrevio is > 0)
                    {
                        header = _facturaHeader.GetById(idPrevio.Value);
                        if (header != null)
                        {
                            var printDup = await ConstruirFacturaPrintAsync(header);
                            return Ok(new
                            {
                                message = "Factura procesada correctamente",
                                idFactura = header.IdFacturaHeader,
                                factura = printDup,
                                idempotente = true
                            });
                        }
                    }
                }

                var detalleUnico = ex.InnerException?.Message ?? ex.Message;
                _logger.LogError(ex, "ProcesarFactura revertido por persistencia. Empresa={Empresa}", dto?.Header?.IdEmpresa);
                return BadRequest(detalleUnico);
            }
            catch (Exception ex)
            {
                var detalle = ex.InnerException?.Message ?? ex.Message;
                _logger.LogError(ex, "ProcesarFactura revertido. Empresa={Empresa}", dto?.Header?.IdEmpresa);
                return BadRequest(detalle);
            }
        }

        private async Task<FacturaPrintDTO> ConstruirFacturaPrintAsync(FacturaHeaders header)
        {
            var empresa = await _Empresas.GetEmpresaById(header.IdEmpresa);
            Sucursal? sucursal = null;
            if (header.IdSucursal is > 0)
            {
                sucursal = await _context.Sucursales.AsNoTracking()
                    .FirstOrDefaultAsync(s =>
                        s.IdSucursal == header.IdSucursal.Value
                        && s.IdEmpresa == header.IdEmpresa);
            }
            var cliente = header.IDCliente > 0
                ? await _Clientes.GetAllClientesById((int)header.IDCliente)
                : null;
            var detalles = _facturaDetalle.GetDetalleByIdHeader(header.IdFacturaHeader);
            var itemsPrint = new List<FacturaItemPrintDTO>();

            foreach (var d in detalles)
            {
                var producto = _Productos.GetProductoById(d.IdProducto);
                var precio = d.PrecioOferta > 0
                    ? d.PrecioOferta
                    : (producto?.PrecioVenta ?? 0);

                itemsPrint.Add(new FacturaItemPrintDTO
                {
                    Nombre = producto?.Nombre ?? "Producto",
                    Cantidad = d.Cantidad,
                    Precio = precio,
                    SubTotal = d.SubTotal
                });
            }

            return new FacturaPrintDTO
            {
                IdFactura = header.IdFacturaHeader,
                Cliente = cliente?.NombreComercial ?? "Al Portador",
                Empresa = empresa?.NombreComercial ?? "Mi Empresa",
                NombreSucursal = DocumentoSucursalContacto.Nombre(sucursal),
                Rnc = empresa?.RNC ?? "",
                Direccion = DocumentoSucursalContacto.FormatearDireccion(sucursal, empresa),
                Telefono = DocumentoSucursalContacto.Telefono(sucursal, empresa),
                Fecha = header.FechaInseccion,
                TipoFactura = header.TipoFactura,
                Total = header.Total,
                Pagado = header.Pagado,
                Pendiente = header.Pendiente,
                Items = itemsPrint
            };
        }

        private async Task AplicarEmisorSucursalAsync(FacturaHeaderDto dto)
        {
            var empresa = await _Empresas.GetEmpresaById(dto.IdEmpresa);
            Sucursal? sucursal = null;
            if (dto.IdSucursal is > 0)
            {
                sucursal = await _context.Sucursales.AsNoTracking()
                    .FirstOrDefaultAsync(s =>
                        s.IdSucursal == dto.IdSucursal.Value
                        && s.IdEmpresa == dto.IdEmpresa);
            }

            dto.Empresa = empresa?.NombreComercial ?? dto.Empresa;
            dto.NombreSucursal = DocumentoSucursalContacto.Nombre(sucursal);
            dto.DireccionEmpresa = DocumentoSucursalContacto.FormatearDireccion(sucursal, empresa);
            dto.TelefonoEmpresa = DocumentoSucursalContacto.Telefono(sucursal, empresa);
            dto.RNCEmpresa = empresa?.RNC ?? dto.RNCEmpresa;
        }
        [HttpPost()]
        [Route("InsertFactura")]
        public async Task<IActionResult> InsertFactura([FromBody] FacturaHeaderDto value)
        {

            try
            {
                var idUsuarioSesion = await ResolverIdUsuarioCobroAsync(value.IdUsuario);
                if (idUsuarioSesion <= 0)
                    return BadRequest(new { message = "No se pudo identificar el usuario de la factura." });

                value.FacturaDetalles.ForEach(c =>
                {
                    c.Productos = null;
                });
                var Header = _Mapper.Map<FacturaHeaders>(value);
                Header.PrintPending = true;
                Header.IdEmpresa = value.IdEmpresa;
                Header.IdUsuario = idUsuarioSesion;
                if (Header.IdMoso is null or 0 or 1)
                    Header.IdMoso = idUsuarioSesion;
                AplicarSucursalDeSesion(Header, value.IdSucursal);
                await _facturaHeader.InsertFacturaHeader(Header);
                return Ok();



            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "No se pudo insertar la factura." });
            }
        }

        [HttpGet]
        [Route("GetAllOrdenesByFecha")]
        public async Task<IActionResult> GetAllOrdenesByFecha(
        int IdEmpresa,
        DateTime fechaDesde,
        DateTime fechaHasta,
        int? idSucursalFiltro = null)
        {
            var (scope, error) = await SucursalConsultaHttp.ResolverAsync(
                HttpContext, _tokens, _sucursales, IdEmpresa, idSucursalFiltro);
            if (error != null)
                return error;

            var listaReturn = new List<FacturaHeaders>();

            // 🔥 Buscar headers por rango de fecha
            var headers = await _facturaHeader.GetAllOrdenesByFecha(
                IdEmpresa,
                fechaDesde,
                fechaHasta);

            if (headers == null || !headers.Any())
                return Ok(new List<FacturaHeaderDto>());

            headers = headers.Where(h => scope.Incluye(h.IdSucursal)).ToList();

            foreach (var item in headers)
            {
                var detalles = await _facturaDetalle.GetOrdenesByHeader(item.IdFacturaHeader);

                if (detalles == null || !detalles.Any())
                    continue;

                var listaDetalles = new List<FacturaDetalles>();

                foreach (var d in detalles)
                {
                    
                        // ⭐ cargar producto
                        var producto = await _Productos.GetAllProductosById(d.IdProducto);
                        d.Productos = producto;

                        // ❌ YA NO CARGAMOS EMPLEADO
                        // 🔥 menos queries y más rápido

                        listaDetalles.Add(d);
                    
                }

                if (listaDetalles.Any())
                {
                    item.FacturaDetalles = listaDetalles;
                    listaReturn.Add(item);
                }
            }

            var mapped = _Mapper.Map<FacturaHeaderDto[]>(listaReturn);
            AsignarNombresSucursal(mapped, scope);
            await AsignarNombresUsuarioAsync(mapped, IdEmpresa);
            return Ok(mapped);
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

            var producto = _Productos.GetProductoById(cita.IdProducto);

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
                NombreCuenta = cita.NombreCliente


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
            header.Pagado = 0;
            header.Pendiente = header.Total;
            header.Nota = (cita.Abono ?? 0) > 0
                ? $"Cliente con saldo a favor por reserva de cita #{cita.IdCita} (RD$ {cita.Abono:N2}). Aplicar al cobrar."
                : header.Nota;
            header.FechaInseccion = DateTime.Now;
            header.FechaBencimiento = DateTime.Now;
            header.IdEmpleados = cita.IdEmpleado;
            header.IdMoso = cita.IdEmpleado;
            header.IdTipoDocumentos = 10;
            // ============================
            // GUARDAR ORDEN
            // ============================
            await _facturaHeader.InsertFacturaHeader(header);

            // Centro de Producción: post-commit aislado (nunca tumba la orden)
            try
            {
                await _produccionPosAdapter.PublicarOrdenSiAplicaAsync(header);
            }
            catch
            {
            }

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
            try
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
                    var pagosLista = dto.DetallePagos.Where(x => x.Monto > 0).ToList();
                    var pagosNc = pagosLista
                        .Where(x => FormaPagoNotaCredito.EsNotaCredito(x.Metodo))
                        .ToList();
                    var pagosAgrupados = pagosLista
                        .Where(x => !FormaPagoNotaCredito.EsNotaCredito(x.Metodo)
                            && !FormaPagoArs.EsArs(x.Metodo))
                        .GroupBy(x => x.Metodo)
                        .Select(g => new
                        {
                            Metodo = g.Key,
                            Monto = g.Sum(x => x.Monto)
                        });

                    foreach (var pagoNc in pagosNc)
                    {
                        await _notasCredito.ConsumirSaldoAFavorEnVentaAsync(
                            header.IdEmpresa,
                            header.IdFacturaHeader,
                            header.IDCliente ?? dto.IdCliente,
                            pagoNc.Monto,
                            pagoNc.IdSaldoAFavor,
                            pagoNc.IdNotaCredito,
                            pagoNc.NcfNotaCredito,
                            header.IdUsuario);

                        totalPagadoAhora += pagoNc.Monto;
                        header.MontoNotaCredito = Math.Round(header.MontoNotaCredito + pagoNc.Monto, 2);

                        var notaNc = $"Aplicación NC {pagoNc.NcfNotaCredito}";
                        var existeIngresoNc = await _IngresosServices.ExisteIngreso(
                            header.IdFacturaHeader,
                            FormaPagoNotaCredito.Metodo);
                        if (!existeIngresoNc)
                        {
                            await _IngresosServices.InsertIngreso(new Ingresos
                            {
                                IdEmpresa = header.IdEmpresa,
                                FechaRegistro = DateTime.Now,
                                Descripcion = $"Factura #{header.IdFacturaHeader} — NC {pagoNc.NcfNotaCredito}",
                                Categoria = "Aplicación Nota de Crédito",
                                Origen = "Sistema",
                                Monto = pagoNc.Monto,
                                FormaPago = FormaPagoNotaCredito.Metodo,
                                Referencia = pagoNc.NcfNotaCredito ?? $"NC-{pagoNc.IdNotaCredito}",
                                IdFacturaHeader = header.IdFacturaHeader,
                                IdCliente = header.IDCliente,
                                Nota = notaNc
                            });
                        }

                        await _PagoFacturaClientes.InsertPagosFacturasClientes(new PagosFacturasClientes
                        {
                            IdFacturaHeader = header.IdFacturaHeader,
                            NumeroDocumento = header.NumeroDocumento,
                            IDCliente = header.IDCliente,
                            FormaPago = FormaPagoNotaCredito.Metodo,
                            Monto = pagoNc.Monto,
                            Nota = notaNc
                        });
                    }

                    await _arsAseguradora.AplicarCoberturaEnVentaAsync(
                        header,
                        pagosLista,
                        esAbonoInicialCredito: false);

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

                        // Tesorería: no tumbar el cobro si la cuenta/método falla
                        try
                        {
                            var metodoConfigurado = await _MetodoPagoCuentaService
                                .GetByMetodoAsync(header.IdEmpresa, pago.Metodo);

                            if (metodoConfigurado != null &&
                                metodoConfigurado.IdCuentaFinanciera > 0)
                            {
                                await _MovimientoFinancieroService.RegistrarEntradaAsync(
                                    header.IdEmpresa,
                                    header.IdUsuario ?? header.IdEmpleados ?? 0,
                                    metodoConfigurado.IdCuentaFinanciera,
                                    pago.Monto,
                                    $"Factura #{header.IdFacturaHeader}",
                                    $"Ingreso automático desde ventas ({pago.Metodo})",
                                    categoria: "VENTA",
                                    referenciaId: header.IdFacturaHeader,
                                    referenciaTipo: "FACTURA",
                                    claveIdempotencia: $"VENTA-{header.IdFacturaHeader}-{pago.Metodo}"
                                );
                            }
                        }
                        catch (Exception exTesoreria)
                        {
                            Console.WriteLine(
                                $"Tesorería omitida en GenerateFacts #{header.IdFacturaHeader}: {exTesoreria.Message}");
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

                header.Pagado = totalPagadoAhora;
                header.TipoFactura = "Contado";
                FormaPagoArs.RecalcularSaldos(header);
                if (header.Pendiente > 0.02m)
                    header.TipoFactura = "Credito";
            }

            // ============================================
            // 🔹 CRÉDITO
            // ============================================
            else if (dto.TipoFactura == "Credito")
            {
                decimal totalAbonadoAhora = 0;

                if (dto.DetalleAbono != null && dto.DetalleAbono.Any())
                {
                    var abonosLista = dto.DetalleAbono.Where(x => x.Monto > 0).ToList();
                    var abonosNc = abonosLista
                        .Where(x => FormaPagoNotaCredito.EsNotaCredito(x.Metodo))
                        .ToList();
                    var pagosAgrupados = abonosLista
                        .Where(x => !FormaPagoNotaCredito.EsNotaCredito(x.Metodo)
                            && !FormaPagoArs.EsArs(x.Metodo))
                        .GroupBy(x => x.Metodo)
                        .Select(g => new
                        {
                            Metodo = g.Key,
                            Monto = g.Sum(x => x.Monto)
                        });

                    foreach (var pagoNc in abonosNc)
                    {
                        if (pagoNc.Monto > header.Pendiente - totalAbonadoAhora)
                            return BadRequest("El monto ingresado excede el pendiente de la factura.");

                        await _notasCredito.ConsumirSaldoAFavorEnVentaAsync(
                            header.IdEmpresa,
                            header.IdFacturaHeader,
                            header.IDCliente ?? dto.IdCliente,
                            pagoNc.Monto,
                            pagoNc.IdSaldoAFavor,
                            pagoNc.IdNotaCredito,
                            pagoNc.NcfNotaCredito,
                            header.IdUsuario);

                        totalAbonadoAhora += pagoNc.Monto;
                        header.MontoNotaCredito = Math.Round(header.MontoNotaCredito + pagoNc.Monto, 2);

                        var notaNc = $"Aplicación NC {pagoNc.NcfNotaCredito}";
                        var existeIngresoNc = await _IngresosServices.ExisteIngreso(
                            header.IdFacturaHeader,
                            FormaPagoNotaCredito.Metodo);
                        if (!existeIngresoNc)
                        {
                            await _IngresosServices.InsertIngreso(new Ingresos
                            {
                                IdEmpresa = header.IdEmpresa,
                                FechaRegistro = DateTime.Now,
                                Descripcion = $"Abono NC — Factura #{header.IdFacturaHeader}",
                                Categoria = "Aplicación Nota de Crédito",
                                Origen = "Sistema",
                                Monto = pagoNc.Monto,
                                FormaPago = FormaPagoNotaCredito.Metodo,
                                Referencia = pagoNc.NcfNotaCredito ?? $"NC-{pagoNc.IdNotaCredito}",
                                IdFacturaHeader = header.IdFacturaHeader,
                                IdCliente = header.IDCliente,
                                Nota = notaNc
                            });
                        }

                        await _PagoFacturaClientes.InsertPagosFacturasClientes(new PagosFacturasClientes
                        {
                            IdFacturaHeader = header.IdFacturaHeader,
                            NumeroDocumento = header.NumeroDocumento,
                            IDCliente = header.IDCliente,
                            FormaPago = FormaPagoNotaCredito.Metodo,
                            Monto = pagoNc.Monto,
                            Nota = notaNc
                        });
                    }

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

                        try
                        {
                            var metodoConfiguradoCredito = await _MetodoPagoCuentaService
                                .GetByMetodoAsync(header.IdEmpresa, pago.Metodo);

                            if (metodoConfiguradoCredito != null &&
                                metodoConfiguradoCredito.IdCuentaFinanciera > 0)
                            {
                                await _MovimientoFinancieroService.RegistrarEntradaAsync(
                                    header.IdEmpresa,
                                    header.IdUsuario ?? header.IdEmpleados ?? 0,
                                    metodoConfiguradoCredito.IdCuentaFinanciera,
                                    pago.Monto,
                                    $"Factura #{header.IdFacturaHeader}",
                                    $"Abono ({pago.Metodo}) — Factura #{header.IdFacturaHeader}",
                                    categoria: "COBRO_CXC",
                                    referenciaId: header.IdFacturaHeader,
                                    referenciaTipo: "FACTURA",
                                    claveIdempotencia: $"ABONO-{header.IdFacturaHeader}-{pago.Metodo}-{DateTime.Now:yyyyMMddHHmmss}"
                                );
                            }
                        }
                        catch (Exception exTesoreria)
                        {
                            Console.WriteLine(
                                $"Tesorería omitida en GenerateFacts abono #{header.IdFacturaHeader}: {exTesoreria.Message}");
                        }

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
                header.TipoFactura = "Credito";
                var pagosArsCredito = (dto.DetalleAbono ?? new List<PagoDTO>())
                    .Concat(dto.DetallePagos ?? new List<PagoDTO>())
                    .ToList();
                await _arsAseguradora.AplicarCoberturaEnVentaAsync(
                    header,
                    pagosArsCredito,
                    esAbonoInicialCredito: totalAbonadoAhora > 0.009m);
                FormaPagoArs.RecalcularSaldos(header);
            }

            header.FechaInseccion = DateTime.Now;
            header.IdTipoDocumentos = 1;
            header.NumeroDocumento = await _secuenciaDocumentoService
                   .GenerarDocumentoAsync(header.IdEmpresa, 1);

            _facturaHeader.UpdateFacturaHeader(header.IdFacturaHeader, header);

            // ============================================
            // 🔥 MOVIMIENTO INVENTARIO POR VENTA
            // ============================================

            var detallesFactura =
                _facturaDetalle
                .GetDetalleByIdHeader(
                    header.IdFacturaHeader
                );

            var movimientoInventario =
                new MovimientosInventario
                {
                    TipoMovimiento = "SALIDA",
                    Motivo = "VENTA",
                    Referencia = $"Factura #{header.IdFacturaHeader}",
                    Observacion = "Salida automática por venta",
                    Fecha = DateTime.Now,
                    IdEmpresa = header.IdEmpresa,
                    IdSucursal = header.IdSucursal,
                    IdUsuario = header.IdUsuario,
                    Activo = true,
                    Detalles = new List<MovimientosInventarioDetalle>()
                };

            foreach (var det in detallesFactura)
            {
                var producto =
                    _Productos.GetProductoById(
                        det.IdProducto
                    );

                if (producto == null)
                    continue;

                // No descontar servicios
                if (producto.EsServicio)
                    continue;

                // Solo productos que controlan stock
                if (!producto.ControlarStock)
                    continue;

                movimientoInventario.Detalles.Add(
                    new MovimientosInventarioDetalle
                    {
                        IdProducto = producto.IdProducto,
                        Cantidad = det.Cantidad,
                        Precio = 0,
                        SubTotal = 0,
                        Observacion = $"Venta factura #{header.IdFacturaHeader}"
                    }
                );
            }

            if (movimientoInventario.Detalles.Any())
            {
                try
                {
                    await _movimientosInventario
                        .GuardarMovimiento(
                            movimientoInventario
                        );
                }
                catch (Exception exInv)
                {
                    Console.WriteLine(
                        $"Inventario omitido en GenerateFacts #{header.IdFacturaHeader}: {exInv.Message}");
                }
            }

            return Ok(new { message = "Factura generada correctamente.", idFactura = header.IdFacturaHeader });
            }
            catch (Exception ex)
            {
                var detalle = ex.InnerException?.Message ?? ex.Message;
                Console.WriteLine($"GenerateFacts error: {detalle}");
                return BadRequest(detalle);
            }
        }
        [HttpGet("ticket-lavador/{idFactura}")]
        public async Task<IActionResult> GetTicketLavador(int idFactura)
        {
            var header = await _facturaHeader.GetAllFactById(idFactura);
            var factura = header?.FirstOrDefault();
            if (factura == null)
                return NotFound();
            if (!TenantRecurso.EsDeLaSesion(HttpContext, factura.IdEmpresa))
                return NotFound();

            var tickets = await _facturaHeader.GetTicketsLavadorByFactura(idFactura);

            if (tickets == null || !tickets.Any())
                return NotFound("No hay tickets para esta factura");

            return Ok(tickets);
        }
        [HttpGet("factura-cliente/{idFactura}")]
        public async Task<IActionResult> GetFacturaCliente(int idFactura)
        {
            try
            {
                var header = await _facturaHeader.GetAllFactById(idFactura);
                var existente = header?.FirstOrDefault();
                if (existente == null)
                    return NotFound("Factura no encontrada");
                if (!TenantRecurso.EsDeLaSesion(HttpContext, existente.IdEmpresa))
                    return NotFound("Factura no encontrada");

                var ecf = await _context.ECFEncabezados.AsNoTracking()
                    .Where(e => e.IdOrigen == idFactura
                        && (e.OrigenDocumento == (int)OrigenDocumento.Pos
                            || e.OrigenDocumento == (int)OrigenDocumento.Facturacion))
                    .OrderByDescending(e => e.IdECF)
                    .Select(e => new { e.ENCF, e.EstadoDGII, e.MensajeRespuesta })
                    .FirstOrDefaultAsync();
                var estadoEcf = ecf?.EstadoDGII ?? "";
                if (estadoEcf.Contains("Rechazado", StringComparison.OrdinalIgnoreCase)
                    || estadoEcf.Contains("Error", StringComparison.OrdinalIgnoreCase))
                {
                    var motivo = string.IsNullOrWhiteSpace(ecf?.MensajeRespuesta)
                        ? "DGII rechazó el comprobante."
                        : ecf!.MensajeRespuesta;
                    return Conflict(new
                    {
                        success = false,
                        message = $"No se imprime {ecf?.ENCF}. {motivo}"
                    });
                }

                var factura = await _facturaHeader.GetFacturaClienteById(idFactura);

                if (factura == null)
                    return NotFound("Factura no encontrada");

                return Ok(factura);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message,
                    detail = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// Genera token firmado para compartir una cotización POS con el cliente final (WhatsApp/link).
        /// </summary>
        [HttpPost("cotizacion/{idFacturaHeader}/compartir")]
        public async Task<IActionResult> CrearLinkCotizacionPublica(
            int idFacturaHeader,
            [FromQuery] int idEmpresa,
            [FromBody] CotizacionCompartirRequest? request)
        {
            if (idFacturaHeader <= 0 || idEmpresa <= 0)
                return BadRequest(new { message = "Cotización o empresa inválida." });

            var link = await _facturaHeader.CrearLinkCotizacionPublicaAsync(
                idFacturaHeader,
                idEmpresa);

            if (link == null)
                return NotFound(new { message = "Cotización no encontrada o no es compartible." });

            var baseUrl = (request?.PublicBaseUrl ?? "").Trim().TrimEnd('/');
            if (!string.IsNullOrWhiteSpace(baseUrl) &&
                Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) &&
                (baseUri.Scheme == Uri.UriSchemeHttp || baseUri.Scheme == Uri.UriSchemeHttps))
            {
                // Token sin encode: WhatsApp detecta mejor el URL.
                link.Url = $"{baseUrl}/cotizacion/ver/{link.Token}";
                link.UrlCorta = await AcortarUrlParaWhatsAppAsync(link.Url);
            }

            return Ok(link);
        }

        /// <summary>
        /// WhatsApp no convierte localhost en enlace. Genera un https:// corto clickeable.
        /// </summary>
        private static async Task<string?> AcortarUrlParaWhatsAppAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            // Si ya es https público (no local), WhatsApp lo linkifica solo.
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                uri.Scheme == Uri.UriSchemeHttps &&
                !IsLocalHost(uri.Host))
            {
                return url;
            }

            try
            {
                using var http = new System.Net.Http.HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(8)
                };
                var encoded = Uri.EscapeDataString(url);
                // TinyURL acepta destinos localhost (is.gd suele rechazarlos).
                var shortUrl = (await http.GetStringAsync(
                    $"https://tinyurl.com/api-create.php?url={encoded}")).Trim();

                if (shortUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
                    !shortUrl.Contains("Error", StringComparison.OrdinalIgnoreCase))
                {
                    return shortUrl;
                }
            }
            catch
            {
                // Sin acortador: el cliente usará la URL larga.
            }

            return null;
        }

        private static bool IsLocalHost(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
                return true;
            return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                   || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                   || host.Equals("::1", StringComparison.OrdinalIgnoreCase)
                   || host.EndsWith(".local", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Vista pública (sin login): el cliente abre el link enviado por WhatsApp/correo.
        /// </summary>
        [HttpGet("cotizacion-publica/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCotizacionPublica(string token)
        {
            var vista = await _facturaHeader.ObtenerCotizacionPublicaAsync(token);

            if (vista == null)
                return NotFound(new { message = "Cotización no encontrada o enlace inválido." });

            return Ok(vista);
        }
        // DELETE api/<FacturaHeaderController>/5
        // Órdenes (10) y cotizaciones (2). Las facturas tipo 1 se anulan, no se borran.
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarFactura(int id)
        {
            var factura = _facturaHeader.GetById(id);
            if (factura == null)
                return NotFound();
            if (!TenantRecurso.EsDeLaSesion(HttpContext, factura.IdEmpresa))
                return NotFound();

            var idUsuario = await ResolverIdUsuarioCobroAsync(null);
            var usuario = idUsuario > 0 ? await _IUsuarios.ObtenerPorId(idUsuario) : null;
            if (!TienePermisoOperativo(usuario, usuario?.PuedeEliminarOrden == true))
                return StatusCode(403, new { message = "No tiene permiso para eliminar órdenes." });

            if (factura.IdTipoDocumentos == 1)
                return BadRequest(new { message = "Las facturas se anulan, no se eliminan." });

            try
            {
                var ok = await _facturaHeader.EliminarFacturaCompleta(id);
                if (!ok)
                    return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "No se pudo eliminar la orden porque tiene documentos relacionados." });
            }

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
        public async Task<IActionResult> AnularFactura([FromBody] AnularFacturaDto dto)
        {
            if (dto == null)
                return BadRequest("Datos de anulación requeridos");

            if (string.IsNullOrWhiteSpace(dto.MotivoAnulacion))
                return BadRequest("Debe indicar el motivo de anulación");

            if (!TenantRecurso.EsDeLaSesion(HttpContext, dto.IdEmpresa))
                return StatusCode(403, new { message = SesionHttp.ForbiddenOtraEmpresa });

            var idUsuarioAnula = await ResolverIdUsuarioCobroAsync(dto.IdUsuario);
            if (idUsuarioAnula <= 0)
                return BadRequest(new { message = "No se pudo identificar el usuario que anula." });

            var usuarioSesion = await _IUsuarios.ObtenerPorId(idUsuarioAnula);
            if (!TienePermisoOperativo(usuarioSesion, usuarioSesion?.PuedeAnularFactura == true))
                return StatusCode(403, new { message = "No tiene permiso para anular facturas." });

            var factura = await _facturaHeader.GetFacturaHeaderById(
                dto.IdFacturaHeader,
                dto.IdEmpresa);

            if (factura == null)
                return NotFound("Factura no existe");

            if (factura.EstaCancelada)
                return BadRequest("Factura ya está anulada");

            var motivo = dto.MotivoAnulacion.Trim();

            if (!string.IsNullOrWhiteSpace(dto.UsuarioAnulo))
            {
                motivo = $"[{dto.UsuarioAnulo.Trim()}] {motivo}";
            }

            // 🔴 marcar cancelada y cerrar CxC (saldo)
            factura.EstaCancelada = true;
            factura.MotivoAnulacion = motivo;
            factura.Pendiente = 0;
            factura.Estado = "Anulada";
            if (factura.MontoCubiertoArs > 0.009m)
            {
                factura.PendienteArs = 0;
                factura.EstadoArs = FormaPagoArs.EstadoAnulada;
            }
            // No sobrescribir FechaInseccion: es la fecha del documento original.
            factura.Clientes = null;
            factura.Empleados = null;
            factura.FacturaDetalles = null;
            factura.TipoDocumentos = null;
            factura.Mesas = null;
            _facturaHeader.UpdateFacturaHeader(dto.IdFacturaHeader, factura);

            // 🔴 revertir ingresos
            try
            {
                await _IngresosServices.RevertirIngresoPorFactura(
                    dto.IdFacturaHeader,
                    dto.IdEmpresa);
            }
            catch
            {
                // Nunca tumbar anulación operativa
            }

            // 🔴 reingresar stock (reverso de la salida por venta)
            try
            {
                var detallesFactura = _facturaDetalle
                    .GetDetalleByIdHeader(dto.IdFacturaHeader);

                var movimientoEntrada = new MovimientosInventario
                {
                    TipoMovimiento = "ENTRADA",
                    Motivo = "ANULACION_VENTA",
                    Referencia = $"Factura #{dto.IdFacturaHeader}",
                    Observacion = "Reingreso automático por anulación de venta",
                    Fecha = DateTime.Now,
                    IdEmpresa = dto.IdEmpresa,
                    IdSucursal = factura.IdSucursal,
                    IdUsuario = idUsuarioAnula,
                    Activo = true,
                    Detalles = new List<MovimientosInventarioDetalle>()
                };

                foreach (var det in detallesFactura)
                {
                    var producto = _Productos.GetProductoById(det.IdProducto);

                    if (producto == null)
                        continue;

                    if (producto.EsServicio)
                        continue;

                    if (!producto.ControlarStock)
                        continue;

                    movimientoEntrada.Detalles.Add(
                        new MovimientosInventarioDetalle
                        {
                            IdProducto = producto.IdProducto,
                            Cantidad = det.Cantidad,
                            Precio = 0,
                            SubTotal = 0,
                            Observacion = $"Anulación factura #{dto.IdFacturaHeader}"
                        });
                }

                if (movimientoEntrada.Detalles.Any())
                    await _movimientosInventario.GuardarMovimiento(movimientoEntrada);
            }
            catch
            {
                // Nunca tumbar anulación operativa
            }

            // Restaurar saldos a favor consumidos con NotaCredito en esta venta
            try
            {
                await _notasCredito.RevertirConsumosSaldoPorFacturaAsync(
                    dto.IdEmpresa,
                    dto.IdFacturaHeader,
                    idUsuarioAnula);
            }
            catch
            {
                // Nunca tumbar anulación operativa
            }

            // 🔴 revertir tesorería (reverso de las entradas por pagos)
            try
            {
                var pagosFactura = await _PagoFacturaClientes
                    .GetPagosByFacturaId(dto.IdFacturaHeader);

                var pagosPorMetodo = (pagosFactura ?? Enumerable.Empty<PagosFacturasClientes>())
                    .Where(p =>
                        p.Monto > 0
                        && !string.IsNullOrWhiteSpace(p.FormaPago)
                        && !FormaPagoNotaCredito.EsNotaCredito(p.FormaPago))
                    .GroupBy(p => p.FormaPago)
                    .Select(g => new { Metodo = g.Key, Monto = g.Sum(x => x.Monto) })
                    .ToList();

                foreach (var pago in pagosPorMetodo)
                {
                    var metodoConfigurado = await _MetodoPagoCuentaService
                        .GetByMetodoAsync(dto.IdEmpresa, pago.Metodo);

                    if (metodoConfigurado != null &&
                        metodoConfigurado.IdCuentaFinanciera > 0)
                    {
                        await _MovimientoFinancieroService.RegistrarSalidaAsync(
                            dto.IdEmpresa,
                            idUsuarioAnula,
                            metodoConfigurado.IdCuentaFinanciera,
                            pago.Monto,
                            $"Anulación factura #{dto.IdFacturaHeader}",
                            $"Reverso automático por anulación de venta ({pago.Metodo})",
                            categoria: "VENTA",
                            referenciaId: dto.IdFacturaHeader,
                            referenciaTipo: "FACTURA",
                            claveIdempotencia: $"VENTA-ANUL-{dto.IdFacturaHeader}-{pago.Metodo}"
                        );
                    }
                }
            }
            catch
            {
                // Nunca tumbar anulación operativa
            }

            // Contabilidad: reverso de asientos ALTA/COGS (no-op si Contabilidad apagada)
            string? contabilidadAdvertencia = null;
            try
            {
                var contab = await _contabilidadEvents.TryPublishAsync(new AlahiaPos.Entities.Events.VentaAnuladaEvent
                {
                    IdEmpresa = dto.IdEmpresa,
                    IdUsuario = idUsuarioAnula,
                    Fecha = DateTime.Now,
                    ReferenciaId = dto.IdFacturaHeader,
                    ReferenciaTipo = "VentaAnulada",
                    Motivo = motivo,
                    NumeroFactura = factura.NumeroDocumento ?? dto.IdFacturaHeader.ToString()
                });
                contabilidadAdvertencia = contab.Advertencia;
            }
            catch
            {
                // Nunca tumbar anulación operativa
            }

            return Ok(new { contabilidadAdvertencia });
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
            await _IPrinter.GenerateTicketLavador(IdFact);

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
                string error = ex.Message;
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
        [HttpGet]
        [Route("GetIngresosCajaAbierta")]
        public async Task<IActionResult>
GetIngresosCajaAbierta(

    int idEmpresa,

    int idUsuario
)
        {
            try
            {
                var result =

                    await _facturaHeader
                    .GetIngresosCajaAbierta(

                        idEmpresa,

                        idUsuario
                    );

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message =
                        "Error obteniendo ingresos de la caja abierta",

                    error =
                        ex.Message
                });
            }
        }

        [HttpGet("GetCuentasPorCobrar/{idEmpresa}")]
        public async Task<IActionResult> GetCuentasPorCobrar(int idEmpresa, int? idSucursalFiltro = null)
        {
            try
            {
                var (scope, error) = await SucursalConsultaHttp.ResolverAsync(
                    HttpContext, _tokens, _sucursales, idEmpresa, idSucursalFiltro);
                if (error != null)
                    return error;

                var result = await _facturaHeader.GetCuentasPorCobrar(idEmpresa, scope);

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

    
    // ======================================================
    // Formato 607 (DGII) — ventas + NC
    // ======================================================

        [HttpGet]
        [Route("Reporte607")]
        public async Task<IActionResult> Reporte607(
            DateTime desde,
            DateTime hasta,
            int idEmpresa,
            string? periodo = null)
        {
            try
            {
                var result = await _reporte607.ObtenerReporte607Async(
                    idEmpresa,
                    desde,
                    hasta,
                    periodo);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Error obteniendo reporte 607",
                    error = ex.Message
                });
            }
        }

        private void AplicarSucursalDeSesion(FacturaHeaders header, int? idSucursalDto)
        {
            var sesion = SesionHttp.TryGet(HttpContext);
            if (sesion != null && sesion.IdSucursal > 0)
            {
                header.IdSucursal = sesion.IdSucursal;
                return;
            }

            var raw = HttpContext.Request.Headers[SesionAuthFilter.HeaderSucursal].FirstOrDefault();
            if (int.TryParse(raw, out var idHeader) && idHeader > 0)
            {
                header.IdSucursal = idHeader;
                return;
            }

            if (idSucursalDto is > 0)
            {
                header.IdSucursal = idSucursalDto;
                return;
            }

            var idUsuario = sesion?.IdUsuario ?? 0;
            if (idUsuario <= 0)
                idUsuario = header.IdUsuario ?? 0;
            if (idUsuario <= 0)
                return;

            var activa = _context.Usuarios.AsNoTracking()
                .Where(u => u.IdUsuario == idUsuario)
                .Select(u => u.IdSucursalActiva)
                .FirstOrDefault();
            if (activa is > 0)
                header.IdSucursal = activa;
        }

        private bool TryIdUsuarioSesion(out int idUsuario)
        {
            var sesion = SesionHttp.TryGet(HttpContext);
            if (sesion != null && sesion.IdUsuario > 0)
            {
                idUsuario = sesion.IdUsuario;
                return true;
            }

            idUsuario = 0;
            return false;
        }

        private Task<int> ResolverIdUsuarioCobroAsync(int? idUsuarioDto)
            => IdUsuarioCobroResolver.ResolverAsync(
                HttpContext, _tokens, idUsuarioDto, HttpContext.RequestAborted);

        public static bool TienePermisoOperativo(Usuarios? usuario, bool flagUsuario)
        {
            if (usuario == null)
                return false;
            if (flagUsuario)
                return true;

            var perfil = usuario.Perfil?.Nombre?.Trim() ?? "";
            if (string.Equals(perfil, "Administrador", StringComparison.OrdinalIgnoreCase)
                || perfil.Contains("ADMIN", StringComparison.OrdinalIgnoreCase))
                return true;

            var ocupacion = usuario.Empleado?.Ocupacion?.Trim() ?? "";
            return string.Equals(ocupacion, "Administrador", StringComparison.OrdinalIgnoreCase);
        }
    } 


}

