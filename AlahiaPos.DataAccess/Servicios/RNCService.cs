using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;

namespace Alahia_Pos.Services
{
    public class RNCService : IRNCService
    {
        public async Task<ClienteDgiiDto?> ConsultarAsync(string rncOrCedula)
        {
            try
            {
                var url = "https://dgii.gov.do/wsMovilDGII/WSMovilDGII.asmx?op=GetContribuyentes";

                XmlDocument soapEnvelopeXml = CreateSoapEnvelope(rncOrCedula);
                HttpWebRequest webRequest = CreateWebRequest(url);

                await InsertSoapEnvelopeIntoWebRequestAsync(soapEnvelopeXml, webRequest);

                using WebResponse webResponse = await webRequest.GetResponseAsync();

                using StreamReader rd = new StreamReader(webResponse.GetResponseStream()); // 👈 igual que tu código

                var soapResult = await rd.ReadToEndAsync();

                var dgii = DeserializeInnerSoapObject(soapResult);

                if (dgii == null || string.IsNullOrEmpty(dgii.RGE_RUC))
                    return null;

                return Mapear(dgii);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        private HttpWebRequest CreateWebRequest(string url)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);

            webRequest.ContentType = "text/xml; charset=utf-8";
            webRequest.Method = "POST";

            // 🔥 CLAVE
            webRequest.Headers.Add(
                "SOAPAction",
                "http://dgii.gov.do/GetContribuyentes"
            );

            return webRequest;
        }

        private XmlDocument CreateSoapEnvelope(string rncOrCedula)
        {
            XmlDocument soapEnvelopeDocument = new XmlDocument();
            soapEnvelopeDocument.LoadXml(@"<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
                xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
             <soap:Body>
               <GetContribuyentes xmlns=""http://dgii.gov.do/"">
            <value>" + rncOrCedula + @"</value>
            <patronBusqueda>0</patronBusqueda>
            <inicioFilas>0</inicioFilas>
            <filaFilas>0</filaFilas>
            <IMEI>?</IMEI>
            </GetContribuyentes>
             </soap:Body>
           </soap:Envelope>");
            return soapEnvelopeDocument;
        }

        private async Task InsertSoapEnvelopeIntoWebRequestAsync(XmlDocument soapEnvelopeXml, HttpWebRequest webRequest)
        {
            using Stream stream = await webRequest.GetRequestStreamAsync();
            soapEnvelopeXml.Save(stream);
        }

        private clsRNCResponse? DeserializeInnerSoapObject(string soapResponse)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(soapResponse);

            var soapBody = xmlDocument.GetElementsByTagName("soap:Body")[0];

            return Newtonsoft.Json.JsonConvert.DeserializeObject<clsRNCResponse>(soapBody.InnerText);
        }

        private ClienteDgiiDto Mapear(clsRNCResponse r)
        {
            return new ClienteDgiiDto
            {
                RNC = r.RGE_RUC,
                Nombre = r.RGE_NOMBRE,
                NombreComercial = string.IsNullOrWhiteSpace(r.NOMBRE_COMERCIAL)
                    ? r.RGE_NOMBRE
                    : r.NOMBRE_COMERCIAL,
                Estado = r.ESTATUS,
                Regimen = r.REGIMEN_PAGOS
            };
        }
    }
}