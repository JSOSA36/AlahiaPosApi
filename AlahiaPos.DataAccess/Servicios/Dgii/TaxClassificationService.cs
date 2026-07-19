using AlahiaPos.Entities.Interfaces;

namespace AlahiaPos.DataAccess.Servicios.Dgii
{
    public class TaxClassificationService : ITaxClassificationService
    {
        public byte? SugerirDestinoItbisCompra(bool tieneProductosInventariables, bool esServicioEstimado)
        {
            if (esServicioEstimado && !tieneProductosInventariables)
                return 6; // servicios
            if (tieneProductosInventariables)
                return 5; // bienes
            return 5;
        }

        public byte? ResolverFormaVentaFiscal(string? tipoFactura)
        {
            if (string.IsNullOrWhiteSpace(tipoFactura))
                return null;

            var t = tipoFactura.Trim().ToLowerInvariant();
            if (t.Contains("credito") || t.Contains("crédito") || t == "credito" || t == "crédito")
                return 15;

            // Contado / mixto: no forzar un único código; montos por medio.
            return null;
        }

        public byte ResolverTipoIngresoDefault() => 1;
    }
}
