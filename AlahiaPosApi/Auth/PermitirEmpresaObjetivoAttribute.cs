using System;

namespace AlahiaPosApi.Auth
{
    /// <summary>
    /// Permite IdEmpresa distinto al de la sesión solo si el usuario es MacroBits
    /// (Empresas.EsEmpresaSistema). Para alta/cobros/admin sobre clientes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class PermitirEmpresaObjetivoAttribute : Attribute
    {
    }
}
