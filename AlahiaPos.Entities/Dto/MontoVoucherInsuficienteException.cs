using System;

namespace AlahiaPos.Entities.Dto
{
    /// <summary>El OCR leyó un monto menor al total adeudado de suscripción.</summary>
    public class MontoVoucherInsuficienteException : InvalidOperationException
    {
        public decimal MontoVoucherDop { get; }
        public decimal TotalAdeudadoDop { get; }
        public decimal MontoReconexionDop { get; }
        public decimal MontoPlanDop { get; }

        public MontoVoucherInsuficienteException(
            decimal montoVoucherDop,
            decimal totalAdeudadoDop,
            decimal montoReconexionDop,
            decimal montoPlanDop)
            : base(BuildMessage(montoVoucherDop, totalAdeudadoDop, montoReconexionDop))
        {
            MontoVoucherDop = montoVoucherDop;
            TotalAdeudadoDop = totalAdeudadoDop;
            MontoReconexionDop = montoReconexionDop;
            MontoPlanDop = montoPlanDop;
        }

        private static string BuildMessage(
            decimal montoVoucherDop,
            decimal totalAdeudadoDop,
            decimal montoReconexionDop)
        {
            var reconex = montoReconexionDop > 0
                ? $" Incluye cargo por reconexión de RD$ {montoReconexionDop:N2}."
                : "";
            return
                $"El monto del voucher (RD$ {montoVoucherDop:N2}) es menor al total adeudado " +
                $"(RD$ {totalAdeudadoDop:N2}).{reconex} " +
                "Debe depositar el total completo antes de reportar el pago.";
        }
    }
}
