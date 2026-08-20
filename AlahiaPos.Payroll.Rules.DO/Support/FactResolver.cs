using System.Globalization;
using AlahiaPos.Payroll.Abstractions;

namespace AlahiaPos.Payroll.Rules.DO.Support;

/// <summary>Consume hechos ya interpretados del período (sin asistencia).</summary>
public static class FactResolver
{
    public static IReadOnlyList<PayrollFact> ForConcept(EvaluationContext context, string conceptCode) =>
        context.Facts
            .Where(f =>
                string.Equals(f.ConceptCode, conceptCode, StringComparison.Ordinal)
                || string.Equals(f.FactType, conceptCode, StringComparison.Ordinal))
            .ToList();

    /// <summary>
    /// Monto del hecho: usa Amount si &gt; 0; si no, Quantity × rate en Attributes["Rate"].
    /// </summary>
    public static decimal ResolveAmount(PayrollFact fact)
    {
        if (fact.Amount != 0m)
            return fact.Amount;

        if (fact.Quantity == 0m)
            return 0m;

        if (fact.Attributes is not null
            && fact.Attributes.TryGetValue("Rate", out var rateRaw)
            && decimal.TryParse(rateRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate))
        {
            return fact.Quantity * rate;
        }

        return 0m;
    }

    public static decimal SumAmount(EvaluationContext context, string conceptCode) =>
        ForConcept(context, conceptCode).Sum(ResolveAmount);
}
