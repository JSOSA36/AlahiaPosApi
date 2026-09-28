using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.Extensions.Options;

namespace AlahiaPosApi.Workers
{
    /// <summary>
    /// Recordatorio WhatsApp/correo de citas confirmadas, hora local RD.
    /// </summary>
    public class CitasWhatsAppRecordatorioWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CitasWhatsAppRecordatorioWorker> _logger;
        private readonly WhatsAppCitasOptions _opt;
        private DateTime? _ultimoDiaEnviado;
        private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(15);

        public CitasWhatsAppRecordatorioWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<CitasWhatsAppRecordatorioWorker> logger,
            IOptions<WhatsAppCitasOptions> options)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _opt = options?.Value ?? new WhatsAppCitasOptions();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CitasWhatsAppRecordatorioWorker iniciado.");
            try { await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken); }
            catch (OperationCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var ahora = AhoraRepublicaDominicana();
                    var hoy = ahora.Date;
                    if (_ultimoDiaEnviado != hoy && HoraAlcanzada(ahora.TimeOfDay))
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var citas = scope.ServiceProvider.GetRequiredService<ICitas>();
                        var n = await citas.EnviarRecordatoriosDelDiaAsync(hoy);
                        _ultimoDiaEnviado = hoy;
                        _logger.LogInformation("Recordatorios de citas {Fecha}: {N}", hoy, n);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error en CitasWhatsAppRecordatorioWorker");
                }

                try { await Task.Delay(Intervalo, stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }

        private bool HoraAlcanzada(TimeSpan ahora)
        {
            if (!TimeSpan.TryParse(_opt.HoraRecordatorio, out var meta))
                meta = new TimeSpan(8, 0, 0);
            return ahora >= meta;
        }

        internal static DateTime AhoraRepublicaDominicana()
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Santo_Domingo");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            }
            catch
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Atlantic Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            }
        }
    }
}
