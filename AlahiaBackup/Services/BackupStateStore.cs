using System.Text.Json;
using AlahiaBackup.Models;

namespace AlahiaBackup.Services
{
    public sealed class BackupStateStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        private readonly string _path;

        public BackupStateStore(string rutaBackup)
        {
            var dir = Path.Combine(rutaBackup, "_state");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "estado.json");
        }

        public BackupRunState Load()
        {
            if (!File.Exists(_path))
                return new BackupRunState();

            try
            {
                var json = File.ReadAllText(_path);
                return JsonSerializer.Deserialize<BackupRunState>(json) ?? new BackupRunState();
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                return new BackupRunState();
            }
        }

        public void Save(BackupRunState state)
        {
            var json = JsonSerializer.Serialize(state, JsonOptions);
            File.WriteAllText(_path, json);
        }
    }
}
