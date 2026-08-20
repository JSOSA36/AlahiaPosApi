using System.Globalization;
using System.Text.Json;
using AlahiaPos.Entities.Domain;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace AlahiaPosApi.Servicios
{
    internal static class NominaReciboPdfBuilder
    {
        private static readonly BaseColor Azul = new(0x1B, 0x4F, 0x72);
        private static readonly BaseColor Texto = new(0x1D, 0x2B, 0x3A);
        private static readonly BaseColor Muted = new(0x5B, 0x6B, 0x7C);
        private static readonly BaseColor Linea = new(0xD7, 0xE3, 0xEE);
        private static readonly BaseColor Zebra = new(0xF4, 0xF8, 0xFB);

        public static byte[] Crear(Empresas empresa, NominaProceso proceso, NominaProcesoEmpleado emp)
        {
            using var ms = new MemoryStream();
            var doc = new Document(PageSize.LETTER, 42f, 42f, 46f, 42f);
            var writer = PdfWriter.GetInstance(doc, ms);
            writer.CloseStream = false;
            doc.Open();

            var title = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, Font.NORMAL, Azul);
            var h2 = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, Font.NORMAL, Azul);
            var normal = FontFactory.GetFont(FontFactory.HELVETICA, 9, Font.NORMAL, Texto);
            var muted = FontFactory.GetFont(FontFactory.HELVETICA, 8.5f, Font.NORMAL, Muted);
            var bold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, Font.NORMAL, Texto);
            var whiteBold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, Font.NORMAL, BaseColor.WHITE);

            var head = new PdfPTable(2) { WidthPercentage = 100 };
            head.SetWidths(new float[] { 62, 38 });
            head.DefaultCell.Border = Rectangle.NO_BORDER;
            var izq = new PdfPCell { Border = Rectangle.NO_BORDER };
            izq.AddElement(new Paragraph(empresa.NombreComercial ?? "Empresa", title));
            if (!string.IsNullOrWhiteSpace(empresa.RNC))
                izq.AddElement(new Paragraph("RNC: " + empresa.RNC, muted));
            if (!string.IsNullOrWhiteSpace(empresa.Direccion))
                izq.AddElement(new Paragraph(empresa.Direccion, muted));
            if (!string.IsNullOrWhiteSpace(empresa.Telefono))
                izq.AddElement(new Paragraph("Tel: " + empresa.Telefono, muted));
            var der = new PdfPCell { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT };
            der.AddElement(new Paragraph("RECIBO DE PAGO", h2) { Alignment = Element.ALIGN_RIGHT });
            der.AddElement(new Paragraph("Volante de nómina", muted) { Alignment = Element.ALIGN_RIGHT });
            der.AddElement(new Paragraph(
                $"Período: {proceso.FechaInicio:dd/MM/yyyy} — {proceso.FechaFin:dd/MM/yyyy}",
                normal)
            { Alignment = Element.ALIGN_RIGHT });
            der.AddElement(new Paragraph($"{proceso.Frecuencia} · {proceso.Estado}", muted) { Alignment = Element.ALIGN_RIGHT });
            head.AddCell(izq);
            head.AddCell(der);
            doc.Add(head);

            doc.Add(new Paragraph("Colaborador", h2) { SpacingBefore = 14, SpacingAfter = 3 });
            doc.Add(new Paragraph(emp.NombreEmpleado ?? "Colaborador", bold));

            var desglose = Desglose(emp);
            Tabla(doc, "Ingresos", desglose.Ingresos, desglose.Bruto, "Total ingresos / bruto", normal, bold);
            Tabla(doc, "Deducciones de ley (TSS / DGII)", desglose.Legales, desglose.TotalLegales, "Total deducciones de ley", normal, bold);
            if (desglose.Otros.Count > 0)
                Tabla(doc, "Otros descuentos", desglose.Otros, desglose.TotalOtros, "Total otros descuentos", normal, bold);

            var neto = new PdfPTable(2) { WidthPercentage = 100, SpacingBefore = 12 };
            neto.SetWidths(new float[] { 70, 30 });
            AddRow(neto, "Total descuentos", Moneda(desglose.TotalDescuentos), bold, Zebra, false);
            AddRow(neto, "NETO A PAGAR", Moneda(desglose.Neto), whiteBold, Azul, true);
            doc.Add(neto);
            doc.Add(new Paragraph(
                "El neto a pagar ya descuenta AFP, SFS, ISR y los demás descuentos listados.",
                muted)
            { SpacingBefore = 8 });

            var firmas = new PdfPTable(2) { WidthPercentage = 100, SpacingBefore = 36 };
            firmas.DefaultCell.Border = Rectangle.NO_BORDER;
            firmas.AddCell(CeldaSinBorde(new Paragraph("______________________________\nFirma del colaborador", muted)));
            var derFirma = CeldaSinBorde(new Paragraph("______________________________\nFirma / sello de la empresa", muted));
            derFirma.HorizontalAlignment = Element.ALIGN_RIGHT;
            firmas.AddCell(derFirma);
            doc.Add(firmas);

            doc.Close();
            return ms.ToArray();
        }

        private static void Tabla(
            Document doc,
            string titulo,
            List<(string Label, decimal Monto)> filas,
            decimal total,
            string totalLabel,
            Font normal,
            Font bold)
        {
            doc.Add(new Paragraph(titulo, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, Font.NORMAL, Azul)) { SpacingBefore = 12, SpacingAfter = 6 });
            var t = new PdfPTable(2) { WidthPercentage = 100 };
            t.SetWidths(new float[] { 70, 30 });
            AddHeader(t, "Concepto");
            AddHeader(t, "Monto");
            for (var i = 0; i < filas.Count; i++)
                AddRow(t, filas[i].Label, Moneda(filas[i].Monto), normal, i % 2 == 1 ? Zebra : BaseColor.WHITE, false);
            AddRow(t, totalLabel, Moneda(total), bold, new BaseColor(0xEA, 0xF0, 0xF6), false);
            doc.Add(t);
        }

        private static void AddHeader(PdfPTable t, string text)
        {
            t.AddCell(new PdfPCell(new Phrase(text, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8, Font.NORMAL, BaseColor.WHITE)))
            {
                BackgroundColor = Azul,
                Padding = 5,
                BorderColor = Linea,
                HorizontalAlignment = text == "Monto" ? Element.ALIGN_RIGHT : Element.ALIGN_LEFT
            });
        }

        private static void AddRow(PdfPTable t, string label, string monto, Font font, BaseColor bg, bool white)
        {
            t.AddCell(new PdfPCell(new Phrase(label, font))
            {
                BackgroundColor = bg,
                Padding = 5,
                BorderColor = Linea
            });
            t.AddCell(new PdfPCell(new Phrase(monto, font))
            {
                BackgroundColor = bg,
                Padding = 5,
                BorderColor = Linea,
                HorizontalAlignment = Element.ALIGN_RIGHT
            });
        }

        private static PdfPCell CeldaSinBorde(IElement el)
        {
            var c = new PdfPCell { Border = Rectangle.NO_BORDER };
            c.AddElement(el);
            return c;
        }

        private static string Moneda(decimal v) =>
            v.ToString("C2", CultureInfo.GetCultureInfo("es-DO"));

        internal static NominaDesglosePdf Desglose(NominaProcesoEmpleado emp)
        {
            var lineas = ParseLineas(emp.LineasJson);
            decimal Concepto(string code) => lineas
                .Where(l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase))
                .Sum(l => l.Amount);

            var ingresos = new List<(string, decimal)>
            {
                ("Sueldo del período", emp.SalarioBase),
                ("Horas extra", emp.HorasExtra),
                ("Comisiones", emp.Comisiones),
                ("Bonificaciones", emp.Bonificaciones),
                ("Beneficios del cargo", emp.OtrosIngresos)
            }.Where(x => x.Item2 != 0 || x.Item1.StartsWith("Sueldo")).ToList();

            var afp = Concepto("AFP_EMPLEADO");
            var sfs = Concepto("SFS_EMPLEADO");
            var isr = Concepto("ISR_EMPLEADO");
            var legales = new List<(string, decimal)>
            {
                ("AFP — Administradora de Fondos de Pensiones", afp),
                ("SFS — Seguro Familiar de Salud", sfs),
                ("ISR — Impuesto Sobre la Renta", isr)
            };
            var totalLegales = afp + sfs + isr;
            if (totalLegales == 0 && emp.DeduccionesLegales != 0)
            {
                legales.Add(("Deducciones de ley (detalle no desglosado)", emp.DeduccionesLegales));
                totalLegales = emp.DeduccionesLegales;
            }

            var otros = new List<(string, decimal)>
            {
                ("Descuento por asistencia / días sin goce", emp.DescuentosAsistencia),
                ("Préstamos", emp.Prestamos),
                ("Anticipos", emp.Anticipos),
                ("Otros descuentos", emp.OtrosDescuentos)
            }.Where(x => x.Item2 != 0).ToList();

            var totalOtros = otros.Sum(x => x.Item2);
            return new NominaDesglosePdf(
                ingresos,
                legales,
                otros,
                ingresos.Sum(x => x.Item2),
                totalLegales,
                totalOtros,
                totalLegales + totalOtros,
                emp.Bruto,
                emp.Neto);
        }

        private static List<(string Code, decimal Amount)> ParseLineas(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array) return new();
                var list = new List<(string, decimal)>();
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    var code = el.TryGetProperty("ConceptCode", out var c) ? c.GetString()
                        : el.TryGetProperty("conceptCode", out var c2) ? c2.GetString()
                        : "";
                    decimal amount = 0;
                    if (el.TryGetProperty("Amount", out var a) || el.TryGetProperty("amount", out a))
                        a.TryGetDecimal(out amount);
                    if (!string.IsNullOrWhiteSpace(code))
                        list.Add((code!, amount));
                }
                return list;
            }
            catch
            {
                return new();
            }
        }
    }

    internal sealed record NominaDesglosePdf(
        List<(string Label, decimal Monto)> Ingresos,
        List<(string Label, decimal Monto)> Legales,
        List<(string Label, decimal Monto)> Otros,
        decimal TotalIngresos,
        decimal TotalLegales,
        decimal TotalOtros,
        decimal TotalDescuentos,
        decimal Bruto,
        decimal Neto);
}
