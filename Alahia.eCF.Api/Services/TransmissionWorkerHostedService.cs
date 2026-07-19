using AlahiaPos.DataAccess.Servicios.FiscalGateway.Transmission;

namespace Alahia.eCF.Api.Services
{
    public sealed class TransmissionWorkerHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TransmissionWorkerHostedService> _logger;

        public TransmissionWorkerHostedService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<TransmissionWorkerHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var poll = Math.Max(1, _configuration.GetValue("TransmissionEngine:WorkerPollSeconds", 5));
            _logger.LogInformation("Transmission worker iniciado (poll={Poll}s)", poll);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var engine = scope.ServiceProvider.GetRequiredService<ITransmissionEngine>();
                    var n = await engine.ProcessPendingAsync(stoppingToken);
                    if (n > 0)
                        _logger.LogDebug("Transmission worker procesó {Count} job(s)", n);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en Transmission worker");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(poll), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
