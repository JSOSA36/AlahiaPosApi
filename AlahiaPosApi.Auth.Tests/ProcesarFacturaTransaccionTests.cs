using AlahiaPos.DataAccess.Data;
using AlahiaPos.DataAccess.Servicios.Ventas;
using AlahiaPos.Entities.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;

namespace AlahiaPosApi.Auth.Tests
{
    public class ProcesarFacturaTransaccionTests
    {
        private static string DevConnection()
        {
            var env = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
            if (!string.IsNullOrWhiteSpace(env)
                && env.Contains("AlahiaPos_Dev", StringComparison.OrdinalIgnoreCase))
            {
                return env;
            }

            string? path = null;
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, "artifacts", "pedidos-api", "appsettings.json");
                if (File.Exists(candidate))
                {
                    path = candidate;
                    break;
                }
            }

            Assert.True(path != null, "No se encontró artifacts/pedidos-api/appsettings.json para AlahiaPos_Dev.");
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var cs = doc.RootElement.GetProperty("ConnectionStrings").GetProperty("Default").GetString();
            Assert.False(string.IsNullOrWhiteSpace(cs));
            Assert.Contains("AlahiaPos_Dev", cs, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("AlahiaPos_Prod", cs, StringComparison.OrdinalIgnoreCase);
            return cs!;
        }

        private static AlahiaPosContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AlahiaPosContext>()
                .UseSqlServer(DevConnection())
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .Options;
            return new AlahiaPosContext(options);
        }

        [Fact]
        public async Task Rollback_no_deja_ingreso_ni_movimiento_parcial()
        {
            await using var ctx = CreateContext();
            var marca = $"TX-E2E-{Guid.NewGuid():N}";

            var ingresosAntes = await ctx.Ingresos.CountAsync(x => x.Referencia == marca);
            var movAntes = await ctx.MovimientosInventario.CountAsync(x => x.Referencia == marca);

            await using (var tx = await ctx.Database.BeginTransactionAsync())
            {
                ctx.Ingresos.Add(new Ingresos
                {
                    IdEmpresa = 62,
                    FechaRegistro = DateTime.Now,
                    Descripcion = marca,
                    Categoria = "TEST_TX",
                    Origen = "Test",
                    Monto = 1,
                    FormaPago = "Efectivo",
                    Referencia = marca
                });
                await ctx.SaveChangesAsync();

                ctx.MovimientosInventario.Add(new MovimientosInventario
                {
                    TipoMovimiento = "SALIDA",
                    Motivo = "VENTA",
                    Referencia = marca,
                    Observacion = "test rollback",
                    Fecha = DateTime.Now,
                    IdEmpresa = 62,
                    Activo = true
                });
                await ctx.SaveChangesAsync();

                await tx.RollbackAsync();
            }

            await using var verify = CreateContext();
            Assert.Equal(ingresosAntes, await verify.Ingresos.CountAsync(x => x.Referencia == marca));
            Assert.Equal(movAntes, await verify.MovimientosInventario.CountAsync(x => x.Referencia == marca));
        }

        [Fact]
        public async Task Commit_persiste_ingreso_y_se_puede_limpiar()
        {
            await using var ctx = CreateContext();
            var marca = $"TX-OK-{Guid.NewGuid():N}";

            await using (var tx = await ctx.Database.BeginTransactionAsync())
            {
                ctx.Ingresos.Add(new Ingresos
                {
                    IdEmpresa = 62,
                    FechaRegistro = DateTime.Now,
                    Descripcion = marca,
                    Categoria = "TEST_TX",
                    Origen = "Test",
                    Monto = 1,
                    FormaPago = "Efectivo",
                    Referencia = marca
                });
                await ctx.SaveChangesAsync();
                await tx.CommitAsync();
            }

            await using var verify = CreateContext();
            var row = await verify.Ingresos.FirstOrDefaultAsync(x => x.Referencia == marca);
            Assert.NotNull(row);

            await using var cleanup = CreateContext();
            var del = await cleanup.Ingresos.FirstAsync(x => x.Referencia == marca);
            cleanup.Ingresos.Remove(del);
            await cleanup.SaveChangesAsync();
        }

        [Fact]
        public async Task VentaYaConfirmada_factura_tipo1_dev_es_true()
        {
            await using var ctx = CreateContext();
            var factura = await ctx.FacturaHeaders.AsNoTracking()
                .Where(h => h.IdEmpresa == 62 && h.IdTipoDocumentos == 1 && !h.EstaCancelada)
                .Select(h => h.IdFacturaHeader)
                .FirstAsync();

            Assert.True(await ProcesarFacturaIntegridad.VentaYaConfirmadaAsync(ctx, factura));
        }

        [Fact]
        public async Task VentaYaConfirmada_orden_tipo10_es_false()
        {
            await using var ctx = CreateContext();
            var orden = await ctx.FacturaHeaders.AsNoTracking()
                .Where(h => h.IdEmpresa == 62 && h.IdTipoDocumentos == 10 && !h.EstaCancelada)
                .Select(h => (int?)h.IdFacturaHeader)
                .FirstOrDefaultAsync();

            if (orden is null)
            {
                Assert.False(await ProcesarFacturaIntegridad.VentaYaConfirmadaAsync(ctx, 0));
                return;
            }

            Assert.False(await ProcesarFacturaIntegridad.VentaYaConfirmadaAsync(ctx, orden.Value));
        }

        [Fact]
        public async Task ClaveIdempotencia_unica_por_empresa_rechaza_duplicado()
        {
            await using var ctx = CreateContext();
            var ids = await ctx.FacturaHeaders.AsNoTracking()
                .Where(h => h.IdEmpresa == 62 && h.IdTipoDocumentos == 1 && !h.EstaCancelada)
                .Select(h => h.IdFacturaHeader)
                .Take(2)
                .ToListAsync();
            Assert.True(ids.Count == 2, "Se necesitan dos facturas de Sabor Urbano para probar el índice.");

            var clave = $"ITX-{Guid.NewGuid():N}"[..32];
            await ctx.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE FacturaHeaders SET ClaveIdempotenciaVenta = {clave} WHERE IdFacturaHeader = {ids[0]}");

            var duplicado = false;
            try
            {
                await ctx.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE FacturaHeaders SET ClaveIdempotenciaVenta = {clave} WHERE IdFacturaHeader = {ids[1]}");
            }
            catch (Exception)
            {
                duplicado = true;
            }
            finally
            {
                await ctx.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE FacturaHeaders SET ClaveIdempotenciaVenta = NULL WHERE IdFacturaHeader = {ids[0]}");
                await ctx.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE FacturaHeaders SET ClaveIdempotenciaVenta = NULL WHERE IdFacturaHeader = {ids[1]}");
            }

            Assert.True(duplicado);
            var encontrada = await ProcesarFacturaIntegridad.BuscarFacturaPorClaveIdempotenciaAsync(ctx, 62, clave);
            Assert.Null(encontrada);
        }

        [Fact]
        public async Task BuscarFacturaPorClave_inexistente_es_null()
        {
            await using var ctx = CreateContext();
            var id = await ProcesarFacturaIntegridad.BuscarFacturaPorClaveIdempotenciaAsync(
                ctx, 62, Guid.NewGuid().ToString("N"));
            Assert.Null(id);
        }

        [Fact]
        public async Task Error_despues_de_SaveChanges_revierte_ingreso()
        {
            await using var ctx = CreateContext();
            var marca = $"TX-FAIL-{Guid.NewGuid():N}";

            try
            {
                await using var tx = await ctx.Database.BeginTransactionAsync();
                ctx.Ingresos.Add(new Ingresos
                {
                    IdEmpresa = 62,
                    FechaRegistro = DateTime.Now,
                    Descripcion = marca,
                    Categoria = "TEST_TX",
                    Origen = "Test",
                    Monto = 1,
                    FormaPago = "Efectivo",
                    Referencia = marca
                });
                await ctx.SaveChangesAsync();
                throw new InvalidOperationException("falla de tesorería simulada");
            }
            catch (InvalidOperationException)
            {
                // dispose de la transacción debe revertir
            }

            await using var verify = CreateContext();
            Assert.Equal(0, await verify.Ingresos.CountAsync(x => x.Referencia == marca));
        }

        [Fact]
        public async Task Indice_unico_ClaveIdempotenciaVenta_existe_en_Dev()
        {
            await using var ctx = CreateContext();
            var conn = ctx.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT CASE WHEN EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_FacturaHeaders_ClaveIdempotenciaVenta'
      AND object_id = OBJECT_ID(N'dbo.FacturaHeaders')
) THEN 1 ELSE 0 END";
            var val = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            Assert.Equal(1, val);
        }

        [Fact]
        public async Task Retry_misma_clave_encuentra_la_misma_factura()
        {
            await using var ctx = CreateContext();
            var id = await ctx.FacturaHeaders.AsNoTracking()
                .Where(h => h.IdEmpresa == 62 && h.IdTipoDocumentos == 1 && !h.EstaCancelada)
                .Select(h => h.IdFacturaHeader)
                .FirstAsync();

            var clave = $"POS-{Guid.NewGuid():N}";
            await ctx.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE FacturaHeaders SET ClaveIdempotenciaVenta = {clave} WHERE IdFacturaHeader = {id}");

            try
            {
                var primera = await ProcesarFacturaIntegridad.BuscarFacturaPorClaveIdempotenciaAsync(ctx, 62, clave);
                var retry = await ProcesarFacturaIntegridad.BuscarFacturaPorClaveIdempotenciaAsync(ctx, 62, clave);
                Assert.Equal(id, primera);
                Assert.Equal(primera, retry);
                Assert.True(await ProcesarFacturaIntegridad.VentaYaConfirmadaAsync(ctx, id));
            }
            finally
            {
                await ctx.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE FacturaHeaders SET ClaveIdempotenciaVenta = NULL WHERE IdFacturaHeader = {id}");
            }
        }

        [Fact]
        public async Task Venta_distinta_con_otra_clave_no_se_mezcla()
        {
            await using var ctx = CreateContext();
            var ids = await ctx.FacturaHeaders.AsNoTracking()
                .Where(h => h.IdEmpresa == 62 && h.IdTipoDocumentos == 1 && !h.EstaCancelada)
                .Select(h => h.IdFacturaHeader)
                .Take(2)
                .ToListAsync();
            Assert.True(ids.Count == 2);

            var claveA = $"POSA-{Guid.NewGuid():N}"[..36];
            var claveB = $"POSB-{Guid.NewGuid():N}"[..36];
            await ctx.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE FacturaHeaders SET ClaveIdempotenciaVenta = {claveA} WHERE IdFacturaHeader = {ids[0]}");
            await ctx.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE FacturaHeaders SET ClaveIdempotenciaVenta = {claveB} WHERE IdFacturaHeader = {ids[1]}");

            try
            {
                Assert.Equal(ids[0], await ProcesarFacturaIntegridad.BuscarFacturaPorClaveIdempotenciaAsync(ctx, 62, claveA));
                Assert.Equal(ids[1], await ProcesarFacturaIntegridad.BuscarFacturaPorClaveIdempotenciaAsync(ctx, 62, claveB));
                Assert.Null(await ProcesarFacturaIntegridad.BuscarFacturaPorClaveIdempotenciaAsync(ctx, 62, claveA + "x"));
            }
            finally
            {
                await ctx.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE FacturaHeaders SET ClaveIdempotenciaVenta = NULL WHERE IdFacturaHeader = {ids[0]}");
                await ctx.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE FacturaHeaders SET ClaveIdempotenciaVenta = NULL WHERE IdFacturaHeader = {ids[1]}");
            }
        }

        [Fact]
        public async Task Concurrencia_misma_clave_una_sola_factura()
        {
            await using var ctx = CreateContext();
            var ids = await ctx.FacturaHeaders.AsNoTracking()
                .Where(h => h.IdEmpresa == 62 && h.IdTipoDocumentos == 1 && !h.EstaCancelada)
                .Select(h => h.IdFacturaHeader)
                .Take(2)
                .ToListAsync();
            Assert.True(ids.Count == 2);

            var clave = $"CONC-{Guid.NewGuid():N}"[..36];
            await using var ctxA = CreateContext();
            await using var ctxB = CreateContext();

            try
            {
                await ctxA.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE FacturaHeaders SET ClaveIdempotenciaVenta = {clave} WHERE IdFacturaHeader = {ids[0]}");

                var segundoFallo = false;
                try
                {
                    await ctxB.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE FacturaHeaders SET ClaveIdempotenciaVenta = {clave} WHERE IdFacturaHeader = {ids[1]}");
                }
                catch
                {
                    segundoFallo = true;
                }

                Assert.True(segundoFallo);
                var id = await ProcesarFacturaIntegridad.BuscarFacturaPorClaveIdempotenciaAsync(ctx, 62, clave);
                Assert.Equal(ids[0], id);

                var tesoreria = ProcesarFacturaIntegridad.ClaveTesoreria(ids[0], "Efectivo");
                var inventario = ProcesarFacturaIntegridad.ReferenciaInventario(ids[0]);
                Assert.Equal($"VENTA-{ids[0]}-Efectivo", tesoreria);
                Assert.Equal($"Factura #{ids[0]}", inventario);
            }
            finally
            {
                await ctx.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE FacturaHeaders SET ClaveIdempotenciaVenta = NULL WHERE IdFacturaHeader = {ids[0]}");
                await ctx.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE FacturaHeaders SET ClaveIdempotenciaVenta = NULL WHERE IdFacturaHeader = {ids[1]}");
            }
        }
    }
}
