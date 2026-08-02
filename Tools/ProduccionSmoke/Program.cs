using System;
using System.Collections.Generic;
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
/// Smoke Etapa 1 Centro de Producción — solo AlahiaPos_Dev.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var cs = args.FirstOrDefault()
            ?? "Server=144.126.143.154\\SQLEXPRESS,1433;Database=AlahiaPos_Dev;User Id=sa;Password=JoelAriel8787;Encrypt=False";

        if (!cs.Contains("AlahiaPos_Dev", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("ABORT: connection string debe apuntar a AlahiaPos_Dev.");
            return 2;
        }

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
        var config = scope.ServiceProvider.GetRequiredService<IProduccionConfiguracionService>();

        var idEmpresa = await ctx.ProduccionConfiguracionEmpresa
            .AsNoTracking()
            .Where(c => c.Activo)
            .Select(c => c.IdEmpresa)
            .FirstAsync();

        Console.WriteLine($"Empresa prueba: {idEmpresa}");
        Console.WriteLine($"DB: AlahiaPos_Dev");

        var pass = 0;
        var fail = 0;
        void Ok(string name) { pass++; Console.WriteLine($"PASS  {name}"); }
        void Fail(string name, string detail) { fail++; Console.WriteLine($"FAIL  {name}: {detail}"); }

        if (await config.EstaActivoAsync(idEmpresa)) Ok("Config activa");
        else Fail("Config activa", "Esperaba Activo=true");

        var key = $"SMOKE:POS_ORDEN:{idEmpresa}:{DateTime.UtcNow.Ticks}";
        var origenId = unchecked((int)(DateTime.UtcNow.Ticks % 1_900_000_000));
        if (origenId <= 0) origenId = Math.Abs(origenId) + 1;

        var creado = await trabajos.CrearDesdeEventoAsync(new ProduccionTrabajoSolicitadoEvent
        {
            IdEmpresa = idEmpresa,
            IdUsuario = 1,
            ReferenciaId = origenId,
            ReferenciaTipo = ProduccionConstantes.OrigenTipoFacturaHeader,
            TipoTrabajo = ProduccionConstantes.TipoPosOrden,
            OrigenModulo = ProduccionConstantes.OrigenModuloPos,
            OrigenTipo = ProduccionConstantes.OrigenTipoFacturaHeader,
            OrigenId = origenId,
            IdempotencyKey = key,
            NumeroVisible = $"SMOKE-{origenId}",
            NombreVisible = "Smoke Test Cliente",
            Referencia = "Local",
            EtiquetaContexto = "Local",
            Observacion = "Prueba Bloque 7",
            IdUsuarioSolicita = 1,
            Prioridad = ProduccionConstantes.PrioridadNormal,
            PlantillaCodigo = null,
            Items = new List<ProduccionTrabajoItemSolicitudDto>
            {
                new() { NombreItem = "Item smoke A", Cantidad = 2, Observacion = "obs" },
                new() { NombreItem = "Item smoke B", Cantidad = 1 }
            }
        });

        if (creado == null) { Fail("CrearDesdeEvento", "null"); return Exit(pass, fail); }
        Ok($"CrearDesdeEvento id={creado.IdTrabajo} estado={creado.CodigoEstadoActual}");

        var otraVez = await trabajos.CrearDesdeEventoAsync(new ProduccionTrabajoSolicitadoEvent
        {
            IdEmpresa = idEmpresa,
            TipoTrabajo = ProduccionConstantes.TipoPosOrden,
            OrigenTipo = ProduccionConstantes.OrigenTipoFacturaHeader,
            OrigenId = origenId,
            IdempotencyKey = key,
            NumeroVisible = "x",
            NombreVisible = "x",
            Items = new List<ProduccionTrabajoItemSolicitudDto> { new() { NombreItem = "Ignorado", Cantidad = 1 } }
        });
        if (otraVez?.IdTrabajo == creado.IdTrabajo) Ok("Idempotencia misma key");
        else Fail("Idempotencia", $"ids {creado.IdTrabajo} vs {otraVez?.IdTrabajo}");

        var activos = await trabajos.ListarActivosAsync(idEmpresa, ProduccionConstantes.TipoPosOrden);
        if (activos.Any(t => t.IdTrabajo == creado.IdTrabajo)) Ok("ListarActivos contiene trabajo");
        else Fail("ListarActivos", "no aparece");

        var t1 = await trabajos.TransicionarAsync(idEmpresa, creado.IdTrabajo, new ProduccionTransicionRequest
        {
            CodigoEstadoEsperado = creado.CodigoEstadoActual,
            CodigoEstadoNuevo = "EN_PREPARACION",
            RowVersion = creado.RowVersion,
            IdUsuario = 1
        });
        if (t1.CodigoEstadoActual == "EN_PREPARACION") Ok("Transición → EN_PREPARACION");
        else Fail("Transición", t1.CodigoEstadoActual);

        try
        {
            await trabajos.TransicionarAsync(idEmpresa, creado.IdTrabajo, new ProduccionTransicionRequest
            {
                CodigoEstadoEsperado = "EN_PREPARACION",
                CodigoEstadoNuevo = "LISTA",
                RowVersion = creado.RowVersion,
                IdUsuario = 1
            });
            Fail("Concurrencia", "esperaba InvalidOperationException");
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("modificado", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("no coincide", StringComparison.OrdinalIgnoreCase))
        {
            Ok("Concurrencia rechazada con RowVersion vieja");
        }
        catch (Exception ex)
        {
            Fail("Concurrencia", ex.Message);
        }

        var t2 = await trabajos.TransicionarAsync(idEmpresa, creado.IdTrabajo, new ProduccionTransicionRequest
        {
            CodigoEstadoEsperado = t1.CodigoEstadoActual,
            CodigoEstadoNuevo = "LISTA",
            RowVersion = t1.RowVersion,
            IdUsuario = 1
        });
        Ok($"Transición → {t2.CodigoEstadoActual}");

        var cancelado = await trabajos.CancelarAsync(idEmpresa, creado.IdTrabajo, new ProduccionCancelarRequest
        {
            CodigoEstadoEsperado = t2.CodigoEstadoActual,
            RowVersion = t2.RowVersion,
            Motivo = "Fin smoke Bloque 7",
            IdUsuario = 1
        });
        if (cancelado.CodigoEstadoActual == "CANCELADA" && !cancelado.ActivoEnTablero)
            Ok("Cancelar → CANCELADA fuera de tablero");
        else
            Fail("Cancelar", $"{cancelado.CodigoEstadoActual} activo={cancelado.ActivoEnTablero}");

        var hist = await trabajos.ListarHistorialAsync(idEmpresa, creado.IdTrabajo);
        if (hist.Count >= 3) Ok($"Historial entradas={hist.Count}");
        else Fail("Historial", $"count={hist.Count}");

        var cfg = await ctx.ProduccionConfiguracionEmpresa
            .AsTracking()
            .FirstAsync(c => c.IdEmpresa == idEmpresa);
        cfg.Activo = false;
        await ctx.SaveChangesAsync();
        try
        {
            var noCrear = await trabajos.CrearDesdeEventoAsync(new ProduccionTrabajoSolicitadoEvent
            {
                IdEmpresa = idEmpresa,
                TipoTrabajo = ProduccionConstantes.TipoPosOrden,
                OrigenTipo = ProduccionConstantes.OrigenTipoFacturaHeader,
                OrigenId = origenId + 7,
                IdempotencyKey = key + ":OFF",
                NumeroVisible = "OFF",
                NombreVisible = "OFF",
                Items = new List<ProduccionTrabajoItemSolicitudDto> { new() { NombreItem = "X", Cantidad = 1 } }
            });
            if (noCrear == null) Ok("Config inactiva → no crea trabajo");
            else Fail("Config inactiva", $"creó id={noCrear.IdTrabajo}");
        }
        finally
        {
            cfg.Activo = true;
            await ctx.SaveChangesAsync();
            Ok("Config restaurada Activo=true");
        }

        return Exit(pass, fail);
    }

    static int Exit(int pass, int fail)
    {
        Console.WriteLine();
        Console.WriteLine($"RESULTADO  pass={pass}  fail={fail}");
        return fail == 0 ? 0 : 1;
    }

    sealed class EmpresaModulosStub : IEmpresaModulos
    {
        public Task<bool> EmpresaTieneModulo(int empresaId, int moduloId) => Task.FromResult(true);
        public Task<IEnumerable<EmpresaModulo>> GetModulosByEmpresa(int empresaId)
            => Task.FromResult(Enumerable.Empty<EmpresaModulo>());
        public Task InsertEmpresaModulo(EmpresaModulo empresaModulo) => Task.CompletedTask;
        public void UpdateEmpresaModulo(int id, EmpresaModulo empresaModulo) { }
    }

    sealed class ModuloStub : IModulo
    {
        public Task<IEnumerable<Modulo>> GetAllModulos()
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
