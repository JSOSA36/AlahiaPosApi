using System.Globalization;
using System.Xml.Linq;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;

namespace AlahiaPos.DataAccess.Servicios
{
    public class RfceBuilderServices : IRfceBuilder
    {
        public string Build(RfceRow row)
        {
            XNamespace ns = "http://www.dgii.gov.do/ecf";

            var xml = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement(ns + "RFCE",
                    new XElement(ns + "Encabezado",

                        new XElement(ns + "Version", "1.0"),

                        new XElement(ns + "IdDoc",
                            new XElement(ns + "TipoeCF", "32"),
                            new XElement(ns + "eNCF", row.ENCF),
                            new XElement(ns + "TipoIngresos", row.TipoIngresos),
                            new XElement(ns + "TipoPago", row.TipoPago)
                        ),

                        new XElement(ns + "Emisor",
                            new XElement(ns + "RNCEmisor", row.RNCEmisor),
                            new XElement(ns + "RazonSocialEmisor", row.RazonSocialEmisor),
                            new XElement(ns + "FechaEmision",
                                row.FechaEmision.ToString("dd-MM-yyyy"))
                        ),

                        ConstruirComprador(ns, row),

                        new XElement(ns + "Totales",
                            new XElement(ns + "MontoGravadoTotal",
                                row.MontoGravadoTotal.ToString("0.00", CultureInfo.InvariantCulture)),

                            new XElement(ns + "TotalITBIS",
                                row.TotalITBIS.ToString("0.00", CultureInfo.InvariantCulture)),

                            new XElement(ns + "MontoTotal",
                                row.MontoTotal.ToString("0.00", CultureInfo.InvariantCulture))
                        )
                    )
                )
            );

            return xml.ToString(SaveOptions.DisableFormatting);
        }

        private XElement? ConstruirComprador(XNamespace ns, RfceRow row)
        {
            if (string.IsNullOrWhiteSpace(row.RNCComprador))
                return null;

            return new XElement(ns + "Comprador",
                new XElement(ns + "RNCComprador", row.RNCComprador),
                new XElement(ns + "RazonSocialComprador", row.RazonSocialComprador)
            );
        }
    }
}