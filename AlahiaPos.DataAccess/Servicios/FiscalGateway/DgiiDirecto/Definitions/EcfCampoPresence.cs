namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto.Definitions
{
    public enum EcfCampoPresence
    {
        /// <summary>Debe existir en el XML.</summary>
        Obligatorio = 1,
        /// <summary>Se incluye solo si hay valor usable.</summary>
        Opcional = 2,
        /// <summary>No debe aparecer; si llega en el DTO se ignora o se rechaza.</summary>
        Prohibido = 3
    }

    public enum EcfProhibidoModo
    {
        /// <summary>Si viene en el DTO se omite silenciosamente.</summary>
        Omitir = 1,
        /// <summary>Si viene en el DTO se rechaza en validación.</summary>
        Rechazar = 2
    }
}
