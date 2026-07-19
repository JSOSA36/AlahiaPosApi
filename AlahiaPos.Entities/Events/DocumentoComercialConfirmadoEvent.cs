namespace AlahiaPos.Entities.Events
{
    /// <summary>
    /// Publicado tras commit comercial exitoso. Consumidores opcionales (DGII, otros)
    /// no deben afectar la operación de negocio.
    /// </summary>
    public class DocumentoComercialConfirmadoEvent : DomainEventBase
    {
        public override string TipoEvento => DomainEventTypes.DocumentoComercialConfirmado;

        /// <summary>Venta | NotaCredito | Compra</summary>
        public string TipoDocumentoFiscal { get; set; } = "Venta";
    }
}
