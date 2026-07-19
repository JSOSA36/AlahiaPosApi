using AlahiaPos.DataAccess.Servicios.FiscalGateway.Http;
using AlahiaPos.Entities.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway
{
    /// <summary>
    /// Resuelve el gateway activo. En el ERP siempre es el adaptador HTTP configurado;
    /// no hay branch por nombre de proveedor.
    /// </summary>
    public sealed class FiscalGatewayFactory : IFiscalGatewayFactory
    {
        private readonly IServiceProvider _sp;
        private readonly FiscalGatewayOptions _opts;

        public FiscalGatewayFactory(IServiceProvider sp, IOptions<FiscalGatewayOptions> opts)
        {
            _sp = sp;
            _opts = opts.Value;
        }

        /// <summary>Endpoint configurado (BaseUrl). No identifica al vendor.</summary>
        public string CurrentProvider =>
            string.IsNullOrWhiteSpace(_opts.BaseUrl) ? "(sin BaseUrl)" : _opts.BaseUrl.Trim();

        public IFiscalGateway Get() => _sp.GetRequiredService<IFiscalGateway>();
    }
}
