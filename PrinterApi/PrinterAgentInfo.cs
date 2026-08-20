using System.Reflection;

namespace PrinterApi;

public static class PrinterAgentInfo
{
    public const string ServiceName = "AlahiaPrinterApi";
    public const int DefaultPort = 5045;

    public static string Version
    {
        get
        {
            var asm = Assembly.GetExecutingAssembly();
            var info = asm
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(info))
            {
                // Puede venir "1.0.0+hash"
                var plus = info.IndexOf('+');
                return plus > 0 ? info[..plus] : info.Trim();
            }

            return asm.GetName().Version?.ToString(3) ?? "0.0.0";
        }
    }

    public static object BuildStatusPayload(string? message = null) => new
    {
        success = true,
        ok = true,
        message = message ?? "Printer API funcionando",
        version = Version,
        port = DefaultPort,
        serviceName = ServiceName,
        machineName = Environment.MachineName,
        utc = DateTime.UtcNow
    };
}
