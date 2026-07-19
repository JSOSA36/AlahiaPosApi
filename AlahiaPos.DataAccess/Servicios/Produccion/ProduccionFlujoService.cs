using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios.Produccion
{
    public class ProduccionFlujoService : IProduccionFlujoService
    {
        private readonly AlahiaPosContext _ctx;

        public ProduccionFlujoService(AlahiaPosContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<ProduccionFlujoDto?> ObtenerFlujoActivoAsync(int idEmpresa, string tipoTrabajoCodigo)
        {
            var flujo = await _ctx.ProduccionFlujo.AsNoTracking()
                .Include(f => f.Estados)
                .Where(f => f.Activo && f.IdEmpresa == idEmpresa && f.TipoTrabajoCodigo == tipoTrabajoCodigo)
                .OrderByDescending(f => f.Version)
                .FirstOrDefaultAsync();

            if (flujo == null)
            {
                flujo = await _ctx.ProduccionFlujo.AsNoTracking()
                    .Include(f => f.Estados)
                    .Where(f => f.Activo && f.IdEmpresa == null && f.TipoTrabajoCodigo == tipoTrabajoCodigo)
                    .OrderByDescending(f => f.Version)
                    .FirstOrDefaultAsync();
            }

            return flujo == null ? null : Map(flujo);
        }

        public async Task<List<ProduccionFlujoDto>> ListarFlujosAsync(int idEmpresa)
        {
            var empresa = await _ctx.ProduccionFlujo.AsNoTracking()
                .Include(f => f.Estados)
                .Where(f => f.Activo && f.IdEmpresa == idEmpresa)
                .ToListAsync();

            var sistema = await _ctx.ProduccionFlujo.AsNoTracking()
                .Include(f => f.Estados)
                .Where(f => f.Activo && f.IdEmpresa == null)
                .ToListAsync();

            var tiposEmpresa = empresa.Select(e => e.TipoTrabajoCodigo).ToHashSet();
            var merged = empresa
                .Concat(sistema.Where(s => !tiposEmpresa.Contains(s.TipoTrabajoCodigo)))
                .OrderBy(f => f.TipoTrabajoCodigo)
                .ThenBy(f => f.Nombre)
                .Select(Map)
                .ToList();

            return merged;
        }

        private static ProduccionFlujoDto Map(Entities.Domain.ProduccionFlujo f) => new()
        {
            IdFlujo = f.IdFlujo,
            TipoTrabajoCodigo = f.TipoTrabajoCodigo,
            Nombre = f.Nombre,
            Version = f.Version,
            SlaObjetivoSegundos = f.SlaObjetivoSegundos,
            SlaAdvertenciaSegundos = f.SlaAdvertenciaSegundos,
            SlaModoInicio = NormalizarSlaModo(f.SlaModoInicio),
            Estados = (f.Estados ?? new List<Entities.Domain.ProduccionFlujoEstado>())
                .OrderBy(e => e.Orden)
                .Select(e => new ProduccionFlujoEstadoDto
                {
                    Codigo = e.Codigo,
                    NombreVisible = e.NombreVisible,
                    Orden = e.Orden,
                    EsInicial = e.EsInicial,
                    EsTerminal = e.EsTerminal,
                    CuentaParaCompletar = e.CuentaParaCompletar,
                    ColorHint = e.ColorHint
                })
                .ToList()
        };

        private static string NormalizarSlaModo(string? modo)
        {
            if (string.Equals(modo, ProduccionConstantes.SlaModoInicioPreparacion, StringComparison.OrdinalIgnoreCase))
                return ProduccionConstantes.SlaModoInicioPreparacion;
            return ProduccionConstantes.SlaModoCreacion;
        }
    }
}
