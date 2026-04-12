namespace Alahia.eCF.Api.Interfaces
{
    public interface IXmlFirmaService
    {
        string FirmarXml(string xmlString, string rutaCertificado, string passwordCertificado);
    }
}
