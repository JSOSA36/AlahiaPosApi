using System;

namespace AlahiaPosApi.Auth
{
    /// <summary>
    /// Solo usuarios de la empresa plataforma (MacroBits) pueden ejecutar la acción.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequiereEmpresaSistemaAttribute : Attribute
    {
    }
}
