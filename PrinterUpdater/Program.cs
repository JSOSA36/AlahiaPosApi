using System.Diagnostics;
using System.ServiceProcess;

namespace PrinterUpdater;

/// <summary>
/// Detiene el servicio AlahiaPrinterApi, copia binarios desde staging y lo reinicia.
/// No sobrescribe appsettings.Local.json ni archivos .Local.*.
/// </summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            var opts = ParseArgs(args);
            if (string.IsNullOrWhiteSpace(opts.Source) || string.IsNullOrWhiteSpace(opts.Target))
            {
                Console.Error.WriteLine(
                    "Uso: PrinterUpdater --service AlahiaPrinterApi --source <stagingDir> --target <installDir>");
                return 2;
            }

            if (!Directory.Exists(opts.Source))
            {
                Console.Error.WriteLine($"Source no existe: {opts.Source}");
                return 3;
            }

            Directory.CreateDirectory(opts.Target);

            Console.WriteLine($"Deteniendo servicio {opts.Service}…");
            StopService(opts.Service, TimeSpan.FromSeconds(45));

            // Esperar a que el proceso libere DLLs
            Thread.Sleep(2000);

            Console.WriteLine($"Copiando de {opts.Source} → {opts.Target}");
            CopyUpdateFiles(opts.Source, opts.Target);

            Console.WriteLine($"Iniciando servicio {opts.Service}…");
            ApplyServiceResilience(opts.Service);
            StartService(opts.Service, TimeSpan.FromSeconds(45));

            Console.WriteLine("Actualización completada.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error actualizando: {ex.Message}");
            try
            {
                // Intentar dejar el servicio arriba aunque falle la copia parcial
                var service = GetArg(args, "--service") ?? "AlahiaPrinterApi";
                ApplyServiceResilience(service);
                StartService(service, TimeSpan.FromSeconds(30));
            }
            catch { /* ignore */ }

            return 1;
        }
    }

    private static void CopyUpdateFiles(string source, string target)
    {
        // Si el zip tiene una sola carpeta raíz, usarla como origen real
        var sourceRoot = ResolveContentRoot(source);

        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceRoot, file);
            var name = Path.GetFileName(file);

            if (ShouldSkip(name, relative))
                continue;

            var dest = Path.Combine(target, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

            // Reintentos por archivos bloqueados
            for (var attempt = 1; attempt <= 5; attempt++)
            {
                try
                {
                    File.Copy(file, dest, overwrite: true);
                    break;
                }
                catch (IOException) when (attempt < 5)
                {
                    Thread.Sleep(500 * attempt);
                }
            }
        }
    }

    private static string ResolveContentRoot(string source)
    {
        var entries = Directory.GetFileSystemEntries(source);
        if (entries.Length == 1 && Directory.Exists(entries[0]))
            return entries[0];
        return source;
    }

    private static bool ShouldSkip(string fileName, string relativePath)
    {
        if (fileName.Equals("appsettings.Local.json", StringComparison.OrdinalIgnoreCase))
            return true;
        if (fileName.EndsWith(".Local.json", StringComparison.OrdinalIgnoreCase))
            return true;
        if (relativePath.Contains($"{Path.DirectorySeparatorChar}staging{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            return true;
        // No auto-reemplazar el updater en ejecución desde sí mismo de forma agresiva;
        // si viene en el zip, sí se copia (proceso ya corriendo desde extract).
        return false;
    }

    private static void StopService(string serviceName, TimeSpan timeout)
    {
        try
        {
            using var sc = new ServiceController(serviceName);
            if (sc.Status is ServiceControllerStatus.Stopped or ServiceControllerStatus.StopPending)
            {
                sc.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                return;
            }

            sc.Stop();
            sc.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
        }
        catch (InvalidOperationException)
        {
            // No instalado como servicio: matar proceso por nombre
            KillProcessByName("AlahiaPrinterApi");
            KillProcessByName("PrinterApi");
        }
    }

    private static void ApplyServiceResilience(string serviceName)
    {
        // Tras un update o un Windows Update, reafirmar arranque automatico y recovery.
        RunSc("config", serviceName, "start=", "delayed-auto");
        RunSc("config", serviceName, "depend=", "Spooler");
        RunSc("failure", serviceName, "reset=", "86400", "actions=", "restart/5000/restart/15000/restart/60000");
        RunSc("failureflag", serviceName, "1");
        EnsureWatchdogTask("AlahiaPrinterApi-Watchdog-Boot", "ONSTART", "0001:00", serviceName);
        EnsureWatchdogTask("AlahiaPrinterApi-Watchdog-Logon", "ONLOGON", "0000:20", serviceName);
    }

    private static void RunSc(params string[] args)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = string.Join(" ", args),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            p?.WaitForExit(15_000);
        }
        catch
        {
            // Si no hay privilegios, el servicio igual se intenta arrancar.
        }
    }

    private static void EnsureWatchdogTask(string taskName, string schedule, string delay, string serviceName)
    {
        try
        {
            var tr = $"cmd.exe /c sc.exe start {serviceName}";
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments =
                    $"/Create /TN \"{taskName}\" /SC {schedule} /DELAY {delay} " +
                    $"/RU SYSTEM /RL HIGHEST /F /TR \"{tr}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            p?.WaitForExit(15_000);
        }
        catch { /* ignore */ }
    }

    private static void StartService(string serviceName, TimeSpan timeout)
    {
        try
        {
            using var sc = new ServiceController(serviceName);
            if (sc.Status is ServiceControllerStatus.Running or ServiceControllerStatus.StartPending)
            {
                sc.WaitForStatus(ServiceControllerStatus.Running, timeout);
                return;
            }

            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running, timeout);
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"No se pudo iniciar el servicio (¿instalado?): {ex.Message}");
            throw;
        }
    }

    private static void KillProcessByName(string name)
    {
        foreach (var p in Process.GetProcessesByName(name))
        {
            try
            {
                p.Kill(entireProcessTree: true);
                p.WaitForExit(10_000);
            }
            catch { /* ignore */ }
        }
    }

    private static Options ParseArgs(string[] args)
    {
        return new Options
        {
            Service = GetArg(args, "--service") ?? "AlahiaPrinterApi",
            Source = GetArg(args, "--source") ?? "",
            Target = GetArg(args, "--target") ?? ""
        };
    }

    private static string? GetArg(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1].Trim('"');
        }
        return null;
    }

    private sealed class Options
    {
        public string Service { get; init; } = "";
        public string Source { get; init; } = "";
        public string Target { get; init; } = "";
    }
}
