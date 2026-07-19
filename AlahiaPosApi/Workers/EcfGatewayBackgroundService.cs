using AlahiaPos.DataAccess.Servicios.FiscalGateway;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AlahiaPosApi.Workers
{
    public class EcfGatewayBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EcfGatewayBackgroundService> _logger;
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

        public EcfGatewayBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<EcfGatewayBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EcfGatewayBackgroundService iniciado.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<EcfGatewayOutboxProcessor>();
                    var n = await processor.ProcessBatchAsync(stoppingToken, batchSize: 5);
                    if (n > 0)
                        _logger.LogDebug("ECF Gateway outbox procesó {N} eventos.", n);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error en ciclo EcfGatewayBackgroundService.");
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
