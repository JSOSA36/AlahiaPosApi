using AlahiaPos.Payroll.Abstractions;

namespace AlahiaPos.Payroll.Rules.DO.Support;

/// <summary>Helpers compartidos entre calculators de contribución / neto.</summary>
public static class ContributionBase
{
    public static decimal IncomeBase(
        IResolvedLinesView deps,
        EvaluationContext context,
        bool includeOvertime)
    {
        var sum = deps.SumAmount(DoConceptCodes.SueldoBase);
        if (includeOvertime)
            sum += deps.SumAmount(DoConceptCodes.HorasExtra);
        return sum;
    }

    public static ConceptCalculationResult RateLine(
        EvaluationContext context,
        string conceptCode,
        string calculatorId,
        decimal baseAmount,
        decimal rate,
        string ruleDescription)
    {
        var amount = DoMoney.Round(baseAmount * rate, context.Money);
        var line = new PayrollLine(
            conceptCode,
            PayrollLineOrigin.Rule,
            amount,
            context.Money.CurrencyCode,
            Description: ruleDescription);

        var trace = new CalculationTrace(
            ConceptCode: conceptCode,
            BaseAmount: baseAmount,
            RuleDescription: ruleDescription,
            ResultAmount: amount,
            CalculatorId: calculatorId,
            RulePackId: context.RulePackId,
            RulePackVersion: context.RulePackVersion,
            Details: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Rate"] = rate.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["Base"] = baseAmount.ToString(System.Globalization.CultureInfo.InvariantCulture)
            });

        return ConceptCalculationResult.FromLine(line, trace);
    }
}
