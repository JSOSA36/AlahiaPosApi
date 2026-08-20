using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace PrinterApi.Servicios;

/// <summary>
/// Consulta el manifiesto de releases y, si hay versión nueva, descarga el zip
/// y lanza PrinterUpdater.exe (proceso aparte) para reemplazar binarios.
/// </summary>
public sealed class PrinterUpdateChecker : BackgroundService
{
    private readonly ILogger<PrinterUpdateChecker> _logger;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public PrinterUpdateChecker(
        ILogger<PrinterUpdateChecker> logger,
        IConfiguration config,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.GetValue("Update:Enabled", true))
        {
            _logger.LogInformation("Auto-update deshabilitado (Update:Enabled=false).");
            return;
        }

        var delaySeconds = Math.Max(5, _config.GetValue("Update:StartupDelaySeconds", 30));
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        var intervalHours = Math.Max(1, _config.GetValue("Update:CheckIntervalHours", 6));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndApplyAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error en ciclo de auto-update de PrinterApi.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task CheckAndApplyAsync(CancellationToken ct)
    {
        var manifestUrl = (_config["Update:ManifestUrl"] ?? "").Trim();
        if (string.IsNullOrWhiteSpace(manifestUrl))
        {
            _logger.LogDebug("Update:ManifestUrl vacío; se omite chequeo.");
            return;
        }

        var client = _httpClientFactory.CreateClient("PrinterUpdate");
        using var resp = await client.GetAsync(manifestUrl, ct);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        var manifest = await JsonSerializer.DeserializeAsync<PrinterReleaseManifest>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            ct);

        if (manifest == null || string.IsNullOrWhiteSpace(manifest.Version))
        {
            _logger.LogWarning("Manifiesto de PrinterApi inválido o sin version.");
            return;
        }

        if (!IsNewer(manifest.Version, PrinterAgentInfo.Version))
        {
            _logger.LogInformation(
                "PrinterApi al día (local {Local}, remoto {Remote}).",
                PrinterAgentInfo.Version,
                manifest.Version);
            return;
        }

        if (string.IsNullOrWhiteSpace(manifest.Url))
        {
            _logger.LogWarning("Manifiesto {Version} sin url de descarga.", manifest.Version);
            return;
        }

        _logger.LogInformation(
            "Nueva versión PrinterApi disponible: {Remote} (local {Local}). Descargando…",
            manifest.Version,
            PrinterAgentInfo.Version);

        var programData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Alahia",
            "PrinterApi");
        var stagingRoot = Path.Combine(programData, "staging");
        Directory.CreateDirectory(stagingRoot);

        var zipPath = Path.Combine(stagingRoot, $"AlahiaPrinterApi-{Sanitize(manifest.Version)}.zip");
        var extractDir = Path.Combine(stagingRoot, Sanitize(manifest.Version));

        await DownloadFileAsync(client, manifest.Url, zipPath, ct);

        if (!string.IsNullOrWhiteSpace(manifest.Sha256))
        {
            var hash = ComputeSha256(zipPath);
            if (!hash.Equals(manifest.Sha256.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(
                    "SHA256 del zip no coincide. Esperado {Expected}, obtenido {Actual}.",
                    manifest.Sha256,
                    hash);
                return;
            }
        }

        if (Directory.Exists(extractDir))
            Directory.Delete(extractDir, true);
        Directory.CreateDirectory(extractDir);
        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractDir);

        var updaterPath = ResolveUpdaterPath(extractDir);
        if (updaterPath == null)
        {
            _logger.LogError("No se encontró PrinterUpdater.exe en el paquete ni en la instalación.");
            return;
        }

        var installDir = AppContext.BaseDirectory.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);
        var serviceName = _config["Update:ServiceName"] ?? PrinterAgentInfo.ServiceName;

        var psi = new ProcessStartInfo
        {
            FileName = updaterPath,
            Arguments =
                $"--service \"{serviceName}\" " +
                $"--source \"{extractDir}\" " +
                $"--target \"{installDir}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(updaterPath) ?? installDir
        };

        _logger.LogInformation("Lanzando actualizador: {File} {Args}", psi.FileName, psi.Arguments);
        Process.Start(psi);
        // El updater detiene este servicio; no esperar aquí.
    }

    private static async Task DownloadFileAsync(
        HttpClient client,
        string url,
        string destination,
        CancellationToken ct)
    {
        using var resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        await using var input = await resp.Content.ReadAsStreamAsync(ct);
        await using var output = File.Create(destination);
        await input.CopyToAsync(output, ct);
    }

    private static string ComputeSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash);
    }

    private static string? ResolveUpdaterPath(string extractDir)
    {
        var candidates = new[]
        {
            Path.Combine(extractDir, "PrinterUpdater.exe"),
            Path.Combine(extractDir, "AlahiaPrinterUpdater.exe"),
            Path.Combine(AppContext.BaseDirectory, "PrinterUpdater.exe"),
            Path.Combine(AppContext.BaseDirectory, "AlahiaPrinterUpdater.exe")
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    /// <summary>True si remote es semver mayor que local.</summary>
    public static bool IsNewer(string remote, string local)
    {
        if (!Version.TryParse(Normalize(remote), out var r))
            return false;
        if (!Version.TryParse(Normalize(local), out var l))
            return true;
        return r > l;
    }

    private static string Normalize(string v)
    {
        v = (v ?? "").Trim();
        var plus = v.IndexOf('+');
        if (plus > 0) v = v[..plus];
        var parts = v.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return $"{parts[0]}.0.0";
        if (parts.Length == 2) return $"{parts[0]}.{parts[1]}.0";
        return string.Join('.', parts.Take(4));
    }

    private static string Sanitize(string version) =>
        string.Join("_", version.Split(Path.GetInvalidFileNameChars()));

    private sealed class PrinterReleaseManifest
    {
        public string Version { get; set; } = "";
        public string Url { get; set; } = "";
        public string? Sha256 { get; set; }
        public bool Mandatory { get; set; }
        public string? Notes { get; set; }
    }
}
