using System;
using System.Runtime.InteropServices;

namespace AlahiaPos.Entities.Dto;

public class RawPrinterHelper
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class DOCINFOA
    {
        public string pDocName;
        public string pOutputFile;
        public string pDataType;
    }

    [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true)]
    private static extern bool OpenPrinter(string szPrinter, out IntPtr hPrinter, IntPtr pd);

    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFOA di);

    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, byte[] bytes, int count, out int written);

    /// <summary>
    /// Envia bytes RAW directo a la impresora por nombre
    /// </summary>
    public static void SendBytesToPrinter(string printerName, byte[] bytes)
    {
        IntPtr hPrinter;

        var docInfo = new DOCINFOA()
        {
            pDocName = "Ticket POS",
            pDataType = "RAW"
        };

        if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
            throw new Exception("No se pudo abrir la impresora");

        if (!StartDocPrinter(hPrinter, 1, docInfo))
            throw new Exception("No se pudo iniciar documento de impresión");

        StartPagePrinter(hPrinter);

        WritePrinter(hPrinter, bytes, bytes.Length, out _);

        EndPagePrinter(hPrinter);
        EndDocPrinter(hPrinter);
        ClosePrinter(hPrinter);
    }
}