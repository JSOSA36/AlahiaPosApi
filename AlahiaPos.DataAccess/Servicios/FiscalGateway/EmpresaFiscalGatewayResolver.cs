using AlahiaPos.DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway
{
    public interface IEmpresaFiscalGatewayResolver
    {
        Task<FiscalGatewayEndpoint> ResolveAsync(int idEmpresa, CancellationToken ct = default);
        Task<bool> VerificarConexionAsync(int idEmpresa, CancellationToken ct = default);
    }

    public sealed class EmpresaFiscalGatewayResolver : IEmpresaFiscalGatewayResolver
    {
        private readonly AlahiaPosContext _ctx;
        private readonly FiscalGatewayOptions _defaults;
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<EmpresaFiscalGatewayResolver> _logger;

        public EmpresaFiscalGatewayResolver(
            AlahiaPosContext ctx,
            IOptions<FiscalGatewayOptions> defaults,
            IHttpClientFactory httpFactory,
            ILogger<EmpresaFiscalGatewayResolver> logger)
        {
            _ctx = ctx;
            _defaults = defaults.Value;
            _httpFactory = httpFactory;
            _logger = logger;
        }

        public async Task<FiscalGatewayEndpoint> ResolveAsync(int idEmpresa, CancellationToken ct = default)
        {
            var emp = await _ctx.Empresas.AsNoTracking()
                .Where(e => e.IdEmpresa == idEmpresa)
                .Select(e => new
                {
                    e.ProveedorFE,
                    e.ProveedorFE_Nombre,
                    e.ProveedorFE_BaseUrl,
                    e.ProveedorFE_ApiKey,
                    e.ProveedorFE_Usuario,
                    e.ProveedorFE_Password
                })
                .FirstOrDefaultAsync(ct);

            var modo = ProveedorFiscalHelper.Normalize(emp?.ProveedorFE);
            if (ProveedorFiscalHelper.EsExterno(modo))
            {
                var url = (emp?.ProveedorFE_BaseUrl ?? "").Trim();
                if (string.IsNullOrWhiteSpace(url))
                    throw new InvalidOperationException(
                        $"Empresa {idEmpresa}: modo PROVEEDOR_EXTERNO requiere ProveedorFE_BaseUrl.");

                return new FiscalGatewayEndpoint
                {
                    Modo = ProveedorFiscalHelper.ProveedorExterno,
                    Nombre = emp?.ProveedorFE_Nombre,
                    BaseUrl = url.TrimEnd('/'),
                    ApiKey = emp?.ProveedorFE_ApiKey,
                    Usuario = emp?.ProveedorFE_Usuario,
                    Password = emp?.ProveedorFE_Password,
                    TimeoutSeconds = _defaults.TimeoutSeconds > 0 ? _defaults.TimeoutSeconds : 30
                };
            }

            if (string.IsNullOrWhiteSpace(_defaults.BaseUrl))
                throw new InvalidOperationException(
                    "FiscalGateway:BaseUrl no configurado (modo DGII_DIRECTO / Alahia.eCF.Api).");

            return new FiscalGatewayEndpoint
            {
                Modo = ProveedorFiscalHelper.DgiiDirecto,
                Nombre = "Alahia.eCF.Api",
                BaseUrl = _defaults.BaseUrl.Trim().TrimEnd('/'),
                ApiKey = _defaults.ApiKey,
                TimeoutSeconds = _defaults.TimeoutSeconds > 0 ? _defaults.TimeoutSeconds : 30
            };
        }

        public async Task<bool> VerificarConexionAsync(int idEmpresa, CancellationToken ct = default)
        {
            try
            {
                var ep = await ResolveAsync(idEmpresa, ct);
                var client = _httpFactory.CreateClient(nameof(EmpresaFiscalGatewayResolver));
                client.Timeout = TimeSpan.FromSeconds(Math.Min(30, Math.Max(5, ep.TimeoutSeconds)));
                // Invoice (PG.eInvoicing) no admite GET /api/Receipt (solo POST) → 405.
                // pendienteAprobacion es el ping compatible del contrato OpenAPI.
                var healthPath = ProveedorFiscalHelper.EsExterno(ep.Modo)
                    ? $"{ep.BaseUrl}/api/Receipt/pendienteAprobacion"
                    : $"{ep.BaseUrl}/api/Receipt";
                using var req = new HttpRequestMessage(HttpMethod.Get, healthPath);
                if (!string.IsNullOrWhiteSpace(ep.ApiKey))
                    req.Headers.TryAddWithoutValidation("X-Api-Key", ep.ApiKey);
                if (!string.IsNullOrWhiteSpace(ep.Usuario) && !string.IsNullOrWhiteSpace(ep.Password))
                {
                    var token = Convert.ToBase64String(
                        System.Text.Encoding.UTF8.GetBytes($"{ep.Usuario}:{ep.Password}"));
                    req.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
                }
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                _logger.LogInformation(
                    "Health gateway empresa={Empresa} modo={Modo} url={Url}",
                    idEmpresa, ep.Modo, healthPath);

                using var res = await client.SendAsync(req, ct);
                // 2xx = OK; 401/403 = llegó al host (credencial mala); 404 en Alahia ping también cuenta.
                var code = (int)res.StatusCode;
                return res.IsSuccessStatusCode || code is 401 or 403 or 404;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Health gateway falló empresa={Empresa}", idEmpresa);
                return false;
            }
        }
    }
}
