using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Win32;

namespace PrinterApi.Servicios;

public interface IPrinterLocalSettings
{
    string Factura { get; }
    string Lavador { get; }
    IReadOnlyList<string> ListInstalledPrinters();
    PrinterSettingsSnapshot GetSnapshot();
    PrinterSettingsSnapshot Save(string factura, string? lavador = null);
}

public sealed class PrinterSettingsSnapshot
{
    public string Factura { get; set; } = "";
    public string Lavador { get; set; } = "";
    public string LocalConfigPath { get; set; } = "";
    public IReadOnlyList<string> InstalledPrinters { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Lee/escribe PrinterSettings en %ProgramData%\Alahia\PrinterApi\appsettings.Local.json
/// y expone los nombres actuales para impresión (sin reiniciar el servicio).
/// </summary>
public sealed class PrinterLocalSettings : IPrinterLocalSettings
{
    private readonly object _gate = new();
    private readonly IConfiguration _config;
    private readonly ILogger<PrinterLocalSettings> _logger;
    private readonly string _localPath;
    private string _factura;
    private string _lavador;

    public PrinterLocalSettings(IConfiguration config, ILogger<PrinterLocalSettings> logger)
    {
        _config = config;
        _logger = logger;
        _localPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Alahia",
            "PrinterApi",
            "appsettings.Local.json");

        _factura = (config["PrinterSettings:Factura"] ?? "").Trim();
        _lavador = (config["PrinterSettings:Lavador"] ?? _factura).Trim();
    }

    public string Factura
    {
        get { lock (_gate) return _factura; }
    }

    public string Lavador
    {
        get { lock (_gate) return string.IsNullOrWhiteSpace(_lavador) ? _factura : _lavador; }
    }

    public IReadOnlyList<string> ListInstalledPrinters()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (string p in PrinterSettings.InstalledPrinters)
            {
                if (!string.IsNullOrWhiteSpace(p))
                    names.Add(p.Trim());
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "InstalledPrinters no disponible en este contexto.");
        }

        try
        {
            foreach (var p in EnumPrintersWin32())
                names.Add(p);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "EnumPrinters Win32 falló.");
        }

        try
        {
            foreach (var p in ReadPrintersFromRegistry())
                names.Add(p);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "No se pudieron leer impresoras del registro.");
        }

        if (names.Count == 0)
        {
            _logger.LogWarning(
                "No se detectaron impresoras (servicio LocalSystem a veces no ve impresoras por usuario). " +
                "El cliente puede escribir el nombre exacto desde Dispositivos e impresoras.");
        }

        return names.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public PrinterSettingsSnapshot GetSnapshot()
    {
        lock (_gate)
        {
            return new PrinterSettingsSnapshot
            {
                Factura = _factura,
                Lavador = string.IsNullOrWhiteSpace(_lavador) ? _factura : _lavador,
                LocalConfigPath = _localPath,
                InstalledPrinters = ListInstalledPrinters()
            };
        }
    }

    public PrinterSettingsSnapshot Save(string factura, string? lavador = null)
    {
        if (string.IsNullOrWhiteSpace(factura))
            throw new ArgumentException("Debe indicar el nombre de la impresora de facturas.");

        var facturaClean = factura.Trim();
        var lavadorClean = string.IsNullOrWhiteSpace(lavador) ? facturaClean : lavador.Trim();

        lock (_gate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_localPath)!);

            JsonObject root;
            if (File.Exists(_localPath))
            {
                var text = File.ReadAllText(_localPath);
                root = string.IsNullOrWhiteSpace(text)
                    ? new JsonObject()
                    : (JsonNode.Parse(text) as JsonObject) ?? new JsonObject();
            }
            else
            {
                root = new JsonObject();
            }

            var settings = root["PrinterSettings"] as JsonObject ?? new JsonObject();
            settings["Factura"] = facturaClean;
            settings["Lavador"] = lavadorClean;
            root["PrinterSettings"] = settings;

            if (root["ApiBaseUrl"] == null)
            {
                var api = (_config["ApiBaseUrl"] ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(api))
                    root["ApiBaseUrl"] = api;
            }

            var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_localPath, json);

            _factura = facturaClean;
            _lavador = lavadorClean;

            _logger.LogInformation(
                "Impresora actualizada: Factura={Factura}, Lavador={Lavador}",
                _factura,
                _lavador);

            return new PrinterSettingsSnapshot
            {
                Factura = _factura,
                Lavador = _lavador,
                LocalConfigPath = _localPath,
                InstalledPrinters = ListInstalledPrinters()
            };
        }
    }

    private static IEnumerable<string> ReadPrintersFromRegistry()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Print\Printers");
        if (key == null) yield break;
        foreach (var name in key.GetSubKeyNames())
        {
            if (!string.IsNullOrWhiteSpace(name))
                yield return name.Trim();
        }
    }

    private static IEnumerable<string> EnumPrintersWin32()
    {
        const int PRINTER_ENUM_LOCAL = 0x00000002;
        const int PRINTER_ENUM_CONNECTIONS = 0x00000004;
        const int level = 2;
        var flags = PRINTER_ENUM_LOCAL | PRINTER_ENUM_CONNECTIONS;

        uint needed = 0;
        uint returned = 0;
        EnumPrinters(flags, null, level, IntPtr.Zero, 0, ref needed, ref returned);
        if (needed == 0) yield break;

        var buffer = Marshal.AllocHGlobal((int)needed);
        try
        {
            if (!EnumPrinters(flags, null, level, buffer, needed, ref needed, ref returned) || returned == 0)
                yield break;

            var stride = Marshal.SizeOf<PRINTER_INFO_2>();
            for (var i = 0; i < returned; i++)
            {
                var info = Marshal.PtrToStructure<PRINTER_INFO_2>(buffer + (i * stride));
                if (!string.IsNullOrWhiteSpace(info.pPrinterName))
                    yield return info.pPrinterName.Trim();
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool EnumPrinters(
        int flags,
        string? name,
        int level,
        IntPtr pPrinterEnum,
        uint cbBuf,
        ref uint pcbNeeded,
        ref uint pcReturned);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct PRINTER_INFO_2
    {
        public string? pServerName;
        public string? pPrinterName;
        public string? pShareName;
        public string? pPortName;
        public string? pDriverName;
        public string? pComment;
        public string? pLocation;
        public IntPtr pDevMode;
        public string? pSepFile;
        public string? pPrintProcessor;
        public string? pDatatype;
        public string? pParameters;
        public IntPtr pSecurityDescriptor;
        public uint Attributes;
        public uint Priority;
        public uint DefaultPriority;
        public uint StartTime;
        public uint UntilTime;
        public uint Status;
        public uint cJobs;
        public uint AveragePPM;
    }
}
