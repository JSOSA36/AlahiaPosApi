using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Rules.DO.Support;

namespace AlahiaPos.Payroll.Rules.DO.Calculators;

public sealed class AfpPatronalCalculator : IConceptCalculator
{
    public string ConceptCode => DoConceptCodes.AfpPatronal;
    public string CalculatorId => "do.afp_patronal";

    public IReadOnlyList<ConceptDependency> DependsOn { get; } = new[]
    {
        new ConceptDependency(DoConceptCodes.SueldoBase, ConceptDependencyKind.Hard),
        new ConceptDependency(DoConceptCodes.HorasExtra, ConceptDependencyKind.Soft)
    };

    public ConceptCalculationResult Execute(EvaluationContext context, IResolvedLinesView resolvedDependencies)
    {
        var includeOt = PackParameterReader.GetBool(context, DoPackParameters.OvertimeIncludedInTssBase, true);
        var rate = PackParameterReader.RequireRate(context, DoPackParameters.AfpEmployerRate);
        var baseAmount = ContributionBase.IncomeBase(resolvedDependencies, context, includeOt);

        return ContributionBase.RateLine(
            context,
            ConceptCode,
            CalculatorId,
            baseAmount,
            rate,
            "AFP patronal = base cotizable × AFP_EMPLOYER_RATE");
    }
}
