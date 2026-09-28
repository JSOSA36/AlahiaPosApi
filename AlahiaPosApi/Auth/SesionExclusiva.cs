using System;

namespace AlahiaPosApi.Auth
{
    /// <summary>
    /// Sesión exclusiva por dispositivo. Desactivada: bloqueaba el login
    /// (token huérfano, otro navegador, localStorage limpio) y no se podía salir.
    /// </summary>
    public static class SesionExclusiva
    {
        public const string MensajeOtroDispositivo =
            "Este usuario ya tiene una sesión activa en otro equipo. Cierre esa sesión para entrar aquí.";

        public static bool TieneSesionEnOtroDispositivo(
            string? tokenActual,
            string? dispositivoActual,
            string? deviceIdNuevo)
        {
            // Desactivado por ahora: no bloquear login.
            return false;
        }
    }
}
