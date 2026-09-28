using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AlahiaPosApi.Pase.Tests
{
    /// <summary>
    /// Humo contra AlahiaPos_Dev: maestros y documentos se pueden leer.
    /// No escribe. Si Dev no esta, sale sin fallar.
    /// </summary>
    public class ErpDevSmokeTests
    {
        [Fact]
        public async Task Dev_no_es_Prod_y_los_modulos_responden()
        {
            await using var ctx = PaseRepo.TryOpenDev();
            if (ctx == null)
                return;
            if (!await ctx.Database.CanConnectAsync())
                return;

            await ctx.Database.OpenConnectionAsync();
            try
            {
                string db;
                await using (var cmd = ctx.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = "SELECT DB_NAME()";
                    db = (await cmd.ExecuteScalarAsync())?.ToString() ?? "";
                }

                Assert.Equal("AlahiaPos_Dev", db);
                Assert.True(await ctx.Empresas.AsNoTracking().AnyAsync());
                Assert.True(await ctx.Usuarios.AsNoTracking().AnyAsync());
                Assert.True(await ctx.Sucursales.AsNoTracking().AnyAsync());
                Assert.True(await ctx.Productos.AsNoTracking().AnyAsync());
                Assert.True(await ctx.Clientes.AsNoTracking().AnyAsync());
                Assert.True(await ctx.Categorias.AsNoTracking().AnyAsync());
                Assert.True(await ctx.Almacenes.AsNoTracking().AnyAsync());
                Assert.True(await ctx.FacturaHeaders.AsNoTracking().AnyAsync());
                _ = await ctx.FacturaHeaders.AsNoTracking().CountAsync(h => h.IdTipoDocumentos == 10);
                _ = await ctx.FacturaHeaders.AsNoTracking().CountAsync(h => h.IdTipoDocumentos == 1);
                _ = await ctx.CajaApertura.AsNoTracking().CountAsync();
                _ = await ctx.CajaCierre.AsNoTracking().CountAsync();
                _ = await ctx.Gastos.AsNoTracking().CountAsync();
                _ = await ctx.Ingresos.AsNoTracking().CountAsync();
                _ = await ctx.MovimientosInventario.AsNoTracking().CountAsync();
                _ = await ctx.CuentaFinanciera.AsNoTracking().CountAsync();
                _ = await ctx.Citas.AsNoTracking().CountAsync();
                _ = await ctx.Proveedores.AsNoTracking().CountAsync();
                _ = await ctx.Perfiles.AsNoTracking().CountAsync();
                _ = await ctx.Modulos.AsNoTracking().CountAsync();
                _ = await ctx.EmpleadosP.AsNoTracking().CountAsync();
            }
            finally
            {
                await ctx.Database.CloseConnectionAsync();
            }
        }
    }
}
