using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Rules.DO.Support;
using AlahiaPos.Payroll.Rules.DO.Tables;

namespace AlahiaPos.Payroll.Rules.DO.Calculators;

public sealed class IsrEmpleadoCalculator : IConceptCalculator
{
    public string ConceptCode => DoConceptCodes.IsrEmpleado;
    public string CalculatorId => "do.isr_empleado";

    public IReadOnlyList<ConceptDependency> DependsOn { get; } = new[]
    {
        new ConceptDependency(DoConceptCodes.SueldoBase, ConceptDependencyKind.Hard),
        new ConceptDependency(DoConceptCodes.HorasExtra, ConceptDependencyKind.Soft),
        new ConceptDependency(DoConceptCodes.AfpEmpleado, ConceptDependencyKind.Hard),
        new ConceptDependency(DoConceptCodes.SfsEmpleado, ConceptDependencyKind.Hard)
    };

    public ConceptCalculationResult Execute(EvaluationContext context, IResolvedLinesView resolvedDependencies)
    {
        var includeOt = PackParameterReader.GetBool(context, DoPackParameters.OvertimeIncludedInIsrBase, true);
        var income = ContributionBase.IncomeBase(resolvedDependencies, context, includeOt);
        var afp = resolvedDependencies.SumAmount(DoConceptCodes.AfpEmpleado);
        var sfs = resolvedDependencies.SumAmount(DoConceptCodes.SfsEmpleado);
        var taxable = income - afp - sfs;

        var tableVersion = PackParameterReader.RequireString(context, DoPackParameters.IsrTableVersion);
        var brackets = IsrBracketTable.Resolve(tableVersion);
        var tax = IsrBracketTable.ComputePeriodTax(taxable, brackets, context.Money);

        var line = new PayrollLine(
            ConceptCode,
            PayrollLineOrigin.Rule,
            tax,
            context.Money.CurrencyCode,
            Description: $"ISR empleado (tabla {tableVersion})");

        var trace = new CalculationTrace(
            ConceptCode,
            BaseAmount: taxable,
            RuleDescription: "ISR period = annualize(taxable)×brackets / 24",
            ResultAmount: tax,
            CalculatorId: CalculatorId,
            RulePackId: context.RulePackId,
            RulePackVersion: context.RulePackVersion,
            Details: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Income"] = income.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["Afp"] = afp.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["Sfs"] = sfs.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["Taxable"] = taxable.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["IsrTableVersion"] = tableVersion
            });

        return ConceptCalculationResult.FromLine(line, trace);
    }
}
