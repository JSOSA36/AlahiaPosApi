using System.Globalization;
using AlahiaPos.Entities.Dto.Fiscal;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace AlahiaPos.DataAccess.Servicios.FacturacionElectronica
{
    /// <summary>
    /// RI CerteCF paso 5: mismas etiquetas que ya aprobaron (Razón Social / Nombre Comercial / RNC).
    /// Bajo el QR: Código de Seguridad y Fecha Firma, como pidió DGII.
    /// </summary>
    public static class CertecfRiPdf
    {
        private static readonly BaseColor HeadBg = new BaseColor(0xE8, 0xE8, 0xE8);

        public static byte[] Crear(CertecfRiEmisor emisor, FiscalDocumentoElectronico doc, string? qrUrl)
            => Crear(emisor, doc, qrUrl, null, null);

        public static byte[] Crear(
            CertecfRiEmisor emisor,
            FiscalDocumentoElectronico doc,
            string? qrUrl,
            string? codigoSeguridad,
            string? fechaFirma)
        {
            var e = doc.Encabezado;
            var es = CultureInfo.GetCultureInfo("es-DO");
            var (codigoQr, fechaQr) = CertecfArtefactos.TimbreDesdeQr(qrUrl);
            var codigo = string.IsNullOrWhiteSpace(codigoSeguridad) ? codigoQr : codigoSeguridad;
            var firma = string.IsNullOrWhiteSpace(fechaFirma) ? fechaQr : fechaFirma;

            using var ms = new MemoryStream();
            var pdf = new Document(PageSize.A4, 42f, 42f, 36f, 36f);
            var writer = PdfWriter.GetInstance(pdf, ms);
            writer.CloseStream = false;
            pdf.Open();

            var titulo = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 13);
            var lab = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.DARK_GRAY);
            var val = FontFactory.GetFont(FontFactory.HELVETICA, 9);
            var valBold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9);
            var th = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8);
            var td = FontFactory.GetFont(FontFactory.HELVETICA, 8);
            var tot = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
            var qrLab = FontFactory.GetFont(FontFactory.HELVETICA, 9);

            pdf.Add(new Paragraph(CertecfArtefactos.TituloRi(e.TipoEcf), titulo)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 4f
            });
            pdf.Add(new Paragraph("e-NCF: " + (e.Encf ?? ""), valBold)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 2f
            });
            if (CertecfArtefactos.LlevaFechaVencimientoRi(e.TipoEcf) && e.FechaVencimientoSecuencia is DateTime fv && fv != default)
            {
                pdf.Add(new Paragraph(
                    "Fecha Vencimiento: " + fv.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
                    val)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 10f
                });
            }
            else
            {
                pdf.Add(new Paragraph(" ") { SpacingAfter = 8f });
            }

            var meta = new PdfPTable(2) { WidthPercentage = 100 };
            meta.SetWidths(new float[] { 28, 72 });
            Fila(meta, "Razón Social", emisor.RazonSocial, lab, valBold);
            Fila(meta, "Nombre Comercial", emisor.NombreComercial, lab, val);
            Fila(meta, "RNC", emisor.Rnc, lab, val);
            Fila(meta, "Dirección", emisor.Direccion, lab, val);
            if (!string.IsNullOrWhiteSpace(emisor.Telefono))
                Fila(meta, "Teléfono", emisor.Telefono, lab, val);
            Fila(meta, "Fecha de emisión", e.FechaEmision.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture), lab, val);
            Fila(meta, "Razón Social comprador", e.RazonSocialComprador ?? "", lab, val);
            Fila(meta, "RNC comprador", e.RncComprador ?? "", lab, val);
            pdf.Add(meta);

            var items = new PdfPTable(4) { WidthPercentage = 100, SpacingBefore = 12f };
            items.SetWidths(new float[] { 46, 14, 20, 20 });
            CeldaHead(items, "Item", th);
            CeldaHead(items, "Cant.", th);
            CeldaHead(items, "Precio", th);
            CeldaHead(items, "Monto", th);
            foreach (var l in doc.Lineas)
            {
                Celda(items, l.NombreItem ?? "", td, Element.ALIGN_LEFT);
                Celda(items, l.Cantidad.ToString("0.##", CultureInfo.InvariantCulture), td, Element.ALIGN_CENTER);
                Celda(items, l.PrecioUnitario.ToString("N2", es), td, Element.ALIGN_RIGHT);
                Celda(items, l.MontoItem.ToString("N2", es), td, Element.ALIGN_RIGHT);
            }
            pdf.Add(items);

            if (e.MontoNoFacturable is decimal noFacturable && noFacturable != 0)
            {
                pdf.Add(new Paragraph("Monto no facturable: " + noFacturable.ToString("N2", es), tot)
                {
                    Alignment = Element.ALIGN_RIGHT,
                    SpacingBefore = 10f
                });
            }
            pdf.Add(new Paragraph("ITBIS: " + e.TotalItbis.ToString("N2", es), tot)
            {
                Alignment = Element.ALIGN_RIGHT,
                SpacingBefore = e.MontoNoFacturable is decimal nf && nf != 0 ? 0f : 10f
            });
            pdf.Add(new Paragraph("Monto total: " + e.MontoTotal.ToString("N2", es), tot)
            {
                Alignment = Element.ALIGN_RIGHT,
                SpacingAfter = 14f
            });

            pdf.Add(new Paragraph("ConsultaTimbre", tot) { Alignment = Element.ALIGN_CENTER });
            if (!string.IsNullOrWhiteSpace(qrUrl))
            {
                var b64 = CertecfArtefactos.QrPngBase64(qrUrl);
                if (!string.IsNullOrWhiteSpace(b64))
                {
                    var img = Image.GetInstance(Convert.FromBase64String(b64));
                    img.ScaleAbsolute(120f, 120f);
                    img.Alignment = Element.ALIGN_CENTER;
                    pdf.Add(img);
                }
            }
            pdf.Add(new Paragraph("Código de Seguridad: " + (codigo ?? ""), qrLab)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingBefore = 4f
            });
            pdf.Add(new Paragraph("Fecha Firma: " + (firma ?? ""), qrLab)
            {
                Alignment = Element.ALIGN_CENTER
            });

            pdf.Close();
            return ms.ToArray();
        }

        private static void Fila(PdfPTable t, string etiqueta, string? valor, Font fl, Font fv)
        {
            t.AddCell(new PdfPCell(new Phrase(etiqueta, fl))
            {
                Border = Rectangle.NO_BORDER,
                Padding = 3f
            });
            t.AddCell(new PdfPCell(new Phrase(valor ?? "", fv))
            {
                Border = Rectangle.NO_BORDER,
                Padding = 3f
            });
        }

        private static void CeldaHead(PdfPTable t, string texto, Font f)
            => t.AddCell(new PdfPCell(new Phrase(texto, f))
            {
                BackgroundColor = HeadBg,
                Padding = 4f,
                HorizontalAlignment = Element.ALIGN_CENTER
            });

        private static void Celda(PdfPTable t, string texto, Font f, int align)
            => t.AddCell(new PdfPCell(new Phrase(texto, f)) { Padding = 4f, HorizontalAlignment = align });
    }
}
