using AlahiaPos.Payroll.Abstractions;

namespace AlahiaPos.Payroll.Rules.DO;

/// <summary>Error de dominio del RulePack DO (no del Core).</summary>
public sealed class DoRulePackException : Exception
{
    public DoRulePackException(string message) : base(message) { }
}

/// <summary>Redondeo según <see cref="MoneyPolicy"/> del contexto.</summary>
public static class DoMoney
{
    public static decimal Round(decimal amount, MoneyPolicy money)
    {
        var mode = money.RoundingMode switch
        {
            MoneyRoundingMode.AwayFromZero => MidpointRounding.AwayFromZero,
            MoneyRoundingMode.ToEven => MidpointRounding.ToEven,
            MoneyRoundingMode.TowardZero => MidpointRounding.ToZero,
            MoneyRoundingMode.ToPositiveInfinity => MidpointRounding.ToPositiveInfinity,
            MoneyRoundingMode.ToNegativeInfinity => MidpointRounding.ToNegativeInfinity,
            _ => MidpointRounding.AwayFromZero
        };

        return Math.Round(amount, money.DecimalPlaces, mode);
    }

    public static MoneyPolicy DefaultDopPolicy { get; } = new(
        CurrencyCode: "DOP",
        DecimalPlaces: 2,
        RoundingMode: MoneyRoundingMode.AwayFromZero,
        RoundPerLine: true,
        RoundAggregates: true);
}
