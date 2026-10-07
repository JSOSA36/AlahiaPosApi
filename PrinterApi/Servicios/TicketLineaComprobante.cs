using PrinterApi.Dto;

namespace PrinterApi.Servicios;

/// <summary>
/// Línea del ticket térmico alineada con la vista previa del e-CF:
/// nombre, cantidad x precio unitario, monto sin ITBIS, e ITBIS de la línea.
/// </summary>
public static class TicketLineaComprobante
{
    public const string Encabezado = "CANT x PRECIO          MONTO";

    public readonly record struct Renglon(string Texto, bool Negrita);

    public static IReadOnlyList<Renglon> Armar(TicketFacturaClienteDetalleDto det)
    {
        var lineas = new List<Renglon>();
        var nombre = (det.Descripcion ?? "").Trim();
        if (nombre.Length == 0)
            nombre = "Item";

        foreach (var parte in Partir(nombre, 32))
            lineas.Add(new Renglon(parte, true));

        var cantidad = det.Cantidad <= 0 ? 1m : det.Cantidad;
        var baseMonto = det.Monto > 0 ? det.Monto : det.Precio;
        var unitario = det.PrecioUnitario > 0
            ? det.PrecioUnitario
            : Redondear(baseMonto / cantidad);
        var monto = det.Monto > 0 ? det.Monto : Redondear(unitario * cantidad);

        lineas.Add(new Renglon(Alinear($"{FmtCant(cantidad)} x {unitario:N2}", $"{monto:N2}"), false));
        if (det.Itbis > 0.009m)
            lineas.Add(new Renglon(Alinear("ITBIS", $"{det.Itbis:N2}"), false));

        return lineas;
    }

    private static string Alinear(string izquierda, string derecha, int ancho = 32)
    {
        izquierda ??= "";
        derecha ??= "";
        var hueco = ancho - izquierda.Length - derecha.Length;
        if (hueco < 1)
            return izquierda + " " + derecha;
        return izquierda + new string(' ', hueco) + derecha;
    }

    private static IEnumerable<string> Partir(string texto, int ancho)
    {
        if (string.IsNullOrEmpty(texto) || texto.Length <= ancho)
        {
            yield return texto;
            yield break;
        }

        var resto = texto;
        while (resto.Length > ancho)
        {
            var corte = resto.LastIndexOf(' ', ancho);
            if (corte <= 0)
                corte = ancho;
            yield return resto[..corte].TrimEnd();
            resto = resto[corte..].TrimStart();
        }

        if (resto.Length > 0)
            yield return resto;
    }

    private static string FmtCant(decimal cantidad)
        => cantidad == decimal.Truncate(cantidad)
            ? cantidad.ToString("0")
            : cantidad.ToString("0.##");

    private static decimal Redondear(decimal valor)
        => decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
}
