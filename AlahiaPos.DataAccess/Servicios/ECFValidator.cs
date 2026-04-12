using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

namespace AlahiaPos.DataAccess.Servicios
{

    public class ECFValidator : IECFValidator
    {
       
            public void Validar(string xmlString, string rutaXsd)
            {
            XmlSchemaSet schemas = new XmlSchemaSet();
            schemas.Add(null, rutaXsd);

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlString);

            xmlDoc.Schemas.Add(schemas);

            string errores = string.Empty;

            xmlDoc.Validate((sender, e) =>
            {
                errores += $"Línea: {e.Exception?.LineNumber} - {e.Message}\n";
            });

            if (!string.IsNullOrWhiteSpace(errores))
            {
                throw new Exception("XML inválido según XSD:\n" + errores);
            }
        }
    }
 }

