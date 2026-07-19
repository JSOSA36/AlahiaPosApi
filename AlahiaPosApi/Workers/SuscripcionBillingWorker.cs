using AlahiaPos.Entities.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AlahiaPosApi.Workers
{
    /// <summary>
    /// Procesa diariamente el ciclo de cobros SaaS (avisos + suspensión).
    /// </summary>
    public class SuscripcionBillingWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SuscripcionBillingWorker> _logger;
        private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
        private DateTime? _ultimaEjecucionDia;

        public SuscripcionBillingWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<SuscripcionBillingWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SuscripcionBillingWorker iniciado.");

            // Primera pasada al arrancar (tras breve espera)
            try { await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken); }
            catch (OperationCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var hoy = DateTime.Now.Date;
                    // Ejecutar al menos una vez al día, y también en cada tick si aún no corrió hoy
                    if (_ultimaEjecucionDia != hoy || DateTime.Now.Hour == 6)
                    {
                        if (_ultimaEjecucionDia != hoy || DateTime.Now.Minute < 15)
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var svc = scope.ServiceProvider.GetRequiredService<ISuscripcionCobroService>();
                            await svc.ProcesarCicloDiarioAsync(stoppingToken);
                            _ultimaEjecucionDia = hoy;
                            _logger.LogInformation("Ciclo de suscripciones procesado para {Fecha}", hoy);
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error en SuscripcionBillingWorker");
                }

                try
                {
                    await Task.Delay(Interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
