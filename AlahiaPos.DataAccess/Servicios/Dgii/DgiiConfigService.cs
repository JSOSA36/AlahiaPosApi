using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AlahiaPos.DataAccess.Servicios.Dgii
{
    public class DgiiConfigService : IDgiiConfigService
    {
        private readonly AlahiaPosContext _context;
        private readonly IFiscalFeatureService _features;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public DgiiConfigService(AlahiaPosContext context, IFiscalFeatureService features)
        {
            _context = context;
            _features = features;
        }

        public async Task<DgiiConfiguracionEmpresaDto> GetAsync(int idEmpresa)
        {
            var cfg = await _context.DgiiConfiguracionEmpresa
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdEmpresa == idEmpresa);

            if (cfg == null)
            {
                return new DgiiConfiguracionEmpresaDto
                {
                    IdEmpresa = idEmpresa,
                    FiscalActivo = false,
                    Generar606 = false,
                    Generar607 = false,
                    GenerarIt1 = false,
                    FacturacionElectronicaActiva = false,
                    Activo = false
                };
            }

            return Map(cfg);
        }

        public async Task<DgiiConfiguracionEmpresaDto> UpsertAsync(
            DgiiConfiguracionEmpresaDto dto,
            int idUsuario,
            string? motivo = null)
        {
            var cfg = await _context.DgiiConfiguracionEmpresa
                .AsTracking()
                .FirstOrDefaultAsync(c => c.IdEmpresa == dto.IdEmpresa);

            var anterior = cfg == null ? null : Map(cfg);
            var esNuevo = cfg == null;

            if (cfg == null)
            {
                cfg = new DgiiConfiguracionEmpresa
                {
                    IdEmpresa = dto.IdEmpresa,
                    FechaCreacion = DateTime.Now
                };
                _context.DgiiConfiguracionEmpresa.Add(cfg);
            }

            cfg.FiscalActivo = dto.FiscalActivo;
            cfg.Generar606 = dto.Generar606;
            cfg.Generar607 = dto.Generar607;
            cfg.GenerarIt1 = dto.GenerarIt1;
            cfg.FacturacionElectronicaActiva = dto.FacturacionElectronicaActiva;
            cfg.RegimenTributarioCodigo = string.IsNullOrWhiteSpace(dto.RegimenTributarioCodigo)
                ? "ORDINARIO"
                : dto.RegimenTributarioCodigo;
            cfg.VersionInstructivoPreferida = string.IsNullOrWhiteSpace(dto.VersionInstructivoPreferida)
                ? "IT-1-2020"
                : dto.VersionInstructivoPreferida;
            cfg.RazonSocial = dto.RazonSocial;
            cfg.DeclaranteNombre = dto.DeclaranteNombre;
            cfg.DeclaranteCalidad = dto.DeclaranteCalidad;
            cfg.EsConstructor = dto.EsConstructor;
            cfg.EsComisionista = dto.EsComisionista;
            cfg.ObligadoLibroVentasSF = dto.ObligadoLibroVentasSF;
            cfg.Activo = dto.Activo;

            var nuevo = Map(cfg);
            _context.DgiiConfiguracionAuditoria.Add(new DgiiConfiguracionAuditoria
            {
                IdEmpresa = dto.IdEmpresa,
                IdUsuario = idUsuario,
                Fecha = DateTime.Now,
                Accion = esNuevo ? "CREATE" : "UPDATE",
                ValorAnteriorJson = anterior == null ? null : JsonSerializer.Serialize(anterior, JsonOptions),
                ValorNuevoJson = JsonSerializer.Serialize(nuevo, JsonOptions),
                Motivo = motivo ?? dto.MotivoCambio
            });

            await _context.SaveChangesAsync();
            await _features.InvalidateCacheAsync(dto.IdEmpresa);
            return nuevo;
        }

        private static DgiiConfiguracionEmpresaDto Map(DgiiConfiguracionEmpresa cfg) => new()
        {
            IdEmpresa = cfg.IdEmpresa,
            FiscalActivo = cfg.FiscalActivo,
            Generar606 = cfg.Generar606,
            Generar607 = cfg.Generar607,
            GenerarIt1 = cfg.GenerarIt1,
            FacturacionElectronicaActiva = cfg.FacturacionElectronicaActiva,
            RegimenTributarioCodigo = cfg.RegimenTributarioCodigo,
            VersionInstructivoPreferida = cfg.VersionInstructivoPreferida,
            Activo = cfg.Activo,
            RazonSocial = cfg.RazonSocial,
            DeclaranteNombre = cfg.DeclaranteNombre,
            DeclaranteCalidad = cfg.DeclaranteCalidad,
            EsConstructor = cfg.EsConstructor,
            EsComisionista = cfg.EsComisionista,
            ObligadoLibroVentasSF = cfg.ObligadoLibroVentasSF
        };
    }
}
