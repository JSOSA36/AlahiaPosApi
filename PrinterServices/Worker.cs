using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PrinterServices.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PrinterServices
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public Worker(
            ILogger<Worker> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🔥 Servicio de impresión iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var printer = scope.ServiceProvider.GetRequiredService<IPrinter>();

                        await printer.GenerateTicketLavador(55);
                    }

                    _logger.LogInformation("Chequeo impresión: {time}", DateTime.Now);

                    await Task.Delay(12000, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en servicio de impresión");
                }
            }

            _logger.LogInformation("Servicio detenido");
        }
    }
}