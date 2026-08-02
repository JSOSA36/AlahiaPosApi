namespace AlahiaPos.Entities.Interfaces
{
    /// <summary>
    /// Extrae el texto plano de un documento PDF (p.ej. estados de cuenta bancarios).
    /// </summary>
    public interface IPdfTextExtractor
    {
        /// <exception cref="InvalidOperationException">Si el PDF está cifrado o no puede leerse.</exception>
        string ExtractText(byte[] pdfBytes);
    }
}
