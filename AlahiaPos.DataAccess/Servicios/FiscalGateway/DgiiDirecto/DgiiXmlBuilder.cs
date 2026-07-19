using AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto.Definitions;
using AlahiaPos.Entities.Dto.Fiscal;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto
{
    /// <summary>
    /// Fachada del XML Builder. Delega al motor genérico + definiciones por tipo.
    /// No introducir lógica por TipoeCF en esta clase.
    /// </summary>
    public class DgiiXmlBuilder
    {
        public string Build(FiscalDocumentoElectronico doc, DateTime fechaHoraFirma)
            => EcfXmlEngine.Build(doc, fechaHoraFirma);

        public static string NombreArchivo(string rncEmisor, string encf)
            => $"{NormalizarRnc(rncEmisor)}{SoloAlnum(encf)}.xml";

        public static string NormalizarRnc(string? rnc)
            => EcfXmlFormat.NormalizarRnc(rnc);

        private static string SoloAlnum(string s)
            => new string((s ?? "").Where(char.IsLetterOrDigit).ToArray());
    }
}
