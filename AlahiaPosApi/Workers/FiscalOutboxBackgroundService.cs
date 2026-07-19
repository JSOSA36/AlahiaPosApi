using AlahiaPos.Entities.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AlahiaPosApi.Workers
{
    /// <summary>
    /// Worker mínimo: procesa outbox fiscal sin bloquear el request del POS.
    /// </summary>
    public class FiscalOutboxBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FiscalOutboxBackgroundService> _logger;
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(3);

        public FiscalOutboxBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<FiscalOutboxBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("FiscalOutboxBackgroundService iniciado.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<IFiscalOutboxProcessor>();
                    var n = await processor.ProcessBatchAsync(stoppingToken, batchSize: 10);
                    if (n > 0)
                        _logger.LogDebug("Fiscal outbox procesó {N} eventos.", n);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error en ciclo FiscalOutboxBackgroundService.");
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
