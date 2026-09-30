using AlahiaBackup.Configuration;
using AlahiaBackup.Interfaces;
using AlahiaBackup.Models;
using Microsoft.Extensions.Options;

namespace AlahiaBackup.Services
{
    public sealed class BackupService : IBackupService
    {
        private readonly ILogger<BackupService> _logger;
        private readonly IHostEnvironment _env;
        private readonly BackupSettings _settings;
        private readonly ISqlBackupService _sql;
        private readonly IBackupEmailService _email;
        private readonly TimeZoneInfo _zona;
        private readonly TimeSpan _hora;
        private BackupStateStore? _stateStore;
        private bool _avisoDevelopment;

        public BackupService(
            ILogger<BackupService> logger,
            IHostEnvironment env,
            IOptions<BackupSettings> settings,
            ISqlBackupService sql,
            IBackupEmailService email)
        {
            _logger = logger;
            _env = env;
            _settings = settings.Value;
            _sql = sql;
            _email = email;
            _zona = ResolverZona(_settings.ZonaHoraria);
            _hora = ParseHora(_settings.HoraBackup);
        }

        public async Task EjecutarBackupAsync(CancellationToken cancellationToken)
        {
            if (_env.IsDevelopment())
            {
                if (!_avisoDevelopment)
                {
                    _logger.LogInformation("AlahiaBackup no corre en Development. En el VPS el servicio usa Production.");
                    _avisoDevelopment = true;
                }
                return;
            }

            if (!_settings.Enabled)
                return;

            var ahora = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _zona);
            if (ahora.TimeOfDay < _hora)
                return;

            var hoy = DateOnly.FromDateTime(ahora.DateTime);
            var hoyTexto = hoy.ToString("yyyy-MM-dd");
            var store = Store();
            var state = store.Load();
            if (!string.Equals(state.Fecha, hoyTexto, StringComparison.Ordinal))
            {
                state = new BackupRunState { Fecha = hoyTexto };
            }

            if (state.CorreoOk || state.Agotado)
                return;

            if (state.UltimoIntentoUtc is { } ultimo
                && DateTime.UtcNow - ultimo < TimeSpan.FromMinutes(Math.Max(1, _settings.MinutosEntreReintentos)))
                return;

            state.Intentos++;
            state.UltimoIntentoUtc = DateTime.UtcNow;

            try
            {
                var bases = (_settings.BasesDeDatos ?? [])
                    .Where(b => !string.IsNullOrWhiteSpace(b))
                    .Select(b => b.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (bases.Length == 0)
                    throw new InvalidOperationException("Backup:BasesDeDatos está vacío.");

                var archivos = new List<BackupArtifact>(bases.Length);
                foreach (var baseDeDatos in bases)
                {
                    archivos.Add(await _sql.RealizarBackupAsync(baseDeDatos, hoy, cancellationToken));
                }

                var enviado = await _email.EnviarAsync(archivos, hoy, cancellationToken);
                state.CorreoOk = enviado;
                state.Agotado = !enviado;
                state.UltimoError = enviado
                    ? null
                    : "El archivo quedó en disco porque supera el tamaño que acepta el correo.";
                store.Save(state);

                if (enviado)
                {
                    _sql.EliminarAntiguos(_settings.DiasRetencion);
                    _logger.LogInformation("Backup del {Fecha} enviado a {Correo}", hoyTexto, _settings.CorreoDestino);
                }
                else
                {
                    _logger.LogWarning("Backup del {Fecha} guardado en disco, sin adjunto por tamaño.", hoyTexto);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                state.UltimoError = ex.Message;
                state.Agotado = state.Intentos >= Math.Max(1, _settings.Reintentos);
                store.Save(state);
                _logger.LogError(ex, "Falló el backup del {Fecha}. Intento {Intento}.", hoyTexto, state.Intentos);

                try
                {
                    await _email.EnviarErrorAsync(ex.Message, state.Intentos, _settings.Reintentos, cancellationToken);
                }
                catch (Exception mailEx) when (mailEx is not OperationCanceledException)
                {
                    _logger.LogError(mailEx, "No se pudo avisar el fallo por correo.");
                }
            }
        }

        private BackupStateStore Store() => _stateStore ??= new BackupStateStore(_settings.RutaBackup);

        private static TimeSpan ParseHora(string hora)
        {
            if (TimeSpan.TryParseExact(hora, @"hh\:mm", null, out var parsed))
                return parsed;
            if (TimeSpan.TryParse(hora, out parsed))
                return parsed;
            return new TimeSpan(2, 0, 0);
        }

        private static TimeZoneInfo ResolverZona(string id)
        {
            if (!string.IsNullOrWhiteSpace(id) && TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zona))
                return zona;
            if (TimeZoneInfo.TryFindSystemTimeZoneById("Atlantic Standard Time", out zona))
                return zona;
            return TimeZoneInfo.Utc;
        }
    }
}
