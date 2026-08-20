namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Política monetaria inmutable del contexto / RulePack (ALAHIA-PE-01 D16).
/// </summary>
public sealed record MoneyPolicy(
    string CurrencyCode,
    int DecimalPlaces,
    MoneyRoundingMode RoundingMode,
    bool RoundPerLine,
    bool RoundAggregates);
