using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace AlahiaPos.Entities.Dto
{
    public class CajaMetodoPagoDto
    {
        public string FormaPago { get; set; }

        public decimal Total { get; set; }

        public static string Clave(string? formaPago)
        {
            var texto = (formaPago ?? string.Empty).Trim();
            if (texto.Length == 0)
                return "OTRO";

            texto = texto.ToUpperInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(texto.Length);
            foreach (var c in texto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            var clave = sb.ToString();
            if (clave is "EFECTIVO" or "CASH" or "CAJA")
                return "EFECTIVO";

            return clave;
        }

        public static string Etiqueta(string clave, string? original = null)
        {
            if (clave == "EFECTIVO")
                return "Efectivo";

            var texto = (original ?? string.Empty).Trim();
            return texto.Length > 0 ? texto : clave;
        }

        public static bool EsEfectivo(string? formaPago) =>
            Clave(formaPago) == "EFECTIVO";
    }
}
