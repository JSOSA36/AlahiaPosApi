using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class XsdSelectorService
    {
        public static string ObtenerRutaXsd(string tipoeCF)
        {
            string basePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Resource",
                "XSD");

            return tipoeCF switch
            {
                "31" => Path.Combine(basePath, "e-CF31v1.0.xsd"),
                "32" => Path.Combine(basePath, "e-CF32v1.0.xsd"),
                "34" => Path.Combine(basePath, "e-CF34v1.0.xsd"),
                "44" => Path.Combine(basePath, "e-CF44v1.0.xsd"),
                "45" => Path.Combine(basePath, "e-CF45v1.0.xsd"),
                _ => throw new Exception($"No existe XSD para TipoeCF {tipoeCF}")
            };
        }
    }
}
