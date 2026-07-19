using System;

namespace AlahiaPos.Entities.Dto.Fiscal
{
    /// <summary>Validación previa del Motor de Definiciones (HTTP 422 en Alahia.eCF.Api).</summary>
    public sealed class FiscalValidationException : Exception
    {
        public string Codigo { get; }

        public FiscalValidationException(string mensaje, string? codigo = null)
            : base(mensaje)
        {
            Codigo = codigo ?? FiscalValidationResult.CodigoValidacionEcf;
        }
    }
}
