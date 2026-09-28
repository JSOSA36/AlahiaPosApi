using System;

namespace AlahiaPosApi.Auth
{
    /// <summary>
    /// La acción solo procede si este PC tiene asiento POS (huella en header X-Pos-Device-Id).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequiereTerminalPosAttribute : Attribute
    {
    }
}
