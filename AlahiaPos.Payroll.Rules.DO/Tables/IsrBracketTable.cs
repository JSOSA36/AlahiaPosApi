using AlahiaPos.Payroll.Abstractions;

namespace AlahiaPos.Payroll.Rules.DO.Tables;

public sealed record IsrBracket(decimal FromInclusive, decimal? ToExclusive, decimal Rate, decimal FixedTax);

/// <summary>
/// Tablas ISR versionadas dentro del pack (no oficiales — ilustrativas para arquitectura).
/// Montos anuales simplificados; el calculator aplica prorrateo quincenal × 24.
/// </summary>
public static class IsrBracketTable
{
    /// <summary>24 quincenas / año (simplificación operativa del pack).</summary>
    public const int PeriodsPerYear = 24;

    private static readonly IReadOnlyList<IsrBracket> Simple2026 = new[]
    {
        // Tramos anuales ilustrativos (DOP).
        new IsrBracket(0m, 416_220m, 0m, 0m),
        new IsrBracket(416_220m, 624_329m, 0.15m, 0m),
        new IsrBracket(624_329m, 867_123m, 0.20m, 31_216m),
        new IsrBracket(867_123m, null, 0.25m, 79_776m)
    };

    public static IReadOnlyList<IsrBracket> Resolve(string tableVersion)
    {
        if (string.Equals(tableVersion, DoPackParameters.DefaultIsrTableVersion, StringComparison.Ordinal)
            || string.Equals(tableVersion, "DO-2026.01-SIMPLE", StringComparison.Ordinal))
        {
            return Simple2026;
        }

        throw new DoRulePackException($"Unknown ISR_TABLE_VERSION '{tableVersion}'.");
    }

    /// <summary>Calcula ISR del período a partir de base gravable del período.</summary>
    public static decimal ComputePeriodTax(decimal periodTaxableBase, IReadOnlyList<IsrBracket> brackets, MoneyPolicy money)
    {
        if (periodTaxableBase <= 0m)
            return 0m;

        var annualized = periodTaxableBase * PeriodsPerYear;
        var annualTax = ComputeAnnualTax(annualized, brackets);
        var periodTax = annualTax / PeriodsPerYear;
        return DoMoney.Round(periodTax, money);
    }

    public static decimal ComputeAnnualTax(decimal annualTaxable, IReadOnlyList<IsrBracket> brackets)
    {
        if (annualTaxable <= 0m)
            return 0m;

        foreach (var b in brackets)
        {
            var upper = b.ToExclusive ?? decimal.MaxValue;
            if (annualTaxable >= b.FromInclusive && annualTaxable < upper)
            {
                var excess = annualTaxable - b.FromInclusive;
                return b.FixedTax + excess * b.Rate;
            }
        }

        // Si cae exactamente en el último tope abierto: último bracket.
        var last = brackets[^1];
        var lastExcess = annualTaxable - last.FromInclusive;
        return last.FixedTax + Math.Max(0m, lastExcess) * last.Rate;
    }
}
