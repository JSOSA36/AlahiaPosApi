namespace AlahiaPosApi.Auth
{
    /// <summary>
    /// Contrato del ERP vivo (erp.alahiapos.com + alahiaposapidemo).
    /// No reactivar por appsettings del IIS.
    /// </summary>
    public static class ErpVivoAuth
    {
        public const bool RequerirSesion = false;
        public const bool RequerirTerminalPos = false;
    }
}
