using AlahiaPos.DataAccess.Servicios;
using AlahiaPos.Entities.Interfaces;
using ESCPOS_NET;
using ESCPOS_NET.Emitters;
using ESCPOS_NET.Utilities;
using AlahiaPos.Entities.Dto;
using System;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios;

public class PrinterTicketServices: IPrinterTicket
{
    private readonly IFacturaHeader _facturaHeaderServices;
    private readonly IClientes _Clientes;
    private readonly IFacturaDetalle _IFacturaDetalle;
    public PrinterTicketServices(IFacturaHeader facturaHeaderServices, IClientes clientes,
        IFacturaDetalle facturaDetalle)
    {
        _facturaHeaderServices = facturaHeaderServices;
        _Clientes = clientes;
        _IFacturaDetalle = facturaDetalle;
    }

    public async Task GenerateTicketLavador(int IdFacturaHeader)
    {
        try
        {
            var listado = await _facturaHeaderServices.GetTicketsLavadorByFactura(IdFacturaHeader);

            if (listado == null || listado.Count == 0)
            {
                Console.WriteLine("No hay tickets pendientes.");
                return;
            }

            var emitter = new EPSON();

            foreach (var ticket in listado)
            {
                try
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
                        bytes.AddRange(emitter.PrintLine($"{det.Cantidad}   {det.Servicio+" "+(det.Precio*det.Cantidad)}"));
                        bytes.AddRange(emitter.SetStyles(PrintStyle.None));
                    }

                    bytes.AddRange(emitter.PrintLine("--------------------------------"));

                    bytes.AddRange(emitter.CenterAlign());
                    bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));
                    bytes.AddRange(emitter.PrintLine("GRACIAS POR SU TRABAJO"));

                    bytes.AddRange(emitter.FeedLines(4));
                    bytes.AddRange(emitter.FullCut());

                    RawPrinterHelper.SendBytesToPrinter("2C-POS80-01-V6 Printer", bytes.ToArray());
                    //var ruta = @"C:\Tickets\factura_test.bin";

                    //Directory.CreateDirectory(@"C:\Tickets");

                    //File.WriteAllBytes(ruta, bytes.ToArray());

                    //Console.WriteLine($"Ticket guardado en archivo: {ruta}");
                    // ⭐ marcar detalles como impresos


                    Console.WriteLine($"Ticket lavador {ticket.AtendidoPor} impreso.");
                }
                catch (Exception exTicket)
                {
                    Console.WriteLine($"Error ticket lavador: {exTicket.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error general impresión: {ex.Message}");
        }
    }
    public async Task GenerateTicketFacturaCliente(int idFactura)
    {
        try
        {
            // ⭐ DTO COMPLETO YA ARMADO
            var factura = await _facturaHeaderServices.GetFacturaClienteById(idFactura);

            if (factura == null)
            {
                Console.WriteLine("Factura no encontrada.");
                return;
            }

            if (factura.Detalles == null || !factura.Detalles.Any())
            {
                Console.WriteLine("Factura sin detalles.");
                return;
            }

            var emitter = new EPSON();
            var bytes = new List<byte>();

            bytes.AddRange(emitter.Initialize());

            // ⭐ EMPRESA
            bytes.AddRange(emitter.CenterAlign());
            bytes.AddRange(emitter.SetStyles(PrintStyle.Bold | PrintStyle.DoubleWidth | PrintStyle.DoubleHeight));
            bytes.AddRange(emitter.PrintLine(factura.NombreEmpresa));

            bytes.AddRange(emitter.SetStyles(PrintStyle.None));
            bytes.AddRange(emitter.PrintLine(factura.DireccionEmpresa));
            bytes.AddRange(emitter.PrintLine($"Tel: {factura.TelefonoEmpresa}"));

            bytes.AddRange(emitter.PrintLine("--------------------------------"));

            // ⭐ TITULO
            bytes.AddRange(emitter.CenterAlign());
            bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));
            bytes.AddRange(emitter.PrintLine("FACTURA CLIENTE"));

            bytes.AddRange(emitter.PrintLine("--------------------------------"));

            // ⭐ INFO FACTURA
            bytes.AddRange(emitter.LeftAlign());
            bytes.AddRange(emitter.PrintLine($"Factura : {factura.NumeroFactura}"));
            bytes.AddRange(emitter.PrintLine($"Fecha   : {factura.Fecha:dd/MM/yyyy} {factura.Hora}"));
            bytes.AddRange(emitter.PrintLine($"Cliente : {factura.Cliente}"));

            bytes.AddRange(emitter.PrintLine("--------------------------------"));

            // ⭐ DETALLES
            bytes.AddRange(emitter.PrintLine("CANT   DESCRIPCION"));

            foreach (var det in factura.Detalles)
            {
                bytes.AddRange(emitter.SetStyles(PrintStyle.Bold));
                bytes.AddRange(emitter.PrintLine($"{det.Cantidad}   {det.Descripcion}"));

                bytes.AddRange(emitter.SetStyles(PrintStyle.None));
                bytes.AddRange(emitter.PrintLine($"       RD$ {det.Precio:N2}"));
            }

            bytes.AddRange(emitter.PrintLine("--------------------------------"));

            // ⭐ TOTAL
            bytes.AddRange(emitter.CenterAlign());
            bytes.AddRange(emitter.SetStyles(PrintStyle.Bold | PrintStyle.DoubleWidth | PrintStyle.DoubleHeight));
            bytes.AddRange(emitter.PrintLine($"TOTAL RD$ {factura.Total:N2}"));

            bytes.AddRange(emitter.SetStyles(PrintStyle.None));
            bytes.AddRange(emitter.PrintLine("--------------------------------"));

            bytes.AddRange(emitter.CenterAlign());
            bytes.AddRange(emitter.PrintLine("GRACIAS POR PREFERIRNOS"));

            bytes.AddRange(emitter.FeedLines(4));
            bytes.AddRange(emitter.FullCut());

            RawPrinterHelper.SendBytesToPrinter("2C-POS80-01-V6 Printer", bytes.ToArray());
            //var ruta = @"C:\Tickets\factura_test.bin";

            //Directory.CreateDirectory(@"C:\Tickets");

            //File.WriteAllBytes(ruta, bytes.ToArray());

            //Console.WriteLine($"Ticket guardado en archivo: {ruta}");
            //Console.WriteLine($"Factura cliente {factura.NumeroFactura} impresa.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error impresión cliente: {ex.Message}");
        }
    }
}