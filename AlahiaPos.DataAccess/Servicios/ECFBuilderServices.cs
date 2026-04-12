using System.Globalization;
using System.Xml.Linq;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;

namespace AlahiaPos.DataAccess.Servicios
{
    public class ECFBuilder : IECFBuilder
    {
       
      

        private XElement ConstruirDetalle(XNamespace ns, List<DtoDetalleItem> detalles)
        {
            return new XElement(ns + "Detalle",
                detalles.Select((d, index) =>
                    new XElement(ns + "Item",

                        new XElement(ns + "NumeroLinea", index + 1), // ✅ agregado

                        new XElement(ns + "Descripcion", d.Descripcion),

                        new XElement(ns + "Cantidad",
                            d.Cantidad.ToString("0.00", CultureInfo.InvariantCulture)),

                        new XElement(ns + "PrecioUnitario",
                            d.PrecioUnitario.ToString("0.00", CultureInfo.InvariantCulture)),

                        new XElement(ns + "MontoItem",
                            d.SubTotal.ToString("0.00", CultureInfo.InvariantCulture))
                    )
                )
            );
        }

      
    }
}