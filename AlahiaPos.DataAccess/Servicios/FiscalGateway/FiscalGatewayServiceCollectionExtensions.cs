using AlahiaPos.DataAccess.Servicios.FiscalGateway.Http;
using AlahiaPos.Entities.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway
{
    /// <summary>
    /// Registra el único <see cref="IFiscalGateway"/> del ERP.
    /// El adaptador HTTP es detalle de infraestructura; el ERP solo ve la interfaz.
    /// </summary>
    public static class FiscalGatewayServiceCollectionExtensions
    {
        /// <summary>
        /// Lee <c>FiscalGateway</c> (preferido). Si falta BaseUrl, hace fallback a
        /// secciones legacy <c>PgEInvoicing</c> / <c>Einvoicing</c> para no romper deploys existentes.
        /// </summary>
        public static IServiceCollection AddFiscalGateway(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var options = ResolveOptions(configuration);
            services.Configure<FiscalGatewayOptions>(o =>
            {
                o.BaseUrl = options.BaseUrl;
                o.ApiKey = options.ApiKey;
                o.TimeoutSeconds = options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 30;
            });

            services.AddHttpClient(nameof(EmpresaFiscalGatewayResolver));
            services.AddHttpClient(nameof(Http.HttpReceiptFiscalGateway));
            services.AddScoped<IEmpresaFiscalGatewayResolver, EmpresaFiscalGatewayResolver>();
            // Transient: resuelve BaseUrl/ApiKey por empresa en cada emisión (no fija BaseAddress).
            services.AddTransient<IFiscalGateway, Http.HttpReceiptFiscalGateway>();
            services.AddSingleton<IFiscalDocumentoValidator, FiscalDocumentoValidator>();
            return services;
        }

        internal static FiscalGatewayOptions ResolveOptions(IConfiguration configuration)
        {
            var merged = new FiscalGatewayOptions();

            // Legacy → canónico (último gana)
            configuration.GetSection("Einvoicing").Bind(merged);
            configuration.GetSection("PgEInvoicing").Bind(merged);
            configuration.GetSection(FiscalGatewayOptions.SectionName).Bind(merged);

            return merged;
        }
    }
}
