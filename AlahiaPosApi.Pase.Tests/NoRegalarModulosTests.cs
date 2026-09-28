using Xunit;

namespace AlahiaPosApi.Pase.Tests
{
    public class NoRegalarModulosTests
    {
        [Fact]
        public void Servicio_solo_activa_Administrador()
        {
            var src = PaseRepo.ReadDataAccessFile(Path.Combine("Servicios", "PerfilRolesService.cs"));
            Assert.Contains("ActivarModuloSoloAdministradoresAsync", src);
            Assert.Contains("Nunca toca Cajero", src);
            Assert.Contains("Administrador", src);
        }

        [Fact]
        public void Plantilla_SQL_no_activa_todos_los_perfiles()
        {
            var path = Path.Combine(PaseRepo.FindRepoRoot(), "Scripts", "_Plantilla_ActivarModulo_SoloAdmin.sql");
            Assert.True(File.Exists(path), path);
            var sql = File.ReadAllText(path);
            Assert.Contains("NUNCA: INSERT/UPDATE PerfilRoles para TODOS", sql);
            Assert.Contains("Administrador", sql);
        }
    }
}
