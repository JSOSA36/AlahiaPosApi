using System;

namespace AlahiaPosApi.Auth
{
    /// <summary>
    /// El body puede traer un IdSucursal distinto al de sesión (cambio de sucursal).
    /// El servicio valida acceso; el guard de sucursal no debe pisar el destino.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class PermitirCambioSucursalAttribute : Attribute
    {
    }
}
