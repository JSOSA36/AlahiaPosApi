using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto.AlahiaAi;
using AlahiaPos.Entities.Interfaces.AlahiaAi;
using Microsoft.EntityFrameworkCore;
using PrinterLibrary;

namespace AlahiaPos.DataAccess.Servicios.AlahiaAi
{
    public interface IEmpresaAiConfigService
    {
        Task<EmpresaAiConfigDto> ObtenerAsync(int idEmpresa, CancellationToken ct = default);
        Task<EmpresaAiConfigDto> GuardarAsync(GuardarEmpresaAiConfigDto dto, CancellationToken ct = default);
        /// <summary>Config operativa con key desencriptada (solo uso interno backend).</summary>
        Task<EmpresaAiRuntimeConfig?> ObtenerRuntimeAsync(int idEmpresa, CancellationToken ct = default);
    }

    public class EmpresaAiRuntimeConfig
    {
        public string Provider { get; set; } = "OpenAI";
        public string Model { get; set; } = "gpt-4o-mini";
        public string? BaseUrl { get; set; }
        public string ApiKey { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    public class EmpresaAiConfigService : IEmpresaAiConfigService
    {
        private readonly AlahiaPosContext _ctx;

        public EmpresaAiConfigService(AlahiaPosContext ctx) => _ctx = ctx;

        public async Task<EmpresaAiConfigDto> ObtenerAsync(int idEmpresa, CancellationToken ct = default)
        {
            var row = await _ctx.EmpresaAiConfig.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdEmpresa == idEmpresa, ct);

            if (row == null)
            {
                return new EmpresaAiConfigDto
                {
                    IdEmpresa = idEmpresa,
                    Provider = "OpenAI",
                    Model = "gpt-4o-mini",
                    Activo = false,
                    HasApiKey = false
                };
            }

            return MapSafe(row);
        }

        public async Task<EmpresaAiConfigDto> GuardarAsync(GuardarEmpresaAiConfigDto dto, CancellationToken ct = default)
        {
            if (dto.IdEmpresa <= 0)
                throw new ArgumentException("IdEmpresa inválido.");

            var provider = (dto.Provider ?? "OpenAI").Trim();
            if (string.IsNullOrWhiteSpace(provider))
                provider = "OpenAI";

            var row = await _ctx.EmpresaAiConfig
                .FirstOrDefaultAsync(x => x.IdEmpresa == dto.IdEmpresa, ct);

            if (row == null)
            {
                row = new EmpresaAiConfig
                {
                    IdEmpresa = dto.IdEmpresa,
                    FechaCreacion = DateTime.UtcNow
                };
                _ctx.EmpresaAiConfig.Add(row);
            }

            row.Provider = provider;
            row.Model = string.IsNullOrWhiteSpace(dto.Model) ? "gpt-4o-mini" : dto.Model.Trim();
            row.BaseUrl = string.IsNullOrWhiteSpace(dto.BaseUrl) ? null : dto.BaseUrl.Trim();
            row.Activo = dto.Activo;
            row.FechaActualizacion = DateTime.UtcNow;
            row.IdUsuarioActualizacion = dto.IdUsuario > 0 ? dto.IdUsuario : null;

            if (dto.ClearApiKey)
            {
                row.ApiKeyCipher = null;
            }
            else if (!string.IsNullOrWhiteSpace(dto.ApiKey))
            {
                row.ApiKeyCipher = Utility.EncryptString(dto.ApiKey.Trim());
            }

            await _ctx.SaveChangesAsync(ct);
            return MapSafe(row);
        }

        public async Task<EmpresaAiRuntimeConfig?> ObtenerRuntimeAsync(int idEmpresa, CancellationToken ct = default)
        {
            var row = await _ctx.EmpresaAiConfig.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdEmpresa == idEmpresa && x.Activo, ct);
            if (row == null || string.IsNullOrWhiteSpace(row.ApiKeyCipher))
                return null;

            string plain;
            try
            {
                plain = Utility.DecryptString(row.ApiKeyCipher);
            }
            catch
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(plain))
                return null;

            return new EmpresaAiRuntimeConfig
            {
                Provider = row.Provider,
                Model = row.Model,
                BaseUrl = row.BaseUrl,
                ApiKey = plain,
                Activo = row.Activo
            };
        }

        private static EmpresaAiConfigDto MapSafe(EmpresaAiConfig row) => new()
        {
            IdEmpresa = row.IdEmpresa,
            Provider = row.Provider,
            Model = row.Model,
            BaseUrl = row.BaseUrl,
            Activo = row.Activo,
            HasApiKey = !string.IsNullOrWhiteSpace(row.ApiKeyCipher),
            FechaActualizacion = row.FechaActualizacion
        };
    }
}
