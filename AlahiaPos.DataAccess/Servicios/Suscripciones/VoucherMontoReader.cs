using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AlahiaPos.Entities.Interfaces;
using Microsoft.Extensions.Configuration;
using Tesseract;

namespace AlahiaPos.DataAccess.Servicios.Suscripciones
{
    /// <summary>
    /// OCR local gratis (Tesseract). Sin IA ni costos por API.
    /// </summary>
    public class VoucherMontoReader : IVoucherMontoReader
    {
        private static readonly HashSet<string> MimePermitidos = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/jpg", "image/png", "image/gif"
        };

        private static readonly string[] PalabrasMonto =
        {
            "MONTO", "IMPORTE", "VALOR", "TRANSFER", "DEPOSITO", "DEPÓSITO",
            "PAGO", "TOTAL", "AMOUNT", "RD$", "DOP", "PESOS"
        };

        private readonly string _tessdataPath;

        public VoucherMontoReader(IConfiguration config)
        {
            var configured = config["Suscripcion:TessdataPath"];
            _tessdataPath = !string.IsNullOrWhiteSpace(configured)
                ? configured.Trim()
                : Path.Combine(AppContext.BaseDirectory, "tessdata");
        }

        public Task<VoucherMontoLectura> LeerMontoAsync(
            byte[] imagenBytes,
            string? contentType,
            CancellationToken ct = default)
        {
            return Task.Run(() => LeerMontoSync(imagenBytes, contentType), ct);
        }

        private VoucherMontoLectura LeerMontoSync(byte[] imagenBytes, string? contentType)
        {
            if (imagenBytes == null || imagenBytes.Length == 0)
                return Fallo("Debe adjuntar la imagen del voucher.");

            if (imagenBytes.Length > 8 * 1024 * 1024)
                return Fallo("El voucher es demasiado grande (máx. 8 MB). Suba una foto más liviana.");

            var mime = NormalizarMime(contentType, imagenBytes);
            if (mime.Contains("pdf", StringComparison.OrdinalIgnoreCase))
            {
                return Fallo(
                    "Para validar el monto debe adjuntar una foto (JPG/PNG) del voucher, no un PDF.");
            }

            if (mime.Contains("webp", StringComparison.OrdinalIgnoreCase))
                return Fallo("Use una foto JPG o PNG del voucher (WebP no soportado).");

            if (!MimePermitidos.Contains(mime))
                return Fallo("Formato no válido. Adjunte una foto JPG o PNG del comprobante.");

            var trained = Path.Combine(_tessdataPath, "eng.traineddata");
            if (!Directory.Exists(_tessdataPath) || !File.Exists(trained))
            {
                return Fallo(
                    "OCR del voucher no está instalado en el servidor (faltan archivos tessdata). Contacte a MacroBits.");
            }

            string texto;
            try
            {
                using var engine = new TesseractEngine(_tessdataPath, "eng", EngineMode.Default);
                using var img = Pix.LoadFromMemory(imagenBytes);
                using var page = engine.Process(img);
                texto = page.GetText() ?? string.Empty;
            }
            catch (Exception ex)
            {
                return Fallo(
                    "No se pudo leer el voucher. Suba una foto más clara y nítida. " +
                    $"({ex.Message})");
            }

            if (string.IsNullOrWhiteSpace(texto))
                return Fallo("No se detectó texto en el voucher. Suba una foto más clara.");

            var monto = ExtraerMonto(texto);
            if (monto == null || monto <= 0)
            {
                return Fallo(
                    "No se pudo detectar el monto en el voucher. Suba una foto nítida donde se vea el monto.",
                    texto);
            }

            return new VoucherMontoLectura
            {
                Ok = true,
                Monto = Math.Round(monto.Value, 2, MidpointRounding.AwayFromZero),
                Moneda = "DOP",
                Raw = texto.Length > 500 ? texto[..500] : texto
            };
        }

        /// <summary>
        /// Busca montos tipo dinero; prioriza los cercanos a palabras MONTO/TRANSFER/RD$.
        /// </summary>
        private static decimal? ExtraerMonto(string texto)
        {
            var normalizado = texto.Replace('\n', ' ').Replace('\r', ' ');
            var upper = normalizado.ToUpperInvariant();

            var matches = Regex.Matches(
                normalizado,
                @"(?:RD\$|DOP|US\$|\$)?\s*(\d{1,3}(?:[.,]\d{3})+[.,]\d{2}|\d+[.,]\d{2}|\d{3,6})",
                RegexOptions.IgnoreCase);

            var candidatos = new List<(decimal Monto, int Index, bool ConEtiqueta)>();
            foreach (Match m in matches)
            {
                if (!TryParseMontoLibre(m.Groups[1].Value, out var valor))
                    continue;

                // Filtra números de cuenta / referencias enormes o irreales para suscripción
                if (valor < 50m || valor > 2_000_000m)
                    continue;

                // Si no tiene decimales y es muy largo, suele ser referencia
                var rawDigits = Regex.Replace(m.Groups[1].Value, @"[^\d]", "");
                if (!m.Groups[1].Value.Contains('.') && !m.Groups[1].Value.Contains(',')
                    && rawDigits.Length >= 8)
                    continue;

                var idx = m.Index;
                var ventana = upper.Substring(
                    Math.Max(0, idx - 40),
                    Math.Min(80, upper.Length - Math.Max(0, idx - 40)));
                var conEtiqueta = PalabrasMonto.Any(p => ventana.Contains(p));

                candidatos.Add((valor, idx, conEtiqueta));
            }

            if (candidatos.Count == 0)
                return null;

            var etiquetados = candidatos.Where(c => c.ConEtiqueta).Select(c => c.Monto).ToList();
            if (etiquetados.Count > 0)
                return etiquetados.Max();

            // Sin etiqueta: el mayor monto “de dinero” (típicamente el depósito)
            return candidatos.Max(c => c.Monto);
        }

        private static bool TryParseMontoLibre(string raw, out decimal value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            var s = raw.Trim().Replace(" ", "");

            if (s.Contains(',') && s.Contains('.'))
            {
                if (s.LastIndexOf(',') > s.LastIndexOf('.'))
                    s = s.Replace(".", "").Replace(',', '.');
                else
                    s = s.Replace(",", "");
            }
            else if (s.Contains(',') && !s.Contains('.'))
            {
                // 1500,00 o 1.500 ambiguo — si 2 dígitos tras coma = decimal
                var parts = s.Split(',');
                if (parts.Length == 2 && parts[1].Length == 2)
                    s = parts[0].Replace(".", "") + "." + parts[1];
                else
                    s = s.Replace(",", "");
            }

            return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        private static string NormalizarMime(string? contentType, byte[] bytes)
        {
            var mime = (contentType ?? "").Split(';')[0].Trim();
            if (!string.IsNullOrWhiteSpace(mime) && mime != "application/octet-stream")
                return mime;

            if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
                return "image/jpeg";
            if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50)
                return "image/png";
            if (bytes.Length >= 4 && bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46)
                return "application/pdf";
            if (bytes.Length >= 4 && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46)
                return "image/webp";

            return "image/jpeg";
        }

        private static VoucherMontoLectura Fallo(string error, string? raw = null) => new()
        {
            Ok = false,
            Error = error,
            Raw = raw
        };
    }
}
