using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Rules.DO.Support;

namespace AlahiaPos.Payroll.Rules.DO.Calculators;

public sealed class SfsEmpleadoCalculator : IConceptCalculator
{
    public string ConceptCode => DoConceptCodes.SfsEmpleado;
    public string CalculatorId => "do.sfs_empleado";

    public IReadOnlyList<ConceptDependency> DependsOn { get; } = new[]
    {
        new ConceptDependency(DoConceptCodes.SueldoBase, ConceptDependencyKind.Hard),
        new ConceptDependency(DoConceptCodes.HorasExtra, ConceptDependencyKind.Soft)
    };

    public ConceptCalculationResult Execute(EvaluationContext context, IResolvedLinesView resolvedDependencies)
    {
        var includeOt = PackParameterReader.GetBool(context, DoPackParameters.OvertimeIncludedInTssBase, true);
        var rate = PackParameterReader.RequireRate(context, DoPackParameters.SfsEmployeeRate);
        var baseAmount = ContributionBase.IncomeBase(resolvedDependencies, context, includeOt);

        return ContributionBase.RateLine(
            context,
            ConceptCode,
            CalculatorId,
            baseAmount,
            rate,
            "SFS empleado = base cotizable × SFS_EMPLOYEE_RATE");
    }
}
