namespace AlahiaPos.Entities.Interfaces
{
    /// <summary>
    /// Acceso opcional al <see cref="IFiscalGateway"/> ya registrado por DI.
    /// No selecciona proveedores: el ERP tiene un único adaptador resuelto por configuración.
    /// </summary>
    public interface IFiscalGatewayFactory
    {
        IFiscalGateway Get();

        /// <summary>BaseUrl configurado (diagnóstico). No es un código de vendor.</summary>
        string CurrentProvider { get; }
    }
}
