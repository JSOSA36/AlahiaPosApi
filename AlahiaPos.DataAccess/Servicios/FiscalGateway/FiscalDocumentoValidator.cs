using System;
using AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto.Definitions;
using AlahiaPos.Entities.Dto.Fiscal;
using AlahiaPos.Entities.Interfaces;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway
{
    /// <summary>
    /// Facade del Motor de Definiciones. No inventa reglas: solo invoca
    /// <see cref="IEcfTipoDefinition.Validar"/> y mapea el resultado.
    /// </summary>
    public sealed class FiscalDocumentoValidator : IFiscalDocumentoValidator
    {
        public FiscalValidationResult Validar(FiscalDocumentoElectronico documento)
        {
            if (documento?.Encabezado == null)
                return FiscalValidationResult.Fallo("Documento fiscal inválido: falta Encabezado.");

            try
            {
                var def = EcfTipoDefinitionRegistry.Get(documento.Encabezado.TipoEcf);
                var ctx = new EcfBuildContext
                {
                    Documento = documento,
                    Definicion = def,
                    FechaHoraFirma = DateTime.Now
                };
                def.Validar(ctx);
                return FiscalValidationResult.Exitoso();
            }
            catch (NotSupportedException ex)
            {
                return FiscalValidationResult.Fallo(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return FiscalValidationResult.Fallo(ex.Message);
            }
        }
    }
}
