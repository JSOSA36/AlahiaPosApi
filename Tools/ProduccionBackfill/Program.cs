using System;
using System.Linq;
using System.Threading.Tasks;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.DataAccess.Servicios.Produccion;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Events;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Backfill Dev: órdenes POS existentes (IdTipoDocumentos=10) → ProduccionTrabajo.
/// Solo AlahiaPos_Dev. Uso: dotnet run -- [idEmpresa]
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var cs = "Server=144.126.143.154\\SQLEXPRESS,1433;Database=AlahiaPos_Dev;User Id=sa;Password=JoelAriel8787;Encrypt=False";
        if (!cs.Contains("AlahiaPos_Dev", StringComparison.OrdinalIgnoreCase))
            return 2;

        var idEmpresa = args.Length > 0 && int.TryParse(args[0], out var e) ? e : 59;

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddDbContext<AlahiaPosContext>(o => o.UseSqlServer(cs));
        services.AddScoped<IProduccionConfiguracionService, ProduccionConfiguracionService>();
        services.AddScoped<IProduccionFlujoService, ProduccionFlujoService>();
        services.AddScoped<IProduccionTrabajoService, ProduccionTrabajoService>();
        services.AddScoped<IEmpresaModulos, EmpresaModulosStub>();
        services.AddScoped<IModulo, ModuloStub>();

        await using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AlahiaPosContext>();
        var trabajos = scope.ServiceProvider.GetRequiredService<IProduccionTrabajoService>();

        var ordenes = await ctx.Set<FacturaHeaders>()
            .AsNoTracking()
            .Where(h => h.IdEmpresa == idEmpresa && h.IdTipoDocumentos == 10)
            .OrderByDescending(h => h.IdFacturaHeader)
            .Take(50)
            .ToListAsync();

        Console.WriteLine($"Empresa {idEmpresa}: {ordenes.Count} órdenes candidatas");

        var creados = 0;
        var omitidos = 0;

        foreach (var h in ordenes)
        {
            var ya = await ctx.ProduccionTrabajo.AsNoTracking().AnyAsync(t =>
                t.IdEmpresa == idEmpresa &&
                t.OrigenTipo == ProduccionConstantes.OrigenTipoFacturaHeader &&
                t.OrigenId == h.IdFacturaHeader);
            if (ya)
            {
                omitidos++;
                continue;
            }

            var dets = await ctx.Set<FacturaDetalles>()
                .AsNoTracking()
                .Where(d => d.IdFacturaHeader == h.IdFacturaHeader)
                .ToListAsync();

            var items = new System.Collections.Generic.List<ProduccionTrabajoItemSolicitudDto>();
            foreach (var d in dets)
            {
                var prod = await ctx.Set<Productos>().AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdProducto == d.IdProducto);
                items.Add(new ProduccionTrabajoItemSolicitudDto
                {
                    OrigenDetalleId = d.IdFacturaDetalle,
                    CodigoItem = prod?.CodigoBarra,
                    NombreItem = string.IsNullOrWhiteSpace(prod?.Nombre) ? $"Producto {d.IdProducto}" : prod!.Nombre!,
                    Cantidad = d.Cantidad <= 0 ? 1 : d.Cantidad,
                    Observacion = string.IsNullOrWhiteSpace(d.Comentario) ? null : d.Comentario
                });
            }

            if (items.Count == 0)
            {
                Console.WriteLine($"SKIP {h.NumeroDocumento}: sin ítems");
                continue;
            }

            var dto = await trabajos.CrearDesdeEventoAsync(new ProduccionTrabajoSolicitadoEvent
            {
                IdEmpresa = idEmpresa,
                IdUsuario = h.IdUsuario ?? 0,
                ReferenciaId = h.IdFacturaHeader,
                ReferenciaTipo = ProduccionConstantes.OrigenTipoFacturaHeader,
                TipoTrabajo = ProduccionConstantes.TipoPosOrden,
                OrigenModulo = ProduccionConstantes.OrigenModuloPos,
                OrigenTipo = ProduccionConstantes.OrigenTipoFacturaHeader,
                OrigenId = h.IdFacturaHeader,
                IdempotencyKey = ProduccionPosAdapter.BuildIdempotencyKey(idEmpresa, h.IdFacturaHeader),
                NumeroVisible = string.IsNullOrWhiteSpace(h.NumeroDocumento) ? h.IdFacturaHeader.ToString() : h.NumeroDocumento!,
                NombreVisible = string.IsNullOrWhiteSpace(h.NombreCuenta) ? "Sin nombre" : h.NombreCuenta!,
                Referencia = string.IsNullOrWhiteSpace(h.TipoOrden) ? null : h.TipoOrden,
                EtiquetaContexto = string.IsNullOrWhiteSpace(h.TipoOrden) ? null : h.TipoOrden,
                Observacion = string.IsNullOrWhiteSpace(h.Nota) ? null : h.Nota,
                IdUsuarioSolicita = h.IdUsuario,
                Prioridad = ProduccionConstantes.PrioridadNormal,
                PlantillaCodigo = null,
                Items = items
            });

            if (dto != null)
            {
                creados++;
                Console.WriteLine($"OK  {h.NumeroDocumento} → trabajo {dto.IdTrabajo} ({dto.CodigoEstadoActual})");
            }
            else
            {
                Console.WriteLine($"FAIL {h.NumeroDocumento}: motor no creó trabajo");
            }
        }

        Console.WriteLine($"RESULTADO creados={creados} omitidos={omitidos}");
        return 0;
    }

    sealed class EmpresaModulosStub : IEmpresaModulos
    {
        public Task<bool> EmpresaTieneModulo(int empresaId, int moduloId) => Task.FromResult(true);
        public Task<System.Collections.Generic.IEnumerable<EmpresaModulo>> GetModulosByEmpresa(int empresaId)
            => Task.FromResult(Enumerable.Empty<EmpresaModulo>());
        public Task InsertEmpresaModulo(EmpresaModulo empresaModulo) => Task.CompletedTask;
        public void UpdateEmpresaModulo(int id, EmpresaModulo empresaModulo) { }
    }

    sealed class ModuloStub : IModulo
    {
        public Task<System.Collections.Generic.IEnumerable<Modulo>> GetAllModulos()
            => Task.FromResult(Enumerable.Empty<Modulo>());
        public Task<Modulo> GetModuloById(int id)
            => Task.FromResult(new Modulo { Id = id, Codigo = "X", Nombre = "X", Activo = true });
        public Task<Modulo?> GetModuloByCodigo(string codigo)
            => Task.FromResult<Modulo?>(new Modulo { Id = 1, Codigo = codigo, Nombre = codigo, Activo = true });
        public Task InsertModulo(Modulo modulo) => Task.CompletedTask;
        public void UpdateModulo(int id, Modulo modulo) { }
        public void DeleteModulo(int id) { }
    }
}
