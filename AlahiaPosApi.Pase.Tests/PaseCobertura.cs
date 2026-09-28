namespace AlahiaPosApi.Pase.Tests
{
    /// <summary>
    /// Cada modulo critico del ERP debe tener una clase de prueba en este proyecto.
    /// Si se agrega un area de pase y no hay test, CatalogoCoberturaTests falla.
    /// </summary>
    public static class PaseCobertura
    {
        public static readonly string[] ClasesObligatorias =
        {
            nameof(ErpVivoAuthTests),
            nameof(IdUsuarioCobroResolverTests),
            nameof(SucursalConsultaContratoTests),
            nameof(FrontendVivoContratoTests),
            nameof(ProcesarFacturaContratoTests),
            nameof(TenantSucursalGuardTests),
            nameof(ExtractoBancoContratoTests),
            nameof(SoloEcfContratoTests),
            nameof(NoRegalarModulosTests),
            nameof(CajaSucursalContratoTests),
            nameof(InventarioSucursalContratoTests),
            nameof(LoginContratoTests),
            nameof(ClientesComprasCitasContratoTests),
            nameof(NominaContabilidadContratoTests),
            nameof(ErpDevSmokeTests),
            nameof(AnularEliminarOrdenContratoTests)
        };
    }
}
