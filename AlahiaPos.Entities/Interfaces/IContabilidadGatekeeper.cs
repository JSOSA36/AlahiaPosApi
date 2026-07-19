namespace AlahiaPos.Entities.Interfaces
{
    public class ContabilidadGatekeeperStatus
    {
        public bool ModuloContratado { get; set; }
        public bool IntegracionAutomaticaActiva { get; set; }
        public bool DebeProcesarEventos => ModuloContratado && IntegracionAutomaticaActiva;
    }

    public interface IContabilidadGatekeeper
    {
        Task<bool> ModuloContratadoAsync(int idEmpresa);
        Task<bool> IntegracionAutomaticaActivaAsync(int idEmpresa);
        Task<ContabilidadGatekeeperStatus> ObtenerEstadoAsync(int idEmpresa);
    }
}
