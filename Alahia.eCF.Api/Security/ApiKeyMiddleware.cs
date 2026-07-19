using Microsoft.Extensions.Options;

namespace Alahia.eCF.Api.Security
{
    public class AlahiaEcfApiSettings
    {
        public string ApiKey { get; set; } = "";
        /// <summary>Si true, exige X-Api-Key en todos los endpoints excepto /swagger y /api/Receipt health opcionales.</summary>
        public bool RequireApiKey { get; set; } = true;
    }

    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AlahiaEcfApiSettings _settings;

        public ApiKeyMiddleware(RequestDelegate next, IOptions<AlahiaEcfApiSettings> settings)
        {
            _next = next;
            _settings = settings.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";
            if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
                path.Equals("/api/Receipt/health", StringComparison.OrdinalIgnoreCase) ||
                path.Equals("/api/ecf/health", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            if (!_settings.RequireApiKey || string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue("X-Api-Key", out var key) ||
                !string.Equals(key.ToString(), _settings.ApiKey, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "X-Api-Key inválida o ausente" });
                return;
            }

            await _next(context);
        }
    }
}
