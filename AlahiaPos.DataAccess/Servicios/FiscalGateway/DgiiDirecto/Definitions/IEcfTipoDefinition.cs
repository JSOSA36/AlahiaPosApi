using System.Xml.Linq;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto.Definitions
{
    /// <summary>
    /// Definición funcional/técnica de un tipo e-CF.
    /// Centraliza XSD, reglas DGII y conocimiento acumulado en pruebas.
    /// </summary>
    public interface IEcfTipoDefinition
    {
        int TipoeCF { get; }
        string Codigo { get; }
        string Nombre { get; }
        string XsdArchivo { get; }

        /// <summary>Notas humanas: hallazgos de prueba, quirks DGII, etc.</summary>
        IReadOnlyList<string> ConocimientoAcumulado { get; }

        /// <summary>Campos IdDoc en orden XSD.</summary>
        IReadOnlyList<EcfCampoDef> IdDoc { get; }

        /// <summary>Campos Emisor en orden XSD (subconjunto soportado).</summary>
        IReadOnlyList<EcfCampoDef> Emisor { get; }

        /// <summary>Campos Comprador en orden XSD. null = sección prohibida / no emitir.</summary>
        IReadOnlyList<EcfCampoDef>? Comprador { get; }

        /// <summary>Campos Totales en orden XSD (subconjunto soportado).</summary>
        IReadOnlyList<EcfCampoDef> Totales { get; }

        /// <summary>Campos Item en orden XSD (subconjunto soportado).</summary>
        IReadOnlyList<EcfCampoDef> Item { get; }

        /// <summary>null = sección prohibida; lista = campos de InformacionReferencia.</summary>
        IReadOnlyList<EcfCampoDef>? InformacionReferencia { get; }

        bool RequiereInformacionReferencia { get; }
        bool PermiteDescuentosORecargos { get; }

        /// <summary>Validaciones de negocio / dependencias entre campos.</summary>
        void Validar(EcfBuildContext ctx);

        /// <summary>Hook opcional post-armado (ajustes excepcionales).</summary>
        void PostProcesar(EcfBuildContext ctx, XElement rootEcf) { }
    }
}
