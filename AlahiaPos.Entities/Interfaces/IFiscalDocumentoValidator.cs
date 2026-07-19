using AlahiaPos.Entities.Dto.Fiscal;

namespace AlahiaPos.Entities.Interfaces
{
    /// <summary>
    /// Valida un documento fiscal contra el Motor de Definiciones (única fuente de verdad).
    /// ERP y Alahia.eCF.Api deben usar esta misma implementación.
    /// </summary>
    public interface IFiscalDocumentoValidator
    {
        FiscalValidationResult Validar(FiscalDocumentoElectronico documento);
    }
}
