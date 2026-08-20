using AlahiaPos.DataAccess.Servicios;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Dto.Invoice;
using AlahiaPos.Entities.Interfaces;
using AutoMapper;
using Google.Apis.Util;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
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
            IReporte607Service reporte607
          

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
            var headers = await _facturaHeader.GetAllOrdenes(IdEmpresa);
            return await MapListadoOrdenesAsync(headers, IdEmpresa);
        }

        [HttpGet]
        [Route("GetAllCotizaciones")]
        public async Task<IEnumerable<FacturaHeaderDto>> GetAllCotizaciones(int IdEmpresa)
        {
            var headers = await _facturaHeader.GetAllCotizaciones(IdEmpresa);
            return await MapListadoOrdenesAsync(headers, IdEmpresa);
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

            var resultado = new List<FacturaHeaders>(lista.Count);
            foreach (var item in lista)
            {
                var detalles = (item.FacturaDetalles ?? Enumerable.Empty<FacturaDetalles>())
                    .Where(d => d.StatuItem == false)
                    .ToList();

                if (detalles.Count == 0)
                    continue;

                foreach (var d in detalles)
                {
                    d.FacturaHeader = null;
                    if (d.Productos != null)
                        d.Productos.MovimientosInventarioDetalle = new List<MovimientosInventarioDetalle>();

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
                resultado.Add(item);
            }

            return _Mapper.Map<FacturaHeaderDto[]>(resultado);
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
                // =========================================
                // 🔥 SI EXISTE → ELIMINAR COMPLETA
                // =========================================

                int? origenIdAnterior = null;
                if (value.IdFacturaHeader > 0)
                {
                    origenIdAnterior = value.IdFacturaHeader;
                    await _facturaHeader
                        .EliminarFacturaCompleta(
                            value.IdFacturaHeader
                        );
                }

                // =========================================
                // 🔥 MAPEAR HEADER
                // =========================================

                List<FacturaDetalles> lista =
                    new List<FacturaDetalles>();

                var Header =
                    _Mapper.Map<FacturaHeaders>(value);

                Header.IdFacturaHeader = 0;

                Header.Clientes = null;

                var _GetEmpleado =
                    await _IUsuarios.ObtenerPorId(
                        (int)value.IdMoso
                    );

                Header.IdMesa = 1;

                Header.Estado_Orden =
                    "Pendiente";

                Header.TipoOrden =
                    string.IsNullOrWhiteSpace(value.TipoOrden)
                        ? ""
                        : value.TipoOrden.Trim();

                Header.FechaInseccion =
                    DateTime.Now.Date;

                Header.IDCliente =
                    value.IDCliente;

                Header.Hora =
                    DateTime.Now.ToString(
                        "hh:mm tt"
                    );

                Header.PrintPending = false;

                Header.IdEmpleadoComision = 1;

                Header.IdEmpresa =
                    value.IdEmpresa;

                Header.IdEmpleados =
                    _GetEmpleado.IdEmpleado;

                Header.PrintLavador = false;
                Header.IdUsuario = _GetEmpleado.IdUsuario;
                Header.IdTipoDocumentos =
                    value.IdTipoDocumentos is 2 or 10
                        ? value.IdTipoDocumentos
                        : 10;

                // =========================================
                // 🔥 DETALLES
                // =========================================

                foreach (var item in Header.FacturaDetalles)
                {
                    var _Producto =
                        _Productos.GetProductoById(
                            item.IdProducto
                        );

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

                    item.SubTotal =
                        (precioFinal * item.Cantidad)
                        + item.Itbis;

                    item.Productos = null;

                    lista.Add(item);
                }

                Header.FacturaDetalles = lista;

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
                    await _secuenciaDocumentoService
                        .GenerarDocumentoAsync(
                            Header.IdEmpresa,
                            (int)Header.IdTipoDocumentos
                        );

                // =========================================
                // 🔥 INSERTAR
                // =========================================

                await _facturaHeader
                    .InsertFacturaHeader(Header);

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
                throw;
            }
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
        [HttpPost]
        [Route("ProcesarFactura")]
        public async Task<IActionResult> ProcesarFactura([FromBody] FacturaDirectaDTO dto)
        {
            FacturaHeaders? header = null;
            try
            {
                if (dto == null || dto.Header == null)
                    return BadRequest("Header vacío");


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
                    header.IdUsuario = dto.Header.IdUsuario;
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

                    // Número interno normal
                    header.NumeroDocumento = await _secuenciaDocumentoService
                        .GenerarDocumentoAsync(header.IdEmpresa, 1);

                    header.IdEmpleadoComision = dto.Header.IdMoso;
                    header.IdEmpleados = dto.Header.IdMoso;
                    header.IdEmpleadoConsumo = dto.Header.IdEmpleadoConsumo is > 0
                        ? dto.Header.IdEmpleadoConsumo
                        : null;
                    header.PorcentajeDescuentoEmpleado = dto.Header.PorcentajeDescuentoEmpleado;
                    header.CargarConsumoNomina = dto.Header.CargarConsumoNomina
                        && header.IdEmpleadoConsumo is > 0;
                    header.IdUsuario = dto.Header.IdUsuario;
                    header.PrintAcount = false;
                    header.IdMesa = 1;
                    header.RNC = dto.Header.RNC;
                    header.NombreEmpresa = dto.Header.NombreEmpresa;
                    header.NombreCuenta = !string.IsNullOrWhiteSpace(dto.Header.NombreCuenta)
                        ? dto.Header.NombreCuenta
                        : dto.Header.NombreEmpresa;

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

                        var itbis = itemDto.Itbis < 0 ? 0 : itemDto.Itbis;
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
                

                // =====================================================
                // 🔥 PAGOS
                // =====================================================

                decimal totalPagadoAhora = 0;

                var pagos = dto.Pagos ?? new List<PagoDTO>();

                var pagosNc = pagos
                    .Where(x => x.Monto > 0 && FormaPagoNotaCredito.EsNotaCredito(x.Metodo))
                    .ToList();

                var pagosAgrupados = pagos
                    .Where(x => x.Monto > 0 && !FormaPagoNotaCredito.EsNotaCredito(x.Metodo))
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
                    .Distinct()
                    .ToList();

                header.FormaPago = metodosPago.Count == 1
                    ? metodosPago.First()
                    : (metodosPago.Count > 1 ? "Mixto" : header.FormaPago);

                var proyectadoPagadoAhora =
                    pagosAgrupados.Sum(p => p.Monto) + pagosNc.Sum(p => p.Monto);
                var pendienteProyectado =
                    header.Total - header.Pagado - proyectadoPagadoAhora;

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
                    if (!header.IDCliente.HasValue || header.IDCliente.Value <= 0)
                        return BadRequest("Para pagar con nota de crédito debe seleccionar un cliente.");

                    await _notasCredito.ConsumirSaldoAFavorEnVentaAsync(
                        header.IdEmpresa,
                        header.IdFacturaHeader,
                        header.IDCliente.Value,
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
                                esAbonoInicialCredito
                                    ? $"Primer abono ({pago.Metodo}) — Factura #{header.IdFacturaHeader}"
                                    : $"Ingreso automático desde ventas ({pago.Metodo})",
                                categoria: esAbonoInicialCredito ? "COBRO_CXC" : "VENTA",
                                referenciaId: header.IdFacturaHeader,
                                referenciaTipo: "FACTURA",
                                claveIdempotencia: $"VENTA-{header.IdFacturaHeader}-{pago.Metodo}"
                            );
                        }
                    }
                    catch (Exception exTesoreria)
                    {
                        Console.WriteLine(
                            $"Tesorería omitida en ProcesarFactura #{header.IdFacturaHeader}: {exTesoreria.Message}");
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
                header.Pendiente = header.Total - header.Pagado;

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
                }

                header.Estado = header.Pendiente > 0 ? "Pendiente" : "Pagada";
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
                    Referencia = $"Factura #{header.IdFacturaHeader}",
                    Observacion = "Salida automática por venta",
                    Fecha = DateTime.Now,
                    IdEmpresa = header.IdEmpresa,
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
                    try
                    {
                        await _movimientosInventario.GuardarMovimiento(movimientoInventario);
                    }
                    catch (Exception exInv)
                    {
                        Console.WriteLine(
                            $"Inventario omitido en ProcesarFactura #{header.IdFacturaHeader}: {exInv.Message}");
                    }
                }

                // =====================================================
                // 🔥 DATOS PARA PRINT Y e-CF
                // =====================================================

                var empresa = await _Empresas.GetEmpresaById(header.IdEmpresa);

                var cliente = header.IDCliente > 0
                    ? await _Clientes.GetAllClientesById((int)header.IDCliente)
                    : null;

                var detalles = _facturaDetalle
                    .GetDetalleByIdHeader(header.IdFacturaHeader);

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

                var facturaPrint = new FacturaPrintDTO
                {
                    IdFactura = header.IdFacturaHeader,
                    Cliente = cliente?.NombreComercial ?? "Al Portador",
                    Empresa = empresa?.NombreComercial ?? "Mi Empresa",
                    Rnc = empresa?.RNC ?? "",
                    Direccion = empresa?.Direccion ?? "",
                    Telefono = empresa?.Telefono ?? "",
                    Fecha = header.FechaInseccion,
                    TipoFactura = header.TipoFactura,
                    Total = header.Total,
                    Pagado = header.Pagado,
                    Pendiente = header.Pendiente,
                    Items = itemsPrint
                };

                // Encola foto fiscal solo si FiscalActivo (no-op si off). No espera worker.
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
                catch
                {
                    // Nunca tumbar venta
                }

                // Contabilidad automática (no-op si Contabilidad apagada)
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
                    var montoCredito = header.Pendiente;
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
            catch (Exception ex)
            {
                var detalle = ex.InnerException?.Message ?? ex.Message;
                Console.WriteLine($"ProcesarFactura error: {detalle}");

                // Si el documento ya se guardó, no devolver error: el cobro ocurrió
                // y debe verse como factura (tipo 1).
                if (header != null && header.IdFacturaHeader > 0)
                {
                    try
                    {
                        header.IdTipoDocumentos = 1;
                        header.Clientes = null;
                        _facturaHeader.UpdateFacturaHeader(header.IdFacturaHeader, header);
                    }
                    catch (Exception exUpdate)
                    {
                        Console.WriteLine($"ProcesarFactura no pudo marcar tipo 1: {exUpdate.Message}");
                    }

                    return Ok(new
                    {
                        message = "Factura procesada correctamente",
                        idFactura = header.IdFacturaHeader,
                        advertencia = detalle
                    });
                }

                return BadRequest(detalle);
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

        [HttpGet]
        [Route("GetAllOrdenesByFecha")]
        public async Task<IEnumerable<FacturaHeaderDto>> GetAllOrdenesByFecha(
        int IdEmpresa,
        DateTime fechaDesde,
        DateTime fechaHasta)
        {
            var listaReturn = new List<FacturaHeaders>();

            // 🔥 Buscar headers por rango de fecha
            var headers = await _facturaHeader.GetAllOrdenesByFecha(
                IdEmpresa,
                fechaDesde,
                fechaHasta);

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

            return _Mapper.Map<FacturaHeaderDto[]>(listaReturn);
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
                header.Pendiente = header.Total - header.Pagado;
                header.TipoFactura = "Credito";
                header.Estado = header.Pendiente > 0 ? "Pendiente" : "Pagada";
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
        public async Task<IActionResult> GetCotizacionPublica(string token)
        {
            var vista = await _facturaHeader.ObtenerCotizacionPublicaAsync(token);

            if (vista == null)
                return NotFound(new { message = "Cotización no encontrada o enlace inválido." });

            return Ok(vista);
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
        public async Task<IActionResult> AnularFactura([FromBody] AnularFacturaDto dto)
        {
            if (dto == null)
                return BadRequest("Datos de anulación requeridos");

            if (string.IsNullOrWhiteSpace(dto.MotivoAnulacion))
                return BadRequest("Debe indicar el motivo de anulación");

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
            // No sobrescribir FechaInseccion: es la fecha del documento original.
            factura.Clientes = null;
            _facturaHeader.UpdateFacturaHeader(dto.IdFacturaHeader, factura);

            // 🔴 revertir ingresos
            await _IngresosServices.RevertirIngresoPorFactura(
                dto.IdFacturaHeader,
                dto.IdEmpresa);

            var idUsuarioAnula = dto.IdUsuario > 0 ? dto.IdUsuario : (factura.IdUsuario ?? 0);

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
                    IdUsuario = dto.IdUsuario > 0 ? dto.IdUsuario : (factura.IdUsuario ?? 0),
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
    } 


}

