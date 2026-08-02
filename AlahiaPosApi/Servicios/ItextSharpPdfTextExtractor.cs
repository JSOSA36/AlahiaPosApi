using System.Text;
using AlahiaPos.Entities.Interfaces;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace AlahiaPosApi.Servicios
{
    /// <summary>
    /// Implementación de IPdfTextExtractor sobre iTextSharp.
    /// Vive en el proyecto API porque iTextSharp solo está referenciado aquí.
    /// </summary>
    public sealed class ItextSharpPdfTextExtractor : IPdfTextExtractor
    {
        public string ExtractText(byte[] pdfBytes)
        {
            using var reader = new PdfReader(pdfBytes);
            if (reader.IsEncrypted())
                throw new InvalidOperationException("No se admiten estados bancarios PDF cifrados.");
            var sb = new StringBuilder();
            for (var i = 1; i <= reader.NumberOfPages; i++)
            {
                sb.AppendLine(PdfTextExtractor.GetTextFromPage(reader, i));
            }
            return sb.ToString();
        }
    }
}
