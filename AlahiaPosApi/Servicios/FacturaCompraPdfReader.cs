using System.Text;
using System.Text.RegularExpressions;
using AlahiaPos.Entities.Dto;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace AlahiaPosApi.Servicios
{
    /// <summary>
    /// Prepara un PDF de factura de compra: texto (e-CF digital) y/o imágenes embebidas (escaneo).
    /// </summary>
    public static class FacturaCompraPdfReader
    {
        private const int MaxPaginas = 4;
        private const int MinImagenBytes = 20 * 1024;

        public static FacturaCompraInterpretarRequest Preparar(
            int idEmpresa,
            int idUsuario,
            byte[] pdfBytes)
        {
            if (pdfBytes == null || pdfBytes.Length < 8)
                throw new InvalidOperationException("El PDF está vacío o no se pudo leer.");

            try
            {
                using var reader = new PdfReader(pdfBytes);
                if (reader.IsEncrypted())
                {
                    throw new InvalidOperationException(
                        "Este PDF está protegido. Guárdelo sin contraseña o adjunte una foto de la factura.");
                }

                var max = Math.Min(reader.NumberOfPages, MaxPaginas);
                var sb = new StringBuilder();
                for (var i = 1; i <= max; i++)
                {
                    try
                    {
                        sb.AppendLine(PdfTextExtractor.GetTextFromPage(reader, i) ?? "");
                    }
                    catch
                    {
                        // Página sin texto extraíble (escaneo).
                    }
                }

                var texto = Regex.Replace(sb.ToString(), @"[ \t]+\n", "\n").Trim();
                var imagenes = ExtraerImagenes(reader, max);
                var textoRico = EsTextoRico(texto);
                var scan = imagenes.Count > 0 && imagenes[0].Bytes.Length >= 80 * 1024;

                var req = new FacturaCompraInterpretarRequest
                {
                    IdEmpresa = idEmpresa,
                    IdUsuario = idUsuario
                };

                if (textoRico && !scan)
                {
                    req.TextoDocumento = texto;
                    return req;
                }

                if (imagenes.Count > 0)
                {
                    req.Paginas = imagenes;
                    if (textoRico)
                        req.TextoDocumento = texto;
                    return req;
                }

                if (textoRico)
                {
                    req.TextoDocumento = texto;
                    return req;
                }

                throw new InvalidOperationException(
                    "Este PDF no tiene texto ni imagen legible. Adjunte una foto JPG/PNG o un PDF del comprobante.");
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new InvalidOperationException(
                    "No se pudo leer este PDF. Pruebe guardarlo sin protección o adjunte una foto de la factura.");
            }
        }

        private static bool EsTextoRico(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto) || texto.Length < 60)
                return false;

            var alnum = texto.Count(char.IsLetterOrDigit);
            if (alnum < 50)
                return false;

            var upper = texto.ToUpperInvariant();
            if (upper.Contains("ITBIS") || upper.Contains("NCF") || upper.Contains("RNC")
                || upper.Contains("FACTURA") || upper.Contains("COMPROBANTE"))
                return true;

            return Regex.IsMatch(texto, @"\d{8,}");
        }

        private static List<FacturaCompraInterpretarPagina> ExtraerImagenes(PdfReader reader, int maxPaginas)
        {
            var encontradas = new List<FacturaCompraInterpretarPagina>();
            for (var i = 1; i <= maxPaginas; i++)
            {
                var page = reader.GetPageN(i);
                ExtraerDesdeRecursos(page?.GetAsDict(PdfName.RESOURCES), encontradas, 0);
            }

            return encontradas
                .Where(p => p.Bytes.Length >= MinImagenBytes)
                .OrderByDescending(p => p.Bytes.Length)
                .Take(3)
                .ToList();
        }

        private static void ExtraerDesdeRecursos(
            PdfDictionary? resources,
            List<FacturaCompraInterpretarPagina> sink,
            int depth)
        {
            if (resources == null || depth > 4)
                return;

            var xObject = resources.GetAsDict(PdfName.XOBJECT);
            if (xObject == null)
                return;

            foreach (var key in xObject.Keys)
            {
                var obj = xObject.GetDirectObject(key);
                if (obj is not PRStream stream)
                    continue;

                var subtype = stream.GetAsName(PdfName.SUBTYPE);
                if (PdfName.IMAGE.Equals(subtype))
                {
                    try
                    {
                        var img = new PdfImageObject(stream);
                        var bytes = img.GetImageAsBytes();
                        if (bytes == null || bytes.Length < MinImagenBytes)
                            continue;

                        var tipo = (img.GetFileType() ?? "").ToLowerInvariant();
                        var mime = tipo switch
                        {
                            "png" => "image/png",
                            "gif" => "image/gif",
                            "jp2" => "image/jpeg",
                            _ => "image/jpeg"
                        };
                        sink.Add(new FacturaCompraInterpretarPagina { Bytes = bytes, Mime = mime });
                    }
                    catch
                    {
                        // CCITT / imagen no rasterizable.
                    }
                }
                else if (PdfName.FORM.Equals(subtype))
                {
                    ExtraerDesdeRecursos(stream.GetAsDict(PdfName.RESOURCES), sink, depth + 1);
                }
            }
        }
    }
}
