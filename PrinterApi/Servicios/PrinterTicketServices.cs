using ESCPOS_NET;
using ESCPOS_NET.Emitters;
using ESCPOS_NET.Utilities;
using PrinterApi.Dto;
using PrinterApi.Dto.PrinterApi.Dto;
using PrinterApi.Interfaz;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text;



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

                // ✅ USA CONFIG
                RawPrinterHelper.SendBytesToPrinter(_printerLavador, bytes.ToArray());
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

            // 🔥 AQUÍ eliges la impresora
            //pd.PrinterSettings.PrinterName = _printerFactura;
            pd.PrinterSettings.PrinterName = "Microsoft Print to PDF";

            pd.PrintController = new StandardPrintController(); // 🔥 SIN DIÁLOGO

            pd.PrintPage += (sender, e) =>
            {
                float y = 20;
                float left = 20;

                Font normal = new Font("Arial", 10);
                Font bold = new Font("Arial", 12, FontStyle.Bold);

                // 🔷 EMPRESA
                e.Graphics.DrawString(factura.NombreEmpresa, bold, Brushes.Black, left, y);
                y += 25;

                e.Graphics.DrawString(factura.DireccionEmpresa, normal, Brushes.Black, left, y);
                y += 20;

                e.Graphics.DrawString($"Tel: {factura.TelefonoEmpresa}", normal, Brushes.Black, left, y);
                y += 25;

                e.Graphics.DrawString("--------------------------------------------", normal, Brushes.Black, left, y);
                y += 20;

                // 🔷 INFO
                e.Graphics.DrawString($"Factura: {factura.NumeroFactura}", normal, Brushes.Black, left, y);
                y += 20;

                e.Graphics.DrawString($"Fecha: {factura.Fecha:dd/MM/yyyy} {factura.Hora}", normal, Brushes.Black, left, y);
                y += 20;

                e.Graphics.DrawString($"Cliente: {factura.Cliente}", normal, Brushes.Black, left, y);
                y += 25;

                e.Graphics.DrawString("--------------------------------------------", normal, Brushes.Black, left, y);
                y += 20;

                // 🔷 DETALLE
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

                // 🔷 TOTAL
                e.Graphics.DrawString($"TOTAL: RD$ {factura.Total:N2}", bold, Brushes.Black, left, y);
                y += 30;

                e.Graphics.DrawString("GRACIAS POR SU COMPRA", normal, Brushes.Black, left, y);
            };

            // 🔥 AQUÍ SE MANDA DIRECTO A LA IMPRESORA
            pd.Print();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error impresión directa: {ex.Message}");
        }
    }
    // ============================
    // 🔹 FACTURA CLIENTE
    // ============================

  

public async Task GenerateTicketBizcocho(int idFacturaHeader, int idEmpresa)
{
    try
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var enc = Encoding.GetEncoding(850); // Español / acentos / ñ

        var factura = await _http.GetFromJsonAsync<FacturaHeaderDto>(
            $"{_baseUrl}/api/BizcochoEncargo/print" +
            $"?IdFacturaHeader={idFacturaHeader}" +
            $"&IdEmpresa={idEmpresa}"
        );

        if (factura == null ||
            factura.FacturaDetalles == null ||
            !factura.FacturaDetalles.Any())
        {
            Console.WriteLine("Encargo no encontrado.");
            return;
        }

            bool esEncargo = factura.IdTipoDocumentos == 14;
            bool esOrden = factura.IdTipoDocumentos == 10;
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

        // 🔥 Tabla de caracteres español CP850
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
                CenterLine("FACTURA");
            else if (esOrden)
                CenterLine("ORDEN");
            else
                CenterLine("ENCARGO BIZCOCHO");
            
            Separator();

        // ============================
        // DATOS DOCUMENTO
        // ============================

        bytes.AddRange(emitter.LeftAlign());

            var hora = DateTime.Now.Hour < 12 ? "AM" : "PM";
            


            if (esOrden)
            {
                LeftLine($"#Orden     :0000 {factura.IdFacturaHeader}");
                
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
                LeftLine($"NCF        : {factura.NCF}");

            string clienteFactura =
                !string.IsNullOrWhiteSpace(factura.NombreEmpresa)
                    ? factura.NombreEmpresa
                    : !string.IsNullOrWhiteSpace(factura.cliente)
                        ? factura.cliente
                        : "Al Portador";

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
            WrappedLeft(det.Productos?.nombre ?? "Producto", 32);
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

            bytes.AddRange(emitter.SetStyles(PrintStyle.None));

        Separator();

        bytes.AddRange(emitter.CenterAlign());
        Line("GRACIAS POR SU PREFERENCIA");

        bytes.AddRange(emitter.FeedLines(4));
        bytes.AddRange(emitter.CashDrawerOpenPin2());
        bytes.AddRange(emitter.FullCut());

            RawPrinterHelper.SendBytesToPrinter(
            _printerFactura,
                bytes.ToArray()
            );

            //try
            //{
                //var ticketTexto = enc.GetString(bytes.ToArray());

                //var ruta = Path.Combine(
                    //AppDomain.CurrentDomain.BaseDirectory,
                    //"TicketBizcocho.txt"
                //);

                //File.WriteAllText(ruta, ticketTexto, enc);

                // 🔥 Abrir automáticamente
                //Process.Start(new ProcessStartInfo
                //{
                   // FileName = ruta,
                   // UseShellExecute = true
                //});
            //}
           // catch (Exception ex)
            //{
                //Console.WriteLine($"Error generando TXT: {ex.Message}");
            //}

            //RawPrinterHelper.SendBytesToPrinter(
               // _printerFactura,
                //bytes.ToArray()
            //);
        }
    catch (Exception ex)
    {
        Console.WriteLine($"Error ticket bizcocho: {ex.Message}");
    }
}

private static string LimpiarTextoTicket(string texto)
{
    if (string.IsNullOrWhiteSpace(texto))
        return "";

    return texto
        .Replace("≤", "ó")
        .Replace("❤", "")
        .Replace("❤️", "")
        .Replace("–", "-")
        .Replace("—", "-")
        .Replace("“", "\"")
        .Replace("”", "\"")
        .Replace("’", "'")
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
            // Abrir cajón (pin 2)
            bytes.AddRange(emitter.CashDrawerOpenPin2());
            bytes.AddRange(emitter.FullCut());

            // ✅ USA CONFIG
            RawPrinterHelper.SendBytesToPrinter(_printerFactura, bytes.ToArray());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error factura: {ex.Message}");
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

            /* =====================================
            🔥 HEADER
            ====================================== */

            bytes.AddRange(emitter.CenterAlign());
            bytes.AddRange(emitter.SetStyles(
                PrintStyle.Bold |
                PrintStyle.DoubleWidth |
                PrintStyle.DoubleHeight
            ));
            bytes.AddRange(emitter.PrintLine("--------------------------------"));

            /* =====================================
            🔥 RESUMEN DE VENTAS
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
            🔥 FORMAS DE PAGO
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
            🔥 CUADRE DE CAJA
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
            🔥 PRODUCTOS
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
            🔥 OBSERVACIÓN
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
            🔥 FOOTER
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
            _printerFactura,
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

            // Abrir automáticamente
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
            🔥 CONSUMIR API
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
            🔥 EMITTER
            ====================================== */

            var emitter = new EPSON();

            var bytes = new List<byte>();

            bytes.AddRange(
                emitter.Initialize()
            );

            /* =====================================
            🔥 HEADER
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
                    $"{cierre.Fecha:dd/MM/yyyy}"
                )
            );

            bytes.AddRange(
                emitter.PrintLine(
                    "--------------------------------"
                )
            );

            /* =====================================
            🔥 RESUMEN
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
            🔥 METODOS DE PAGO
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
            🔥 DETALLE ENCARGOS
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
            🔥 FOOTER
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
            🔥 IMPRIMIR
            ====================================== */

            RawPrinterHelper
            .SendBytesToPrinter(

            _printerFactura,

            bytes.ToArray()
            );

            /* =====================================
🔥 GENERAR ARCHIVO DE PRUEBA
===================================== */
            //var ruta = @"C:\Temp\TicketPrueba.bin";

            // Crear carpeta si no existe
            //Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);

            // Guardar archivo
            //File.WriteAllBytes(
               // ruta,
               // bytes.ToArray()
            //);

            // Abrir automáticamente
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