using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface INotification
    {
        /// <summary>
        /// Enviar notificación a todos los dispositivos que tengan un TAG específico.
        /// </summary>
        Task<bool> EnviarNotificacionPorTagAsync(string tagKey, string tagValue, string titulo, string cuerpo);

        /// <summary>
        /// Enviar notificación a una empresa usando el tag empresa_id.
        /// </summary>
        Task<bool> EnviarPorEmpresaAsync(int idEmpresa, string titulo, string cuerpo);

        /// <summary>
        /// Enviar notificación a TODOS los usuarios.
        /// </summary>
        Task<bool> EnviarATodosAsync(string titulo, string cuerpo);
    }
}
