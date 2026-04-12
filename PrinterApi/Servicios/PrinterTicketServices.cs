using ESCPOS_NET;
using ESCPOS_NET.Emitters;
using ESCPOS_NET.Utilities;
using PrinterApi.Dto;
using PrinterApi.Interfaz;
using System.Net.Http.Json;

public class PrinterTicketServices : IPrinterTicket
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string _printerFactura;
    private readonly string _printerLavador;

    public PrinterTicketServices(HttpClient http, IConfiguration config)
    {
        _http = http;
        _baseUrl = config["ApiBaseUrl"];
        _printerFactura = config["PrinterSettings:Factura"];
        _printerLavador = config["PrinterSettings:Lavador"];
    }

    // ============================
    // 🔹 TICKET LAVADOR
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
                bytes.AddRange(emitter.PrintLine("THE JH CAR WASH"));

                bytes.AddRange(emitter.SetStyles(PrintStyle.None));
                bytes.AddRange(emitter.PrintLine("TICKET LAVADOR"));
                bytes.AddRange(emitter.PrintLine("--------------------------------"));

                bytes.AddRange(emitter.LeftAlign());
                bytes.AddRange(emitter.PrintLine($"Factura : {ticket.NumeroFactura}"));
                bytes.AddRange(emitter.PrintLine($"Fecha   : {ticket.Fecha:dd/MM/yyyy hh:mm tt}"));
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

                // ✅ USA CONFIG
                RawPrinterHelper.SendBytesToPrinter(_printerLavador, bytes.ToArray());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error lavador: {ex.Message}");
        }
    }

    // ============================
    // 🔹 FACTURA CLIENTE
    // ============================
    public async Task GenerateTicketFacturaCliente(int idFactura)
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

            var emitter = new EPSON();
            var bytes = new List<byte>();

            bytes.AddRange(emitter.Initialize());

            bytes.AddRange(emitter.CenterAlign());
            bytes.AddRange(emitter.SetStyles(PrintStyle.Bold | PrintStyle.DoubleWidth | PrintStyle.DoubleHeight));
            bytes.AddRange(emitter.PrintLine(factura.NombreEmpresa));

            bytes.AddRange(emitter.SetStyles(PrintStyle.None));
            bytes.AddRange(emitter.PrintLine(factura.DireccionEmpresa));
            bytes.AddRange(emitter.PrintLine($"Tel: {factura.TelefonoEmpresa}"));

            bytes.AddRange(emitter.PrintLine("--------------------------------"));

            bytes.AddRange(emitter.CenterAlign());
            bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));
            bytes.AddRange(emitter.PrintLine("FACTURA CLIENTE"));

            bytes.AddRange(emitter.PrintLine("--------------------------------"));

            bytes.AddRange(emitter.LeftAlign());
            bytes.AddRange(emitter.PrintLine($"Factura : {factura.NumeroFactura}"));
            bytes.AddRange(emitter.PrintLine($"Fecha   : {factura.Fecha:dd/MM/yyyy} {factura.Hora}"));
            bytes.AddRange(emitter.PrintLine($"Cliente : {factura.Cliente}"));

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

            bytes.AddRange(emitter.CenterAlign());
            bytes.AddRange(emitter.SetStyles(PrintStyle.Bold | PrintStyle.DoubleWidth | PrintStyle.DoubleHeight));
            bytes.AddRange(emitter.PrintLine($"TOTAL RD$ {factura.Total:N2}"));

            bytes.AddRange(emitter.SetStyles(PrintStyle.None));
            bytes.AddRange(emitter.PrintLine("--------------------------------"));

            bytes.AddRange(emitter.CenterAlign());
            bytes.AddRange(emitter.PrintLine("GRACIAS POR PREFERIRNOS"));

            bytes.AddRange(emitter.FeedLines(4));
            bytes.AddRange(emitter.FullCut());

            // ✅ USA CONFIG
            RawPrinterHelper.SendBytesToPrinter(_printerFactura, bytes.ToArray());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error factura: {ex.Message}");
        }
    }
}