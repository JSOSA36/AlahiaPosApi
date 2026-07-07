namespace AlahiaPos.Entities.Dto.Invoice
{
    public class TotalesDto
    {
        // Total gravado
        public decimal MontoGravadoTotal { get; set; }

        // Monto gravado a la tasa I1 (18%)
        public decimal MontoGravadoI1 { get; set; }

        // Monto exento
        public decimal MontoExento { get; set; }

        // Tasa ITBIS
        public decimal ITBIS1 { get; set; }

        // ITBIS total
        public decimal TotalITBIS { get; set; }

        // ITBIS de la tasa I1
        public decimal TotalITBIS1 { get; set; }

        // Total factura
        public decimal MontoTotal { get; set; }
    }
}