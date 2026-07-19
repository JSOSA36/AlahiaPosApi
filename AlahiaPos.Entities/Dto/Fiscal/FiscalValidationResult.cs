using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto.Fiscal
{
    /// <summary>Resultado de validación previa (Motor de Definiciones).</summary>
    public sealed class FiscalValidationResult
    {
        public const string CodigoValidacionEcf = "VALIDACION_ECF";

        public bool Ok { get; init; }
        public string? Codigo { get; init; }
        public string? Mensaje { get; init; }
        public List<string> Mensajes { get; init; } = new();

        public static FiscalValidationResult Exitoso() => new() { Ok = true };

        public static FiscalValidationResult Fallo(string mensaje, string codigo = CodigoValidacionEcf)
            => new()
            {
                Ok = false,
                Codigo = codigo,
                Mensaje = mensaje,
                Mensajes = new List<string> { mensaje }
            };
    }
}
