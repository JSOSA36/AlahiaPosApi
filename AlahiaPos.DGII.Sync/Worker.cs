using AlahiaPos.DGII.Sync.Interfaces;

namespace AlahiaPos.DGII.Sync
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _sp;

        public Worker(ILogger<Worker> logger, IServiceProvider sp)
        {
            _logger = logger;
            _sp = sp;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🟢 Worker iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var ahora = DateTime.Now;

                    var proximaEjecucion = DateTime.Today.AddHours(4);

                    if (ahora >= proximaEjecucion)
                        proximaEjecucion = proximaEjecucion.AddDays(1);

                    var delay = proximaEjecucion - ahora;

                    _logger.LogInformation($"⏳ Próxima ejecución en: {delay}");

                    await Task.Delay(delay, stoppingToken);

                    _logger.LogInformation("🚀 Ejecutando sincronización DGII...");

                    using var scope = _sp.CreateScope();

                    var sync = scope.ServiceProvider
                        .GetRequiredService<IDgiiSyncService>();

                    await sync.EjecutarSyncAsync(stoppingToken);

                    _logger.LogInformation("✅ Sincronización completada");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error en el Worker DGII");
                }
            }
        }
    }
}