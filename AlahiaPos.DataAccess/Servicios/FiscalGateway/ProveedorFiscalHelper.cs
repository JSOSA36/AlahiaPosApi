namespace AlahiaPos.DataAccess.Servicios.FiscalGateway
{
    public static class ProveedorFiscalHelper
    {
        public const string DgiiDirecto = "DGII_DIRECTO";
        public const string ProveedorExterno = "PROVEEDOR_EXTERNO";

        public static string Normalize(string? proveedor)
        {
            // El envío es siempre DGII directo. Un valor viejo de proveedor externo no se usa.
            return DgiiDirecto;
        }

        public static bool EsExterno(string? proveedor)
            => false;

        public static string Etiqueta(string? proveedor)
            => EsExterno(proveedor) ? "Proveedor externo" : "DGII directo (Alahia)";

        public static bool EsValido(string? proveedor)
        {
            var n = (proveedor ?? "").Trim().ToUpperInvariant();
            return string.IsNullOrEmpty(n)
                || n is DgiiDirecto or ProveedorExterno
                or "EXTERNO" or "PROVEEDOR" or "PEDRO" or "PG" or "THIRD_PARTY"
                or "DIRECTO" or "DGII" or "ALAHIA";
        }
    }

    public sealed class FiscalGatewayEndpoint
    {
        public string Modo { get; init; } = ProveedorFiscalHelper.DgiiDirecto;
        public string? Nombre { get; init; }
        public string BaseUrl { get; init; } = "";
        public string? ApiKey { get; init; }
        public string? Usuario { get; init; }
        public string? Password { get; init; }
        public int TimeoutSeconds { get; init; } = 30;
    }
}
