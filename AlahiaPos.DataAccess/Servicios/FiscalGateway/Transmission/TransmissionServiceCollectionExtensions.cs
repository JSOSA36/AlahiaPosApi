using Microsoft.Extensions.DependencyInjection;
using AlahiaPos.DataAccess.Servicios.FiscalGateway.Transmission.Providers.Dgii;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.Transmission
{
    public static class TransmissionServiceCollectionExtensions
    {
        public static IServiceCollection AddAlahiaTransmissionEngine(
            this IServiceCollection services,
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            services.Configure<TransmissionEngineOptions>(configuration.GetSection(TransmissionEngineOptions.SectionName));
            services.Configure<TransmissionProvidersOptions>(configuration.GetSection(TransmissionProvidersOptions.SectionName));

            services.AddSingleton<ITransmissionJobStore, InMemoryTransmissionJobStore>();
            services.AddScoped<ITransmissionProviderResolver, TransmissionProviderResolver>();
            services.AddScoped<ITransmissionProvider, DgiiTransmissionProvider>();
            services.AddScoped<ITransmissionEngine, TransmissionEngine>();
            return services;
        }
    }
}
