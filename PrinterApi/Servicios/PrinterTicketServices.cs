using ESCPOS_NET;
using ESCPOS_NET.Emitters;
using ESCPOS_NET.Utilities;
using PrinterApi.Dto;
using PrinterApi.Dto.PrinterApi.Dto;
using PrinterApi.Interfaz;
using PrinterApi.Servicios;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;

using TicketNotaCreditoDto = AlahiaPos.Entities.Dto.TicketNotaCreditoDto;
using TicketFechaHora = AlahiaPos.Entities.Dto.TicketFechaHora;



public class PrinterTicketServices : IPrinterTicket
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly IPrinterLocalSettings _printerSettings;

    public PrinterTicketServices(HttpClient http, IConfiguration config, IPrinterLocalSettings printerSettings)
    {
        _http = http;
        _baseUrl = config["ApiBaseUrl"];
        _printerSettings = printerSettings;
    }

    private string PrinterFactura => _printerSettings.Factura;
    private string PrinterLavador => _printerSettings.Lavador;

    // ============================
    // ?? TICKET LAVADOR
    // ============================
    public async Task GenerateTicketLavador(int idFacturaHeader)
    {
        try
        {
            var listado = await _http.GetFromJsonAsync<List<TicketLavadorDto>>(
                $"{_baseUrl}/api/FacturaHeader/ticket-lavador/{idFacturaHeader}");

            

            if (listado == null || listado.Count == 0)
            {
                Console.WriteLine("No hay tickets.");
                return;
            }

            var emitter = new EPSON();

            foreach (var ticket in listado)
            {
                var bytes = new List<byte>();

                bytes.AddRange(emitter.Initialize());

                bytes.AddRange(emitter.CenterAlign());
                bytes.AddRange(emitter.SetStyles(PrintStyle.Bold | PrintStyle.DoubleWidth | PrintStyle.DoubleHeight));
                bytes.AddRange(emitter.PrintLine("AUTOSERVICIOSTOTALCLEAN N&C.,S.R.L"));

                bytes.AddRange(emitter.SetStyles(PrintStyle.None));
                bytes.AddRange(emitter.PrintLine("TICKET LAVADOR"));
                bytes.AddRange(emitter.PrintLine("--------------------------------"));

                bytes.AddRange(emitter.LeftAlign());
                bytes.AddRange(emitter.PrintLine($"Factura : {ticket.NumeroFactura}"));
                bytes.AddRange(emitter.PrintLine($"Fecha   : {DateTime.Now:dd/MM/yyyy hh:mm tt}"));
              
                bytes.AddRange(emitter.PrintLine($"Cliente : {ticket.Cliente}"));

                bytes.AddRange(emitter.PrintLine("--------------------------------"));

                bytes.AddRange(emitter.CenterAlign());
                bytes.AddRange(emitter.SetStyles(PrintStyle.Bold | PrintStyle.DoubleHeight));
                bytes.AddRange(emitter.PrintLine($"LAVADOR: {ticket.AtendidoPor}"));

                bytes.AddRange(emitter.SetStyles(PrintStyle.None));
                bytes.AddRange(emitter.PrintLine("--------------------------------"));

                bytes.AddRange(emitter.LeftAlign());
                bytes.AddRange(emitter.PrintLine("CANT   SERVICIO"));

                foreach (var det in ticket.Servicios)
                {
                    bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));
                    bytes.AddRange(emitter.PrintLine($"{det.Cantidad}   {det.Servicio} {(det.Precio * det.Cantidad)}"));
                    bytes.AddRange(emitter.SetStyles(PrintStyle.None));
                }

                bytes.AddRange(emitter.PrintLine("--------------------------------"));
                bytes.AddRange(emitter.CenterAlign());
                bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));
                bytes.AddRange(emitter.PrintLine("GRACIAS POR SU TRABAJO"));

                bytes.AddRange(emitter.FeedLines(4));
                bytes.AddRange(emitter.FullCut());

                // ? USA CONFIG
                RawPrinterHelper.SendBytesToPrinter(PrinterLavador, bytes.ToArray());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error lavador: {ex.Message}");
        }
    }
    public async Task GenerateFactDirect(int idFactura)
    {
        try
        {
            var factura = await _http.GetFromJsonAsync<TicketFacturaClienteDto>(
                $"{_baseUrl}/api/FacturaHeader/factura-cliente/{idFactura}");

            if (factura == null)
            {
                Console.WriteLine("Factura no encontrada.");
                return;
            }

            PrintDocument pd = new PrintDocument();

            // ?? AQU? eliges la impresora
            //pd.PrinterSettings.PrinterName = PrinterFactura;
            pd.PrinterSettings.PrinterName = PrinterFactura;

            pd.PrintController = new StandardPrintController(); // ?? SIN DI?LOGO

            pd.PrintPage += (sender, e) =>
            {
                float y = 20;
                float left = 20;

                Font normal = new Font("Arial", 10);
                Font bold = new Font("Arial", 12, FontStyle.Bold);

                // ?? EMPRESA
                e.Graphics.DrawString(factura.NombreEmpresa, bold, Brushes.Black, left, y);
                y += 25;

                e.Graphics.DrawString(factura.DireccionEmpresa, normal, Brushes.Black, left, y);
                y += 20;

                e.Graphics.DrawString($"Tel: {factura.TelefonoEmpresa}", normal, Brushes.Black, left, y);
                y += 25;

                e.Graphics.DrawString("--------------------------------------------", normal, Brushes.Black, left, y);
                y += 20;

                // ?? INFO
                e.Graphics.DrawString($"Factura: {factura.NumeroFactura}", normal, Brushes.Black, left, y);
                y += 20;

                e.Graphics.DrawString($"Fecha: {factura.Fecha:dd/MM/yyyy} {factura.Hora}", normal, Brushes.Black, left, y);
                y += 20;

                e.Graphics.DrawString($"Cliente: {factura.Cliente}", normal, Brushes.Black, left, y);
                y += 20;

                if (!string.IsNullOrWhiteSpace(factura.RncCliente))
                {
                    e.Graphics.DrawString($"RNC/Ced: {factura.RncCliente}", normal, Brushes.Black, left, y);
                    y += 20;
                }

                if (!string.IsNullOrWhiteSpace(factura.NCF))
                {
                    e.Graphics.DrawString(
                        factura.EsComprobanteElectronico
                            ? $"e-NCF: {factura.NCF}"
                            : $"NCF: {factura.NCF}",
                        normal, Brushes.Black, left, y);
                    y += 20;
                }

                if (!string.IsNullOrWhiteSpace(factura.TipoFactura))
                {
                    e.Graphics.DrawString($"Tipo: {factura.TipoFactura}", normal, Brushes.Black, left, y);
                    y += 20;
                }

                if (!string.IsNullOrWhiteSpace(factura.FormaPago))
                {
                    e.Graphics.DrawString($"Forma Pago: {factura.FormaPago}", normal, Brushes.Black, left, y);
                    y += 20;
                }

                if (!string.IsNullOrWhiteSpace(factura.SecurityCode))
                {
                    e.Graphics.DrawString($"Cod. Seguridad: {factura.SecurityCode}", normal, Brushes.Black, left, y);
                    y += 20;
                }

                var ff = TicketFechaHora.ParaImpresion(
                    factura.FechaFirma,
                    factura.FechaEmisionEcf,
                    factura.Fecha,
                    factura.Hora);
                if (ff.HasValue)
                {
                    e.Graphics.DrawString($"F. Firma: {ff:dd/MM/yyyy HH:mm}", normal, Brushes.Black, left, y);
                    y += 20;
                }

                y += 5;

                e.Graphics.DrawString("--------------------------------------------", normal, Brushes.Black, left, y);
                y += 20;

                // ?? DETALLE
                foreach (var det in factura.Detalles)
                {
                    e.Graphics.DrawString($"{det.Cantidad} x {det.Descripcion}", normal, Brushes.Black, left, y);
                    y += 20;

                    e.Graphics.DrawString($"RD$ {det.Precio:N2}", normal, Brushes.Black, left + 400, y);
                    y += 20;
                }

                y += 10;

                e.Graphics.DrawString("--------------------------------------------", normal, Brushes.Black, left, y);
                y += 20;

                // ?? TOTAL
                e.Graphics.DrawString($"TOTAL: RD$ {factura.Total:N2}", bold, Brushes.Black, left, y);
                y += 30;

                if (!string.IsNullOrWhiteSpace(factura.UrlQR))
                {
                    e.Graphics.DrawString("Consulte el e-CF (QR) en DGII", normal, Brushes.Black, left, y);
                    y += 20;
                    e.Graphics.DrawString(factura.UrlQR, new Font("Arial", 7), Brushes.Black, left, y);
                    y += 25;
                }

                e.Graphics.DrawString("GRACIAS POR SU COMPRA", normal, Brushes.Black, left, y);
            };

            // ?? AQU? SE MANDA DIRECTO A LA IMPRESORA
            pd.Print();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error impresi?n directa: {ex.Message}");
        }
    }
    // ============================
    // ?? FACTURA CLIENTE
    // ============================

  

public async Task GenerateTicketBizcocho(int idFacturaHeader, int idEmpresa)
{
    try
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var enc = Encoding.GetEncoding(850); // Español / acentos

        if (string.IsNullOrWhiteSpace(_baseUrl))
            throw new InvalidOperationException(
                "ApiBaseUrl no configurado en PrinterApi (appsettings.Local.json).");

        if (string.IsNullOrWhiteSpace(PrinterFactura))
            throw new InvalidOperationException(
                "Impresora de factura no configurada en el agente de impresión.");

        FacturaHeaderDto? factura = null;
        try
        {
            factura = await _http.GetFromJsonAsync<FacturaHeaderDto>(
                $"{_baseUrl}/api/BizcochoEncargo/print" +
                $"?IdFacturaHeader={idFacturaHeader}" +
                $"&IdEmpresa={idEmpresa}"
            );
        }
        catch (Exception ex)
        {
            // Listado de facturas / POS: si falla el endpoint de encargo, usar ticket cliente.
            Console.WriteLine(
                $"Bizcocho/print falló ({ex.Message}). Reintento con factura-cliente {idFacturaHeader}.");
            await GenerateTicketFacturaCliente(idFacturaHeader);
            return;
        }

        if (factura == null ||
            factura.FacturaDetalles == null ||
            !factura.FacturaDetalles.Any())
        {
            await GenerateTicketFacturaCliente(idFacturaHeader);
            return;
        }

            // Factura de venta POS → ticket cliente (TOTAL / PAGADO / PENDIENTE + e-CF)
            if (factura.IdTipoDocumentos == 1)
            {
                await GenerateTicketFacturaCliente(idFacturaHeader);
                return;
            }

            bool esEncargo = factura.IdTipoDocumentos == 14;
            bool esOrden = factura.IdTipoDocumentos == 10;
            bool esCotizacion = factura.IdTipoDocumentos == 2;
            bool esFacturaFinal = factura.IdTipoDocumentos == 1;

            var emitter = new EPSON();
        var bytes = new List<byte>();

        void Line(string text = "")
        {
            text = LimpiarTextoTicket(text);
            bytes.AddRange(enc.GetBytes(text + "\n"));
        }

        void CenterLine(string text = "")
        {
            bytes.AddRange(emitter.CenterAlign());
            Line(text);
        }

        void LeftLine(string text = "")
        {
            bytes.AddRange(emitter.LeftAlign());
            Line(text);
        }

        void Separator()
        {
            Line("--------------------------------");
        }

        void WrappedCenter(string text, int max = 32)
        {
            bytes.AddRange(emitter.CenterAlign());
            foreach (var line in DividirTextoTicket(text, max))
                Line(line);
        }

        void WrappedLeft(string text, int max = 32)
        {
            bytes.AddRange(emitter.LeftAlign());
            foreach (var line in DividirTextoTicket(text, max))
                Line(line);
        }

        bytes.AddRange(emitter.Initialize());

        // ?? Tabla de caracteres espa?ol CP850
        bytes.AddRange(new byte[] { 0x1B, 0x74, 0x02 });

        // ============================
        // HEADER EMPRESA
        // ============================

        bytes.AddRange(emitter.CenterAlign());

        string nombreEmpresa = factura.Empresa ?? "";
        string direccion = factura.DireccionEmpresa ?? "";

        // Si el nombre es largo, NO usar doble ancho porque rompe el ticket
        if (nombreEmpresa.Length <= 16)
        {
            bytes.AddRange(emitter.SetStyles(
                PrintStyle.Bold |
                PrintStyle.DoubleWidth |
                PrintStyle.DoubleHeight
            ));

            CenterLine(nombreEmpresa);
        }
        else
        {
            bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));
            WrappedCenter(nombreEmpresa.ToUpper(), 32);
        }

        bytes.AddRange(emitter.SetStyles(PrintStyle.None));

        WrappedCenter(direccion, 32);

        CenterLine($"RNC: {factura.RNCEmpresa}");
        CenterLine($"Tel: {factura.TelefonoEmpresa}");

        Separator();

        bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));
            if (esFacturaFinal)
                CenterLine(TituloComprobanteElectronico(
                    factura.EsComprobanteElectronico,
                    factura.TipoECF,
                    factura.NCF));
            else if (esOrden)
                CenterLine("ORDEN");
            else if (esCotizacion)
                CenterLine("COTIZACION");
            else
                CenterLine("ENCARGO BIZCOCHO");
            
            Separator();

        // ============================
        // DATOS DOCUMENTO
        // ============================

        bytes.AddRange(emitter.LeftAlign());

            var hora = DateTime.Now.Hour < 12 ? "AM" : "PM";
            


            if (esOrden || esCotizacion)
            {
                LeftLine(
                    esCotizacion
                        ? $"#Cotizacion :0000 {factura.IdFacturaHeader}"
                        : $"#Orden     :0000 {factura.IdFacturaHeader}");
                
                LeftLine($"Fecha      : {DateTime.Now:dd/MM/yyyy}");
                LeftLine($"Hora : {DateTime.Now:hh:mm} {hora}");
            }
              
            else
            {
                LeftLine($"#Documento : {factura.NumeroDocumento}");
                LeftLine($"Fecha      : {factura.FechaInseccion:dd/MM/yyyy}");
                LeftLine($"Hora : {DateTime.Now:hh:mm} {hora}");
                LeftLine($"Forma Pago : {factura.FormaPago}");
            }
               

        if (esFacturaFinal)
        {
            if (!string.IsNullOrWhiteSpace(factura.NCF))
            {
                LeftLine(factura.EsComprobanteElectronico
                    ? $"e-NCF      : {factura.NCF}"
                    : $"NCF        : {factura.NCF}");
            }

            // NombreEmpresa en FacturaHeader = nombre del cliente en POS (no la razón social del emisor).
            string clienteFactura =
                !string.IsNullOrWhiteSpace(factura.NombreEmpresa)
                    ? factura.NombreEmpresa
                    : !string.IsNullOrWhiteSpace(factura.cliente)
                        ? factura.cliente
                        : "Consumidor Final";

            WrappedLeft($"Cliente    : {clienteFactura}", 32);

            if (!string.IsNullOrWhiteSpace(factura.RNC))
                LeftLine($"RNC Cliente: {factura.RNC}");
        }
            else
            {
                WrappedLeft($"Cliente    : {factura.cliente}", 32);

                if (!string.IsNullOrWhiteSpace(factura.celular))
                    LeftLine($"Celular    : {factura.celular}");

                // Solo los encargos muestran entrega
                if (esEncargo)
                {
                    LeftLine($"Entrega    : {factura.FechaEntrega:dd/MM/yyyy}");
                    LeftLine($"Hora       : {factura.HoraEntrega:hh:mm tt}");
                }
            }

            Separator();

        // ============================
        // DETALLES
        // ============================

        foreach (var det in factura.FacturaDetalles)
        {
            var totalLinea = det.SubTotal;
            var itbisLinea = det.Itbis;
            var subtotalLinea = totalLinea - itbisLinea;

            bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));
            var nombreProducto =
                !string.IsNullOrWhiteSpace(det.Descripcion)
                    ? det.Descripcion
                    : (det.Productos?.nombre ?? "Producto");
            WrappedLeft(nombreProducto!, 32);
            bytes.AddRange(emitter.SetStyles(PrintStyle.None));

            if (!string.IsNullOrWhiteSpace(det.TipoMasa))
                LeftLine($"Masa      : {det.TipoMasa}");

            if (!string.IsNullOrWhiteSpace(det.TipoRelleno))
                WrappedLeft($"Relleno   : {det.TipoRelleno}", 32);

            if (det.Libras > 0)
                LeftLine($"Libras    : {det.Libras}");

            LeftLine($"Cantidad  : {det.Cantidad:N2}");
            LeftLine($"Precio    : RD$ {subtotalLinea:N2}");

            if (itbisLinea > 0)
            {
                LeftLine($"ITBIS     : RD$ {itbisLinea:N2}");
                LeftLine($"Subtotal  : RD$ {totalLinea:N2}");
            }

            Separator();
        }

        // ============================
        // TOTALES
        // ============================

        bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));

            if (esEncargo)
            {
                LeftLine($"ABONO     : RD$ {factura.Pagado:N2}");
                LeftLine($"PENDIENTE : RD$ {factura.Pendiente:N2}");
            }
            if (esFacturaFinal)
            {
                LeftLine($"SUBTOTAL  : RD$ {factura.SubTotal:N2}");

               
                    LeftLine($"DESCUENTO : RD$ {factura.TotalDescuento:N2}");

                LeftLine($"ITBIS     : RD$ {factura.TotalItbis:N2}");

                Separator();
            }




            decimal totalFinal = factura.Total - factura.TotalDescuento;

            LeftLine($"TOTAL     : RD$ {totalFinal:N2}");

            if (esFacturaFinal && factura.Pendiente > 0.02m)
            {
                LeftLine($"PAGADO    : RD$ {factura.Pagado:N2}");
                LeftLine($"PENDIENTE : RD$ {factura.Pendiente:N2}");
            }

            bytes.AddRange(emitter.SetStyles(PrintStyle.None));

        Separator();

        bytes.AddRange(emitter.CenterAlign());
        Line("GRACIAS POR SU PREFERENCIA");
            Separator();
            if (factura.IdTipoDocumentos == 1 &&
     !string.IsNullOrWhiteSpace(factura.Politicas))
            {
                bytes.AddRange(emitter.CenterAlign());
                bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));
                Line("POLITICAS CORPORATIVAS");
                bytes.AddRange(emitter.SetStyles(PrintStyle.None));

                Separator();

                WrappedLeft(factura.Politicas, 32);
                Separator();
            }

            if (esFacturaFinal)
            {
                AppendEcfFiscalBlockEscPos(
                    bytes,
                    emitter,
                    factura.EsComprobanteElectronico,
                    factura.TipoECF,
                    factura.SecurityCode,
                    factura.UrlQR,
                    factura.FechaFirma ?? factura.FechaEmisionEcf,
                    factura.EstadoDgii,
                    factura.RNCEmpresa,
                    factura.RNC,
                    factura.NCF,
                    factura.Total,
                    factura.FechaEmisionEcf ?? factura.FechaInseccion,
                    factura.Hora,
                    factura.FechaInseccion);
            }

            bytes.AddRange(emitter.FeedLines(4));
        bytes.AddRange(emitter.CashDrawerOpenPin2());
        bytes.AddRange(emitter.FullCut());

            RawPrinterHelper.SendBytesToPrinter(
            PrinterFactura,
                bytes.ToArray()
            );
        }
    catch (Exception ex)
    {
        Console.WriteLine($"Error ticket bizcocho: {ex}");
        throw;
    }
}

private static string LimpiarTextoTicket(string texto)
{
    if (string.IsNullOrWhiteSpace(texto))
        return "";

    return texto
        .Replace("=", "?")
        .Replace("?", "")
        .Replace("??", "")
        .Replace("?", "-")
        .Replace("?", "-")
        .Replace("?", "\"")
        .Replace("?", "\"")
        .Replace("?", "'")
        .Trim();
}

private static List<string> DividirTextoTicket(string texto, int max)
{
    texto = LimpiarTextoTicket(texto);

    var lineas = new List<string>();

    if (string.IsNullOrWhiteSpace(texto))
        return lineas;

    var palabras = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var linea = "";

    foreach (var palabra in palabras)
    {
        if ((linea + " " + palabra).Trim().Length > max)
        {
            if (!string.IsNullOrWhiteSpace(linea))
                lineas.Add(linea.Trim());

            linea = palabra;
        }
        else
        {
            linea += " " + palabra;
        }
    }

    if (!string.IsNullOrWhiteSpace(linea))
        lineas.Add(linea.Trim());

    return lineas;
}
public async Task GenerateTicketFacturaCliente(int idFactura)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_baseUrl))
                throw new InvalidOperationException(
                    "ApiBaseUrl no configurado en PrinterApi (appsettings.Local.json).");

            if (string.IsNullOrWhiteSpace(PrinterFactura))
                throw new InvalidOperationException(
                    "Impresora de factura no configurada en el agente.");

            var factura = await _http.GetFromJsonAsync<TicketFacturaClienteDto>(
                $"{_baseUrl}/api/FacturaHeader/factura-cliente/{idFactura}");

            if (factura == null)
                throw new InvalidOperationException(
                    $"Factura {idFactura} no encontrada en la API ({_baseUrl}).");

            var emitter = new EPSON();
            var bytes = new List<byte>();

            bytes.AddRange(emitter.Initialize());

            bytes.AddRange(emitter.CenterAlign());
            bytes.AddRange(emitter.SetStyles(PrintStyle.Bold | PrintStyle.DoubleWidth | PrintStyle.DoubleHeight));
            bytes.AddRange(emitter.PrintLine(factura.NombreEmpresa ?? ""));

            bytes.AddRange(emitter.SetStyles(PrintStyle.None));
            bytes.AddRange(emitter.PrintLine(factura.DireccionEmpresa ?? ""));
            if (!string.IsNullOrWhiteSpace(factura.RncEmpresa))
                bytes.AddRange(emitter.PrintLine($"RNC: {factura.RncEmpresa}"));
            bytes.AddRange(emitter.PrintLine($"Tel: {factura.TelefonoEmpresa}"));

            bytes.AddRange(emitter.PrintLine("--------------------------------"));

            bytes.AddRange(emitter.CenterAlign());
            bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));
            bytes.AddRange(emitter.PrintLine(
                TituloComprobanteElectronico(
                    factura.EsComprobanteElectronico,
                    factura.TipoECF,
                    factura.NCF,
                    facturaCliente: true)));

            bytes.AddRange(emitter.PrintLine("--------------------------------"));

            bytes.AddRange(emitter.LeftAlign());
            bytes.AddRange(emitter.PrintLine(
                !string.IsNullOrWhiteSpace(factura.NumeroDocumento)
                    ? $"Documento: {factura.NumeroDocumento}"
                    : $"Factura : {factura.NumeroFactura}"));
            bytes.AddRange(emitter.PrintLine($"Fecha   : {factura.Fecha:dd/MM/yyyy} {factura.Hora}"));
            bytes.AddRange(emitter.PrintLine($"Cliente : {factura.Cliente}"));
            if (!string.IsNullOrWhiteSpace(factura.RncCliente))
                bytes.AddRange(emitter.PrintLine($"RNC/Ced : {factura.RncCliente}"));

            if (!string.IsNullOrWhiteSpace(factura.NCF))
            {
                bytes.AddRange(emitter.PrintLine(
                    factura.EsComprobanteElectronico
                        ? $"e-NCF   : {factura.NCF}"
                        : $"NCF     : {factura.NCF}"));
            }

            AppendTipoYFormaPagoEscPos(bytes, emitter, factura);

            bytes.AddRange(emitter.PrintLine("--------------------------------"));

            bytes.AddRange(emitter.PrintLine("CANT   DESCRIPCION"));

            foreach (var det in factura.Detalles)
            {
                bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));
                bytes.AddRange(emitter.PrintLine($"{det.Cantidad}   {det.Descripcion}"));

                bytes.AddRange(emitter.SetStyles(PrintStyle.None));
                bytes.AddRange(emitter.PrintLine($"       RD$ {det.Precio:N2}"));
            }

            bytes.AddRange(emitter.PrintLine("--------------------------------"));

            if (factura.SubTotal > 0)
                bytes.AddRange(emitter.PrintLine($"SubTotal RD$ {factura.SubTotal:N2}"));
            if (factura.TotalDescuento > 0)
                bytes.AddRange(emitter.PrintLine($"Desc.    RD$ {factura.TotalDescuento:N2}"));
            if (factura.TotalItbis > 0)
                bytes.AddRange(emitter.PrintLine($"ITBIS    RD$ {factura.TotalItbis:N2}"));

            bytes.AddRange(emitter.CenterAlign());
            bytes.AddRange(emitter.SetStyles(PrintStyle.Bold | PrintStyle.DoubleWidth | PrintStyle.DoubleHeight));
            bytes.AddRange(emitter.PrintLine($"TOTAL RD$ {factura.Total:N2}"));

            bytes.AddRange(emitter.SetStyles(PrintStyle.None));
            bytes.AddRange(emitter.LeftAlign());
            if (factura.Pendiente > 0.02m)
            {
                bytes.AddRange(emitter.PrintLine($"PAGADO   RD$ {factura.Pagado:N2}"));
                bytes.AddRange(emitter.PrintLine($"PENDIENTE RD$ {factura.Pendiente:N2}"));
            }

            bytes.AddRange(emitter.PrintLine("--------------------------------"));

            AppendEcfFiscalBlockEscPos(
                bytes,
                emitter,
                factura.EsComprobanteElectronico,
                factura.TipoECF,
                factura.SecurityCode,
                factura.UrlQR,
                factura.FechaFirma ?? factura.FechaEmisionEcf,
                factura.EstadoDgii,
                factura.RncEmpresa,
                factura.RncCliente,
                factura.NCF,
                factura.Total,
                factura.FechaEmisionEcf ?? factura.Fecha,
                factura.Hora,
                factura.Fecha);

            bytes.AddRange(emitter.CenterAlign());
            bytes.AddRange(emitter.PrintLine("GRACIAS POR PREFERIRNOS"));

            bytes.AddRange(emitter.FeedLines(4));
            // Abrir caj?n (pin 2)
            bytes.AddRange(emitter.CashDrawerOpenPin2());
            bytes.AddRange(emitter.FullCut());

            // ? USA CONFIG
            RawPrinterHelper.SendBytesToPrinter(PrinterFactura, bytes.ToArray());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error factura: {ex.Message}");
            throw;
        }
    }

    public async Task<byte[]> PreviewTicketFacturaClientePdfAsync(int idFactura)
    {
        var factura = await _http.GetFromJsonAsync<TicketFacturaClienteDto>(
            $"{_baseUrl}/api/FacturaHeader/factura-cliente/{idFactura}");

        if (factura == null)
            throw new InvalidOperationException($"Factura {idFactura} no encontrada.");

        return PrinterApi.Servicios.TicketFacturaPreviewPdf.Build(factura);
    }

    private static void AppendTipoYFormaPagoEscPos(
        List<byte> bytes,
        EPSON emitter,
        TicketFacturaClienteDto factura)
    {
        var pagos = (factura.Pagos ?? new List<TicketFacturaClientePagoDto>())
            .Where(p => p != null && p.Monto > 0 && !string.IsNullOrWhiteSpace(p.Metodo))
            .ToList();

        var formaPago = (factura.FormaPago ?? "").Trim();
        if (string.IsNullOrWhiteSpace(formaPago) && pagos.Count == 1)
            formaPago = pagos[0].Metodo;
        else if (string.IsNullOrWhiteSpace(formaPago) && pagos.Count > 1)
            formaPago = "Mixto";

        if (string.IsNullOrWhiteSpace(factura.TipoFactura)
            && string.IsNullOrWhiteSpace(formaPago))
            return;

        bytes.AddRange(emitter.LeftAlign());
        bytes.AddRange(emitter.SetStyles(PrintStyle.None));

        if (!string.IsNullOrWhiteSpace(factura.TipoFactura))
            bytes.AddRange(emitter.PrintLine($"Tipo     : {factura.TipoFactura}"));

        if (!string.IsNullOrWhiteSpace(formaPago))
            bytes.AddRange(emitter.PrintLine($"Forma Pago: {formaPago}"));
    }

    /// <summary>
    /// Bloque fiscal e-CF para recibo: e-NCF ya va arriba; aquí código seguridad + QR.
    /// Nunca imprime TrackId (uso interno).
    /// UrlQR debe ser URL de ConsultaTimbre (no data:image). Si viene imagen truncada,
    /// se reconstruye la URL con RNC/e-NCF/código de seguridad.
    /// </summary>
    private static void AppendEcfFiscalBlockEscPos(
        List<byte> bytes,
        EPSON emitter,
        bool esElectronico,
        string? tipoEcf,
        string? securityCode,
        string? urlQr,
        DateTime? fechaFirma,
        string? estadoDgii,
        string? rncEmisor = null,
        string? rncComprador = null,
        string? encf = null,
        decimal? montoTotal = null,
        DateTime? fechaEmision = null,
        string? hora = null,
        DateTime? fechaDocumento = null)
    {
        if (!esElectronico
            && string.IsNullOrWhiteSpace(securityCode)
            && string.IsNullOrWhiteSpace(urlQr))
            return;

        bytes.AddRange(emitter.CenterAlign());
        bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));
        bytes.AddRange(emitter.PrintLine("DATOS DGII e-CF"));
        bytes.AddRange(emitter.SetStyles(PrintStyle.None));

        bytes.AddRange(emitter.LeftAlign());
        if (!string.IsNullOrWhiteSpace(tipoEcf))
            bytes.AddRange(emitter.PrintLine($"Tipo e-CF: {tipoEcf}"));

        if (!string.IsNullOrWhiteSpace(securityCode))
            bytes.AddRange(emitter.PrintLine($"Cod.Seguridad: {securityCode}"));

        var fFirmaVisible = TicketFechaHora.ParaImpresion(
            fechaFirma,
            fechaEmision,
            fechaDocumento,
            hora);
        if (fFirmaVisible.HasValue)
            bytes.AddRange(emitter.PrintLine($"F.Firma : {fFirmaVisible:dd/MM/yyyy HH:mm}"));

        // Estado legible al cliente solo si es Aceptado (no TrackId)
        if (!string.IsNullOrWhiteSpace(estadoDgii)
            && estadoDgii.Contains("Acept", StringComparison.OrdinalIgnoreCase))
        {
            bytes.AddRange(emitter.PrintLine($"Estado  : {estadoDgii}"));
        }

        var qrPayload = ResolveQrPayloadForThermal(
            urlQr,
            securityCode,
            rncEmisor,
            rncComprador,
            encf,
            montoTotal,
            fechaEmision,
            fechaFirma);

        if (!string.IsNullOrWhiteSpace(qrPayload))
        {
            bytes.AddRange(emitter.CenterAlign());
            bytes.AddRange(emitter.PrintLine("Escanee el codigo QR"));
            bytes.AddRange(emitter.PrintQRCode(
                qrPayload,
                TwoDimensionCodeType.QRCODE_MODEL2,
                Size2DCode.NORMAL,
                CorrectionLevel2DCode.PERCENT_15));
            bytes.AddRange(emitter.FeedLines(1));
        }

        bytes.AddRange(emitter.PrintLine("--------------------------------"));
    }

    private static string TituloComprobanteElectronico(
        bool esElectronico,
        string? tipoEcf,
        string? ncf,
        bool facturaCliente = false)
    {
        if (!esElectronico)
            return facturaCliente ? "FACTURA CLIENTE" : "FACTURA";

        var tipo = (tipoEcf ?? "").Trim();
        if (tipo.Length == 0 && !string.IsNullOrWhiteSpace(ncf) && ncf.Length >= 3
            && ncf.StartsWith("E", StringComparison.OrdinalIgnoreCase))
            tipo = ncf.Substring(1, 2);

        // "32" / "E32" / "Tipo 32"
        if (tipo.StartsWith("E", StringComparison.OrdinalIgnoreCase) && tipo.Length >= 3)
            tipo = tipo.Substring(1, 2);
        if (tipo.Length > 2)
            tipo = new string(tipo.Where(char.IsDigit).Take(2).ToArray());

        return tipo switch
        {
            "31" => "FACTURA CREDITO FISCAL e-CF",
            "32" => "FACTURA DE CONSUMO e-CF",
            "33" => "NOTA DE DEBITO e-CF",
            "34" => "NOTA DE CREDITO e-CF",
            "41" => "COMPROBANTE COMPRAS e-CF",
            "43" => "COMPROBANTE GASTOS MENORES e-CF",
            "44" => "COMPROBANTE REGIMEN ESPECIAL e-CF",
            "45" => "COMPROBANTE GUBERNAMENTAL e-CF",
            "46" => "COMPROBANTE EXPORTACION e-CF",
            "47" => "COMPROBANTE PAGOS AL EXTERIOR e-CF",
            _ => "COMPROBANTE FISCAL ELECTRONICO"
        };
    }

    /// <summary>
    /// ESC/POS PrintQRCode necesita texto/URL corta. data:image (Invoice) no sirve.
    /// </summary>
    private static string? ResolveQrPayloadForThermal(
        string? urlQr,
        string? securityCode,
        string? rncEmisor,
        string? rncComprador,
        string? encf,
        decimal? montoTotal,
        DateTime? fechaEmision,
        DateTime? fechaFirma)
    {
        if (!string.IsNullOrWhiteSpace(urlQr))
        {
            var u = urlQr.Trim();
            if (u.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return u;
        }

        if (string.IsNullOrWhiteSpace(securityCode)
            || string.IsNullOrWhiteSpace(encf)
            || string.IsNullOrWhiteSpace(rncEmisor))
            return null;

        static string Digitos(string? s) =>
            string.IsNullOrWhiteSpace(s)
                ? ""
                : new string(s.Where(char.IsDigit).ToArray());

        var firma = fechaFirma ?? DateTime.Now;
        var emision = fechaEmision ?? firma;
        var monto = (montoTotal ?? 0m).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

        // Misma forma que DgiiDirectoMapper.BuildQrUrl (producción).
        var qs = string.Join("&", new[]
        {
            "RncEmisor=" + Uri.EscapeDataString(Digitos(rncEmisor)),
            "RncComprador=" + Uri.EscapeDataString(Digitos(rncComprador)),
            "ENCF=" + Uri.EscapeDataString(encf.Trim()),
            "FechaEmision=" + Uri.EscapeDataString(emision.ToString("dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture)),
            "MontoTotal=" + Uri.EscapeDataString(monto),
            "FechaFirma=" + Uri.EscapeDataString(firma.ToString("dd-MM-yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)),
            "CodigoSeguridad=" + Uri.EscapeDataString(securityCode.Trim())
        });

        return "https://ecf.dgii.gov.do/ecf/ConsultaTimbre?" + qs;
    }

    public async Task GenerateTicketNotaCredito(
        int idNotaCredito,
        int idEmpresa)
    {
        try
        {
            var nota = await _http.GetFromJsonAsync<TicketNotaCreditoDto>(
                $"{_baseUrl}/api/NotasCredito/ticket/{idNotaCredito}/{idEmpresa}");

            if (nota == null)
            {
                Console.WriteLine("Nota de cr?dito no encontrada.");
                return;
            }

            var emitter = new EPSON();
            var bytes = new List<byte>();

            bytes.AddRange(emitter.Initialize());
            bytes.AddRange(emitter.CenterAlign());
            bytes.AddRange(emitter.SetStyles(
                PrintStyle.Bold | PrintStyle.DoubleWidth));
            bytes.AddRange(emitter.PrintLine(nota.NombreEmpresa));
            bytes.AddRange(emitter.SetStyles(PrintStyle.None));
            bytes.AddRange(emitter.PrintLine(nota.DireccionEmpresa));
            bytes.AddRange(emitter.PrintLine($"Tel: {nota.TelefonoEmpresa}"));
            bytes.AddRange(emitter.PrintLine("--------------------------------"));
            bytes.AddRange(emitter.CenterAlign());
            bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));
            bytes.AddRange(emitter.PrintLine("NOTA DE CREDITO"));
            bytes.AddRange(emitter.SetStyles(PrintStyle.None));
            bytes.AddRange(emitter.PrintLine("--------------------------------"));
            bytes.AddRange(emitter.LeftAlign());
            bytes.AddRange(emitter.PrintLine($"No. NC  : {nota.NumeroDocumento}"));

            if (!string.IsNullOrWhiteSpace(nota.NCF))
            {
                bytes.AddRange(emitter.PrintLine(
                    nota.NCF.StartsWith("E", StringComparison.OrdinalIgnoreCase)
                        ? $"e-NCF NC: {nota.NCF}"
                        : $"NCF NC  : {nota.NCF}"));
            }

            if (!string.IsNullOrWhiteSpace(nota.NCFModificado))
            {
                bytes.AddRange(emitter.PrintLine(
                    $"NCF Mod.: {nota.NCFModificado}"));
            }

            bytes.AddRange(emitter.PrintLine(
                $"Factura : {nota.NumeroFactura}"));
            bytes.AddRange(emitter.PrintLine(
                $"Fecha   : {nota.Fecha:dd/MM/yyyy HH:mm}"));
            bytes.AddRange(emitter.PrintLine(
                $"Cliente : {nota.Cliente}"));

            if (!string.IsNullOrWhiteSpace(nota.RNC))
            {
                bytes.AddRange(emitter.PrintLine(
                    $"RNC     : {nota.RNC}"));
            }

            bytes.AddRange(emitter.PrintLine("--------------------------------"));
            bytes.AddRange(emitter.PrintLine("CANT   DESCRIPCION"));

            foreach (var det in nota.Detalles)
            {
                bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));
                bytes.AddRange(emitter.PrintLine(
                    $"{det.Cantidad}   {det.Descripcion}"));
                bytes.AddRange(emitter.SetStyles(PrintStyle.None));
                bytes.AddRange(emitter.PrintLine(
                    $"       RD$ {det.SubTotal:N2}"));
            }

            bytes.AddRange(emitter.PrintLine("--------------------------------"));
            bytes.AddRange(emitter.PrintLine(
                $"SubTotal RD$ {nota.SubTotal:N2}"));
            bytes.AddRange(emitter.PrintLine(
                $"ITBIS    RD$ {nota.TotalItbis:N2}"));
            bytes.AddRange(emitter.CenterAlign());
            bytes.AddRange(emitter.SetStyles(
                PrintStyle.Bold | PrintStyle.DoubleWidth));
            bytes.AddRange(emitter.PrintLine(
                $"TOTAL RD$ {nota.Total:N2}"));
            bytes.AddRange(emitter.SetStyles(PrintStyle.None));
            bytes.AddRange(emitter.PrintLine("--------------------------------"));

            var esEcfNc = !string.IsNullOrWhiteSpace(nota.NCF)
                && nota.NCF.StartsWith("E", StringComparison.OrdinalIgnoreCase);
            AppendEcfFiscalBlockEscPos(
                bytes,
                emitter,
                esEcfNc,
                "34",
                nota.SecurityCode,
                nota.UrlQR,
                nota.FechaEmisionEcf,
                nota.EstadoDgii,
                nota.RncEmisor,
                nota.RNC,
                nota.NCF,
                nota.Total,
                nota.FechaEmisionEcf ?? nota.Fecha);

            bytes.AddRange(emitter.CenterAlign());
            bytes.AddRange(emitter.PrintLine("DEVOLUCION DE MERCANCIA"));
            bytes.AddRange(emitter.FeedLines(4));
            bytes.AddRange(emitter.FullCut());

            RawPrinterHelper.SendBytesToPrinter(
                PrinterFactura,
                bytes.ToArray());
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error nota credito: {ex.Message}");
        }
    }

    public async Task GenerateTicketReciboAbono(int idPago)
    {
        try
        {
            var recibo = await _http.GetFromJsonAsync<AlahiaPos.Entities.Dto.TicketReciboAbonoDto>(
                $"{_baseUrl}/api/PagoFacturasClientes/recibo/{idPago}");

            if (recibo == null)
            {
                Console.WriteLine("Recibo de abono no encontrado.");
                return;
            }

            var emitter = new EPSON();
            var bytes = new List<byte>();

            bytes.AddRange(emitter.Initialize());
            bytes.AddRange(emitter.CenterAlign());
            bytes.AddRange(emitter.SetStyles(
                PrintStyle.Bold | PrintStyle.DoubleWidth | PrintStyle.DoubleHeight));
            bytes.AddRange(emitter.PrintLine(recibo.NombreEmpresa ?? ""));
            bytes.AddRange(emitter.SetStyles(PrintStyle.None));

            if (!string.IsNullOrWhiteSpace(recibo.DireccionEmpresa))
                bytes.AddRange(emitter.PrintLine(recibo.DireccionEmpresa));
            if (!string.IsNullOrWhiteSpace(recibo.RncEmpresa))
                bytes.AddRange(emitter.PrintLine($"RNC: {recibo.RncEmpresa}"));
            if (!string.IsNullOrWhiteSpace(recibo.TelefonoEmpresa))
                bytes.AddRange(emitter.PrintLine($"Tel: {recibo.TelefonoEmpresa}"));

            bytes.AddRange(emitter.PrintLine("--------------------------------"));
            bytes.AddRange(emitter.CenterAlign());
            bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));
            bytes.AddRange(emitter.PrintLine("RECIBO DE ABONO"));
            bytes.AddRange(emitter.SetStyles(PrintStyle.None));
            bytes.AddRange(emitter.PrintLine("--------------------------------"));

            bytes.AddRange(emitter.LeftAlign());
            bytes.AddRange(emitter.PrintLine(
                $"Factura : {recibo.NumeroDocumento}"));
            if (!string.IsNullOrWhiteSpace(recibo.NcfFactura))
            {
                bytes.AddRange(emitter.PrintLine(
                    recibo.NcfFactura.StartsWith("E", StringComparison.OrdinalIgnoreCase)
                        ? $"e-NCF   : {recibo.NcfFactura}"
                        : $"NCF     : {recibo.NcfFactura}"));
            }

            bytes.AddRange(emitter.PrintLine(
                $"Fecha   : {recibo.FechaPago:dd/MM/yyyy hh:mm tt}"));
            bytes.AddRange(emitter.PrintLine(
                $"Cliente : {recibo.Cliente}"));
            if (!string.IsNullOrWhiteSpace(recibo.RncCliente))
                bytes.AddRange(emitter.PrintLine($"RNC/Ced : {recibo.RncCliente}"));

            bytes.AddRange(emitter.PrintLine("--------------------------------"));
            bytes.AddRange(emitter.PrintLine(
                $"Forma pago: {recibo.FormaPago}"));

            if (!string.IsNullOrWhiteSpace(recibo.Nota))
            {
                foreach (var line in DividirTextoTicket(recibo.Nota, 32))
                    bytes.AddRange(emitter.PrintLine(line));
            }

            bytes.AddRange(emitter.PrintLine("--------------------------------"));
            bytes.AddRange(emitter.PrintLine(
                $"Total factura RD$ {recibo.TotalFactura:N2}"));
            bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));
            bytes.AddRange(emitter.PrintLine(
                $"ABONO        RD$ {recibo.MontoAbono:N2}"));
            bytes.AddRange(emitter.SetStyles(PrintStyle.None));
            bytes.AddRange(emitter.PrintLine(
                $"Pagado acum. RD$ {recibo.PagadoAcumulado:N2}"));
            bytes.AddRange(emitter.PrintLine(
                $"PENDIENTE    RD$ {recibo.Pendiente:N2}"));
            bytes.AddRange(emitter.PrintLine("--------------------------------"));

            bytes.AddRange(emitter.CenterAlign());
            bytes.AddRange(emitter.PrintLine("GRACIAS POR SU PAGO"));
            bytes.AddRange(emitter.FeedLines(4));
            bytes.AddRange(emitter.FullCut());

            RawPrinterHelper.SendBytesToPrinter(
                PrinterFactura,
                bytes.ToArray());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error recibo abono: {ex.Message}");
            throw;
        }
    }
    
    public async Task ImprimirCierre(int idCajaCierre)
    {
        try
        {
            var cierre = await _http.GetFromJsonAsync<CajaListadoDto>(
                $"{_baseUrl}/api/CajaCierre/ImprimirCierre/{idCajaCierre}"
            );

            if (cierre == null)
            {
                Console.WriteLine("Cierre no encontrado");
                return;
            }

            var emitter = new EPSON();
            var bytes = new List<byte>();

            bytes.AddRange(emitter.Initialize());

            bytes.AddRange(emitter.CenterAlign());
            bytes.AddRange(emitter.SetStyles(
                PrintStyle.Bold |
                PrintStyle.DoubleWidth |
                PrintStyle.DoubleHeight
            ));
            bytes.AddRange(emitter.PrintLine("CIERRE DE CAJA"));
            bytes.AddRange(emitter.SetStyles(PrintStyle.None));
            bytes.AddRange(emitter.PrintLine("--------------------------------"));

            bytes.AddRange(emitter.LeftAlign());
            var fechaCierre = cierre.FechaCierre ?? DateTime.Now;
            bytes.AddRange(emitter.PrintLine(
                $"Apertura : {cierre.FechaApertura:dd/MM/yyyy hh:mm tt}"
            ));
            bytes.AddRange(emitter.PrintLine(
                $"Cierre   : {fechaCierre:dd/MM/yyyy hh:mm tt}"
            ));
            if (!string.IsNullOrWhiteSpace(cierre.UsuarioCierre)
                || !string.IsNullOrWhiteSpace(cierre.UsuarioApertura))
            {
                bytes.AddRange(emitter.PrintLine(
                    $"Usuario  : {(cierre.UsuarioCierre ?? cierre.UsuarioApertura)}"
                ));
            }
            if (cierre.IdCajaCierre > 0)
            {
                bytes.AddRange(emitter.PrintLine($"Caja #   : {cierre.IdCajaCierre}"));
            }
            bytes.AddRange(emitter.PrintLine("--------------------------------"));

            /* =====================================
            ?? RESUMEN DE VENTAS
            ===================================== */

            bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));
            bytes.AddRange(emitter.PrintLine("RESUMEN DE VENTAS"));
            bytes.AddRange(emitter.SetStyles(PrintStyle.None));

            bytes.AddRange(emitter.PrintLine(
                $"Ventas Brutas : RD$ {cierre.VentasBrutas:N2}"
            ));

            bytes.AddRange(emitter.PrintLine(
                $"Descuentos    : RD$ {cierre.TotalDescuento:N2}"
            ));

            bytes.AddRange(emitter.PrintLine(
                $"Ingresos Caja : RD$ {cierre.TotalIngresosExtra:N2}"
            ));

            bytes.AddRange(emitter.PrintLine(
                $"Gastos Caja   : RD$ {cierre.TotalGastos:N2}"
            ));

            bytes.AddRange(emitter.PrintLine("--------------------------------"));

            bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));

            bytes.AddRange(emitter.PrintLine(
                $"TOTAL VENDIDO : RD$ {cierre.TotalIngresosNetos:N2}"
            ));

            bytes.AddRange(emitter.SetStyles(PrintStyle.None));

            bytes.AddRange(emitter.PrintLine("--------------------------------"));

            /* =====================================
            ?? FORMAS DE PAGO
            ===================================== */

            bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));
            bytes.AddRange(emitter.PrintLine("FORMAS DE PAGO"));
            bytes.AddRange(emitter.SetStyles(PrintStyle.None));

            decimal totalMetodos = 0;

            if (cierre.MetodosPago != null &&
                cierre.MetodosPago.Any())
            {
                foreach (var metodo in cierre.MetodosPago)
                {
                    bytes.AddRange(emitter.PrintLine(
                        $"{metodo.FormaPago.PadRight(18)} RD$ {metodo.Total:N2}"
                    ));

                    totalMetodos += metodo.Total;
                }

                bytes.AddRange(emitter.PrintLine("--------------------------------"));

                bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));

                bytes.AddRange(emitter.PrintLine(
                    $"TOTAL COBRADO : RD$ {totalMetodos:N2}"
                ));

                bytes.AddRange(emitter.SetStyles(PrintStyle.None));

                bytes.AddRange(emitter.PrintLine("--------------------------------"));
            }

            /* =====================================
            ?? CUADRE DE CAJA
            ===================================== */

            bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));
            bytes.AddRange(emitter.PrintLine("CUADRE DE CAJA"));
            bytes.AddRange(emitter.SetStyles(PrintStyle.None));

            bytes.AddRange(emitter.PrintLine(
                $"Fondo Inicial : RD$ {cierre.MontoInicial:N2}"
            ));

            var efectivo = cierre.MetodosPago?
                .FirstOrDefault(x =>
                    x.FormaPago.ToUpper() == "EFECTIVO")
                ?.Total ?? 0;

            bytes.AddRange(emitter.PrintLine(
                $"+ Ventas Efect.: RD$ {efectivo:N2}"
            ));

            bytes.AddRange(emitter.PrintLine(
                $"+ Ingresos Caja: RD$ {cierre.TotalIngresosExtra:N2}"
            ));

            bytes.AddRange(emitter.PrintLine(
                $"- Gastos Caja  : RD$ {cierre.TotalGastos:N2}"
            ));

            bytes.AddRange(emitter.PrintLine("--------------------------------"));

            bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));

            bytes.AddRange(emitter.PrintLine(
                $"DEBE HABER    : RD$ {cierre.DebeHaber:N2}"
            ));

            bytes.AddRange(emitter.SetStyles(PrintStyle.None));

            bytes.AddRange(emitter.PrintLine(
                $"Total Contado : RD$ {cierre.MontoRealCaja:N2}"
            ));

            bytes.AddRange(emitter.PrintLine("--------------------------------"));

            bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));

            bytes.AddRange(emitter.PrintLine(
                $"DIFERENCIA    : RD$ {cierre.Diferencia:N2}"
            ));

            bytes.AddRange(emitter.SetStyles(PrintStyle.None));

            bytes.AddRange(emitter.PrintLine("--------------------------------"));

            /* =====================================
            ?? PRODUCTOS
            ====================================== */

            if (cierre.ProductosVendidos != null &&
                cierre.ProductosVendidos.Any())
            {
                bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));
                bytes.AddRange(emitter.PrintLine("PRODUCTOS VENDIDOS"));
                bytes.AddRange(emitter.SetStyles(PrintStyle.None));

                bytes.AddRange(emitter.PrintLine("--------------------------------"));

                foreach (var item in cierre.ProductosVendidos)
                {
                    bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));
                    bytes.AddRange(emitter.PrintLine(item.Producto));
                    bytes.AddRange(emitter.SetStyles(PrintStyle.None));

                    bytes.AddRange(emitter.PrintLine(
                        $"Cant : {item.CantidadVendida:N2}"
                    ));

                    bytes.AddRange(emitter.PrintLine(
                        $"Total: RD$ {item.TotalVendido:N2}"
                    ));

                    bytes.AddRange(emitter.PrintLine(
                        $"Exist: {item.ExistenciaActual:N2}"
                    ));

                    bytes.AddRange(emitter.PrintLine("--------------------------------"));
                }
            }

            /* =====================================
            ?? OBSERVACI?N
            ====================================== */

            if (!string.IsNullOrWhiteSpace(cierre.Observacion))
            {
                bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));
                bytes.AddRange(emitter.PrintLine("OBSERVACION"));
                bytes.AddRange(emitter.SetStyles(PrintStyle.None));

                bytes.AddRange(emitter.PrintLine(cierre.Observacion));
                bytes.AddRange(emitter.PrintLine("--------------------------------"));
            }

            /* =====================================
            ?? FOOTER
            ====================================== */

            bytes.AddRange(emitter.CenterAlign());

            bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));

            bytes.AddRange(emitter.PrintLine(
                "CIERRE REALIZADO CORRECTAMENTE"
            ));

            bytes.AddRange(emitter.SetStyles(PrintStyle.None));

            bytes.AddRange(emitter.FeedLines(4));
            bytes.AddRange(emitter.FullCut());

            RawPrinterHelper.SendBytesToPrinter(
            PrinterFactura,
            bytes.ToArray()
            );

            //var ruta = @"C:\Temp\TicketPrueba.bin";

            // Crear carpeta si no existe
            //Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);

            // Guardar archivo
            //File.WriteAllBytes(
               // ruta,
                //bytes.ToArray()
            //);

            // Abrir autom?ticamente
            //System.Diagnostics.Process.Start(new ProcessStartInfo
            //{
               // FileName = ruta,
               // UseShellExecute = true
            //});
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error imprimiendo cierre: {ex.Message}"
            );
        }
    }
    public async Task ImprimirCierreEncargos(
    int idEmpresa
)
    {
        try
        {
            /* =====================================
            ?? CONSUMIR API
            ====================================== */

            var cierre =

                await _http
                .GetFromJsonAsync<CierreEncargoDiaDto>(

                    $"{_baseUrl}/api/BizcochoEncargo/" +

                    $"GetCierreDelDia" +

                    $"?idEmpresa={idEmpresa}"
                );

            if (cierre == null)
            {
                Console.WriteLine(
                    "No hay cierre disponible."
                );

                return;
            }

            /* =====================================
            ?? EMITTER
            ====================================== */

            var emitter = new EPSON();

            var bytes = new List<byte>();

            bytes.AddRange(
                emitter.Initialize()
            );

            /* =====================================
            ?? HEADER
            ====================================== */

            bytes.AddRange(
                emitter.CenterAlign()
            );

            bytes.AddRange(
                emitter.SetStyles(

                    PrintStyle.Bold |

                    PrintStyle.DoubleWidth |

                    PrintStyle.DoubleHeight
                )
            );

            bytes.AddRange(
                emitter.PrintLine(
                    "CIERRE ENCARGOS"
                )
            );

            bytes.AddRange(
                emitter.SetStyles(
                    PrintStyle.None
                )
            );

            bytes.AddRange(
                emitter.PrintLine(
                    $"{cierre.Fecha:dd/MM/yyyy hh:mm tt}"
                )
            );

            bytes.AddRange(
                emitter.PrintLine(
                    "--------------------------------"
                )
            );

            /* =====================================
            ?? RESUMEN
            ====================================== */

            bytes.AddRange(
                emitter.LeftAlign()
            );

            bytes.AddRange(
                emitter.PrintLine(

                    $"Nuevos : " +

                    $"{cierre.CantidadEncargosNuevos}"
                )
            );

            bytes.AddRange(
                emitter.PrintLine(

                    $"Registrado: RD$ " +

                    $"{cierre.TotalEncargosNuevos:N2}"
                )
            );

            bytes.AddRange(
                emitter.PrintLine(

                    $"Cobrado : RD$ " +

                    $"{cierre.TotalCobradoHoy:N2}"
                )
            );

            bytes.AddRange(
                emitter.PrintLine(

                    $"Pendiente: RD$ " +

                    $"{cierre.TotalPendiente:N2}"
                )
            );

            bytes.AddRange(
                emitter.PrintLine(
                    "--------------------------------"
                )
            );

            /* =====================================
            ?? METODOS DE PAGO
            ====================================== */

            if (

                cierre.MetodosPago != null

                &&

                cierre.MetodosPago.Any()
            )
            {
                bytes.AddRange(
                    emitter.SetStyles(
                        PrintStyle.Bold
                    )
                );

                bytes.AddRange(
                    emitter.PrintLine(
                        "COBROS DEL DIA"
                    )
                );

                bytes.AddRange(
                    emitter.SetStyles(
                        PrintStyle.None
                    )
                );

                foreach (var item in cierre.MetodosPago)
                {
                    bytes.AddRange(
                        emitter.PrintLine(

                            $"{item.FormaPago}: " +

                            $"RD$ {item.Total:N2}"
                        )
                    );
                }

                bytes.AddRange(
                    emitter.PrintLine(
                        "--------------------------------"
                    )
                );
            }

            /* =====================================
            ?? DETALLE ENCARGOS
            ====================================== */

            if (

                cierre.Encargos != null

                &&

                cierre.Encargos.Any()
            )
            {
                bytes.AddRange(
                    emitter.SetStyles(
                        PrintStyle.Bold
                    )
                );

                bytes.AddRange(
                    emitter.PrintLine(
                        "ENCARGOS DEL DIA"
                    )
                );

                bytes.AddRange(
                    emitter.SetStyles(
                        PrintStyle.None
                    )
                );

                bytes.AddRange(
                    emitter.PrintLine(
                        "--------------------------------"
                    )
                );

                foreach (var item in cierre.Encargos)
                {
                    bytes.AddRange(
                        emitter.SetStyles(
                            PrintStyle.Bold
                        )
                    );

                    bytes.AddRange(
                        emitter.PrintLine(
                            item.Cliente
                        )
                    );

                    bytes.AddRange(
                        emitter.SetStyles(
                            PrintStyle.None
                        )
                    );

                    bytes.AddRange(
                        emitter.PrintLine(

                            $"Doc: " +

                            $"{item.NumeroDocumento}"
                        )
                    );

                    bytes.AddRange(
                        emitter.PrintLine(

                            $"Total: RD$ " +

                            $"{item.Total:N2}"
                        )
                    );

                    bytes.AddRange(
                        emitter.PrintLine(

                            $"Abono: RD$ " +

                            $"{item.Abonado:N2}"
                        )
                    );

                    bytes.AddRange(
                        emitter.PrintLine(

                            $"Pend.: RD$ " +

                            $"{item.Pendiente:N2}"
                        )
                    );

                    bytes.AddRange(
                        emitter.PrintLine(

                            $"Estado: " +

                            $"{item.Estado}"
                        )
                    );

                    bytes.AddRange(
                        emitter.PrintLine(
                            "--------------------------------"
                        )
                    );
                }
            }

            /* =====================================
            ?? FOOTER
            ====================================== */

            bytes.AddRange(
                emitter.CenterAlign()
            );

            bytes.AddRange(
                emitter.PrintLine(
                    "CIERRE GENERADO"
                )
            );

            bytes.AddRange(
                emitter.PrintLine(
                    "CORRECTAMENTE"
                )
            );

            bytes.AddRange(
                emitter.FeedLines(4)
            );

            bytes.AddRange(
                emitter.FullCut()
            );

            /* =====================================
            ?? IMPRIMIR
            ====================================== */

            RawPrinterHelper
            .SendBytesToPrinter(

            PrinterFactura,

            bytes.ToArray()
            );

            /* =====================================
?? GENERAR ARCHIVO DE PRUEBA
===================================== */
            //var ruta = @"C:\Temp\TicketPrueba.bin";

            // Crear carpeta si no existe
            //Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);

            // Guardar archivo
            //File.WriteAllBytes(
               // ruta,
               // bytes.ToArray()
            //);

            // Abrir autom?ticamente
            //System.Diagnostics.Process.Start(new ProcessStartInfo
            //{
                //FileName = ruta,
               // UseShellExecute = true
            //});

        }
        catch (Exception ex)
        {
            Console.WriteLine(

                $"Error cierre encargos: " +

                ex.Message
            );
        }


    }

}