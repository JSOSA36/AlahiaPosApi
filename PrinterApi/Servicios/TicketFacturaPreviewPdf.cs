using System.Linq;
using PrinterApi.Dto;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TicketFechaHora = AlahiaPos.Entities.Dto.TicketFechaHora;

namespace PrinterApi.Servicios;

/// <summary>
/// Vista previa PDF estilo ticket 80mm (sin impresora física).
/// Incluye bloque e-CF DGII + QR. Nunca muestra TrackId.
/// </summary>
public static class TicketFacturaPreviewPdf
{
    static TicketFacturaPreviewPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Build(TicketFacturaClienteDto factura)
    {
        byte[]? qrPng = null;
        var qrContent = ResolveQrContent(factura);
        if (!string.IsNullOrWhiteSpace(qrContent))
            qrPng = BuildQrPng(qrContent);

        // ~80mm de ancho a 72 dpi ≈ 226 pt; usamos 226 para simular térmica.
        const float pageWidth = 226f;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(pageWidth, PageSizes.A4.Height);
                page.MarginHorizontal(10);
                page.MarginVertical(12);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily(Fonts.CourierNew));

                page.Content().Column(col =>
                {
                    col.Spacing(2);

                    CenterBold(col, factura.NombreEmpresa ?? "", 11);
                    Line(col, factura.DireccionEmpresa);
                    if (!string.IsNullOrWhiteSpace(factura.RncEmpresa))
                        Line(col, $"RNC: {factura.RncEmpresa}");
                    Line(col, $"Tel: {factura.TelefonoEmpresa}");
                    Sep(col);

                    CenterBold(col, TituloEcf(factura), 9);
                    Sep(col);

                    Line(col,
                        !string.IsNullOrWhiteSpace(factura.NumeroDocumento)
                            ? $"Documento: {factura.NumeroDocumento}"
                            : $"Factura : {factura.NumeroFactura}");
                    Line(col, $"Fecha   : {factura.Fecha:dd/MM/yyyy} {factura.Hora}");
                    Line(col, $"Cliente : {factura.Cliente}");
                    if (!string.IsNullOrWhiteSpace(factura.RncCliente))
                        Line(col, $"RNC/Ced : {factura.RncCliente}");

                    if (!string.IsNullOrWhiteSpace(factura.NCF))
                    {
                        Line(col,
                            factura.EsComprobanteElectronico
                                ? $"e-NCF   : {factura.NCF}"
                                : $"NCF     : {factura.NCF}");
                    }

                    AppendTipoYFormaPagoPdf(col, factura);

                    Sep(col);
                    Line(col, "CANT   DESCRIPCION");

                    foreach (var det in factura.Detalles ?? [])
                    {
                        col.Item().Text(t =>
                        {
                            t.Span($"{det.Cantidad}   {det.Descripcion}").Bold();
                        });
                        Line(col, $"       RD$ {det.Precio:N2}");
                    }

                    Sep(col);

                    if (factura.SubTotal > 0)
                        Line(col, $"SubTotal RD$ {factura.SubTotal:N2}");
                    if (factura.TotalDescuento > 0)
                        Line(col, $"Desc.    RD$ {factura.TotalDescuento:N2}");
                    if (factura.TotalItbis > 0)
                        Line(col, $"ITBIS    RD$ {factura.TotalItbis:N2}");

                    CenterBold(col, $"TOTAL RD$ {factura.Total:N2}", 12);

                    if (factura.Pendiente > 0.02m)
                    {
                        Line(col, $"PAGADO   RD$ {factura.Pagado:N2}");
                        Line(col, $"PENDIENTE RD$ {factura.Pendiente:N2}");
                    }

                    Sep(col);

                    // Bloque e-CF (sin TrackId)
                    if (factura.EsComprobanteElectronico
                        || !string.IsNullOrWhiteSpace(factura.SecurityCode)
                        || !string.IsNullOrWhiteSpace(factura.UrlQR))
                    {
                        CenterBold(col, "DATOS DGII e-CF", 9);

                        if (!string.IsNullOrWhiteSpace(factura.TipoECF))
                            Line(col, $"Tipo e-CF: {factura.TipoECF}");

                        if (!string.IsNullOrWhiteSpace(factura.SecurityCode))
                            Line(col, $"Cod.Seguridad: {factura.SecurityCode}");

                        var fechaFirma = TicketFechaHora.ParaImpresion(
                            factura.FechaFirma,
                            factura.FechaEmisionEcf,
                            factura.Fecha,
                            factura.Hora);
                        if (fechaFirma.HasValue)
                            Line(col, $"F.Firma : {fechaFirma:dd/MM/yyyy HH:mm}");

                        if (!string.IsNullOrWhiteSpace(factura.EstadoDgii)
                            && factura.EstadoDgii.Contains("Acept", StringComparison.OrdinalIgnoreCase))
                        {
                            Line(col, $"Estado  : {factura.EstadoDgii}");
                        }

                        if (qrPng != null)
                        {
                            col.Item().AlignCenter().Text("Escanee el codigo QR").FontSize(8);
                            col.Item().AlignCenter().Width(120).Image(qrPng).FitWidth();
                        }

                        Sep(col);
                    }

                    CenterBold(col, "GRACIAS POR PREFERIRNOS", 8);
                    col.Item().PaddingTop(8).AlignCenter()
                        .Text("[PREVIEW — no enviado a impresora]")
                        .FontSize(7)
                        .FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf();
    }

    private static string TituloEcf(TicketFacturaClienteDto factura)
    {
        if (!factura.EsComprobanteElectronico)
            return "FACTURA CLIENTE";

        var tipo = (factura.TipoECF ?? "").Trim();
        if (tipo.Length == 0 && !string.IsNullOrWhiteSpace(factura.NCF) && factura.NCF.Length >= 3
            && factura.NCF.StartsWith("E", StringComparison.OrdinalIgnoreCase))
            tipo = factura.NCF.Substring(1, 2);
        if (tipo.StartsWith("E", StringComparison.OrdinalIgnoreCase) && tipo.Length >= 3)
            tipo = tipo.Substring(1, 2);
        if (tipo.Length > 2)
            tipo = new string(tipo.Where(char.IsDigit).Take(2).ToArray());

        return tipo switch
        {
            "31" => "FACTURA CREDITO FISCAL e-CF",
            "32" => "FACTURA DE CONSUMO e-CF",
            "33" => "NOTA DE DEBITO e-CF",
            "34" => "NOTA DE CREDITO e-CF",
            _ => "COMPROBANTE FISCAL ELECTRONICO"
        };
    }

    private static string? ResolveQrContent(TicketFacturaClienteDto factura)
    {
        var url = factura.UrlQR?.Trim();
        if (!string.IsNullOrWhiteSpace(url)
            && (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
            return url;

        if (string.IsNullOrWhiteSpace(factura.SecurityCode)
            || string.IsNullOrWhiteSpace(factura.NCF)
            || string.IsNullOrWhiteSpace(factura.RncEmpresa))
            return null;

        static string Digitos(string? s) =>
            string.IsNullOrWhiteSpace(s) ? "" : new string(s.Where(char.IsDigit).ToArray());

        var firma = factura.FechaFirma ?? factura.FechaEmisionEcf ?? factura.Fecha;
        var emision = factura.FechaEmisionEcf ?? factura.Fecha;
        var qs = string.Join("&", new[]
        {
            "RncEmisor=" + Uri.EscapeDataString(Digitos(factura.RncEmpresa)),
            "RncComprador=" + Uri.EscapeDataString(Digitos(factura.RncCliente)),
            "ENCF=" + Uri.EscapeDataString(factura.NCF.Trim()),
            "FechaEmision=" + Uri.EscapeDataString(emision.ToString("dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture)),
            "MontoTotal=" + Uri.EscapeDataString(factura.Total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)),
            "FechaFirma=" + Uri.EscapeDataString(firma.ToString("dd-MM-yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)),
            "CodigoSeguridad=" + Uri.EscapeDataString(factura.SecurityCode.Trim())
        });
        return "https://ecf.dgii.gov.do/ecf/ConsultaTimbre?" + qs;
    }

    private static byte[] BuildQrPng(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(5);
    }

    private static void AppendTipoYFormaPagoPdf(ColumnDescriptor col, TicketFacturaClienteDto factura)
    {
        var pagos = (factura.Pagos ?? [])
            .Where(p => p != null && p.Monto > 0 && !string.IsNullOrWhiteSpace(p.Metodo))
            .ToList();

        var formaPago = (factura.FormaPago ?? "").Trim();
        if (string.IsNullOrWhiteSpace(formaPago) && pagos.Count == 1)
            formaPago = pagos[0].Metodo;
        else if (string.IsNullOrWhiteSpace(formaPago) && pagos.Count > 1)
            formaPago = "Mixto";

        if (!string.IsNullOrWhiteSpace(factura.TipoFactura))
            Line(col, $"Tipo     : {factura.TipoFactura}");

        if (!string.IsNullOrWhiteSpace(formaPago))
            Line(col, $"Forma Pago: {formaPago}");
    }

    private static void Sep(ColumnDescriptor col) =>
        col.Item().Text("--------------------------------").FontSize(8);

    private static void Line(ColumnDescriptor col, string? text) =>
        col.Item().Text(text ?? "").FontSize(8);

    private static void CenterBold(ColumnDescriptor col, string text, float size) =>
        col.Item().AlignCenter().Text(text).Bold().FontSize(size);
}
