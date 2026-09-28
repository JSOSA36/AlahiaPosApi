using System.Text.Json;
using AlahiaPos.DataAccess.Data;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPosApi.Pase.Tests
{
    internal static class PaseRepo
    {
        public static string FindRepoRoot()
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                var sln = Path.Combine(dir.FullName, "AlahiaPos.sln");
                if (File.Exists(sln))
                    return dir.FullName;
            }

            throw new DirectoryNotFoundException("No se encontro AlahiaPos.sln");
        }

        public static string ReadApiFile(string relativeUnderAlahiaPosApi)
        {
            var path = Path.Combine(FindRepoRoot(), "AlahiaPosApi", relativeUnderAlahiaPosApi);
            if (!File.Exists(path))
                throw new FileNotFoundException(path);
            return File.ReadAllText(path);
        }

        public static string ReadDataAccessFile(string relativeUnderDataAccess)
        {
            var path = Path.Combine(FindRepoRoot(), "AlahiaPos.DataAccess", relativeUnderDataAccess);
            if (!File.Exists(path))
                throw new FileNotFoundException(path);
            return File.ReadAllText(path);
        }

        public static string ReadEntitiesFile(string relativeUnderEntities)
        {
            var path = Path.Combine(FindRepoRoot(), "AlahiaPos.Entities", relativeUnderEntities);
            if (!File.Exists(path))
                throw new FileNotFoundException(path);
            return File.ReadAllText(path);
        }

        public static string? FindFrontendAppConfig()
        {
            return FindFrontendFile(Path.Combine("src", "app", "servicios", "app-config.service.ts"));
        }

        public static string? FindFrontendFile(string relativeUnderFrontend)
        {
            var sibling = Path.GetFullPath(Path.Combine(FindRepoRoot(), "..", "CLoudAlahiaPos",
                relativeUnderFrontend));
            return File.Exists(sibling) ? sibling : null;
        }

        public static string? TryDevConnectionString()
        {
            var env = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
            if (EsDev(env))
                return env;

            foreach (var rel in new[]
            {
                Path.Combine("AlahiaPosApi", "appsettings.json"),
                Path.Combine("artifacts", "pedidos-api", "appsettings.json")
            })
            {
                var path = Path.Combine(FindRepoRoot(), rel);
                if (!File.Exists(path))
                    continue;
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (!doc.RootElement.TryGetProperty("ConnectionStrings", out var css))
                    continue;
                if (!css.TryGetProperty("Default", out var def))
                    continue;
                var cs = def.GetString();
                if (EsDev(cs))
                    return cs;
            }

            return null;
        }

        public static AlahiaPosContext? TryOpenDev()
        {
            var cs = TryDevConnectionString();
            if (cs == null)
                return null;
            var options = new DbContextOptionsBuilder<AlahiaPosContext>()
                .UseSqlServer(cs)
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .Options;
            return new AlahiaPosContext(options);
        }

        private static bool EsDev(string? cs)
        {
            if (string.IsNullOrWhiteSpace(cs))
                return false;
            if (cs.Contains("AlahiaPos_Prod", StringComparison.OrdinalIgnoreCase))
                return false;
            return cs.Contains("AlahiaPos_Dev", StringComparison.OrdinalIgnoreCase);
        }
    }
}
