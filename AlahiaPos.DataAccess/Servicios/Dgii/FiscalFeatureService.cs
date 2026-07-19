using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AlahiaPos.DataAccess.Servicios.Dgii
{
    /// <summary>
    /// Evaluación centralizada de flags DGII. Sin fila de config = fiscal apagado.
    /// </summary>
    public class FiscalFeatureService : IFiscalFeatureService
    {
        private readonly AlahiaPosContext _context;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        public FiscalFeatureService(AlahiaPosContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<FiscalFeatureFlags> GetFeaturesAsync(int idEmpresa)
        {
            var key = CacheKey(idEmpresa);
            if (_cache.TryGetValue(key, out FiscalFeatureFlags? cached) && cached != null)
                return cached;

            var cfg = await _context.DgiiConfiguracionEmpresa
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdEmpresa == idEmpresa);

            FiscalFeatureFlags flags;
            if (cfg == null)
            {
                flags = FiscalFeatureFlags.Apagado(idEmpresa);
            }
            else
            {
                flags = new FiscalFeatureFlags
                {
                    IdEmpresa = idEmpresa,
                    TieneConfiguracion = true,
                    FiscalActivo = cfg.FiscalActivo,
                    Generar606 = cfg.Generar606,
                    Generar607 = cfg.Generar607,
                    GenerarIt1 = cfg.GenerarIt1,
                    FacturacionElectronicaActiva = cfg.FacturacionElectronicaActiva
                };
            }

            _cache.Set(key, flags, CacheTtl);
            return flags;
        }

        public Task InvalidateCacheAsync(int idEmpresa)
        {
            _cache.Remove(CacheKey(idEmpresa));
            return Task.CompletedTask;
        }

        private static string CacheKey(int idEmpresa) => $"fiscal-features:{idEmpresa}";
    }
}
