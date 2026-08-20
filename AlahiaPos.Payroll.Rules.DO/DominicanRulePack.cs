using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Rules.DO.Calculators;

namespace AlahiaPos.Payroll.Rules.DO;

/// <summary>
/// RulePack operativo República Dominicana — <c>DO-2026.01</c>.
/// Solo referencia Abstractions. Tasas/tablas son parámetros versionados del pack.
/// </summary>
public sealed class DominicanRulePack : IPayrollRulePack
{
    private DominicanRulePack()
    {
        Metadata = new RulePackMetadata(
            PackId: DoPackParameters.PackId,
            JurisdictionCode: DoPackParameters.JurisdictionCode,
            Version: DoPackParameters.Version,
            DefaultMoneyPolicy: DoMoney.DefaultDopPolicy,
            Parameters: DoPackParameters.DefaultParameters);

        Calculators = new IConceptCalculator[]
        {
            new SueldoBaseCalculator(),
            new HorasExtraCalculator(),
            new AfpEmpleadoCalculator(),
            new SfsEmpleadoCalculator(),
            new IsrEmpleadoCalculator(),
            new AfpPatronalCalculator(),
            new SfsPatronalCalculator(),
            new InfotepPatronalCalculator(),
            new NetoCalculator()
        };

        Concepts = Calculators.Select(c => new ConceptMetadata(
            ConceptCode: c.ConceptCode,
            DisplayName: c.ConceptCode,
            Category: Categorize(c.ConceptCode),
            DependsOn: c.DependsOn,
            IsActive: true)).ToList();
    }

    public static DominicanRulePack Create() => new();

    public RulePackMetadata Metadata { get; }
    public IReadOnlyList<IConceptCalculator> Calculators { get; }
    public IReadOnlyList<ConceptMetadata> Concepts { get; }

    private static string Categorize(string code) => code switch
    {
        DoConceptCodes.SueldoBase or DoConceptCodes.HorasExtra => "INGRESO",
        DoConceptCodes.AfpEmpleado or DoConceptCodes.SfsEmpleado or DoConceptCodes.IsrEmpleado => "DEDUCCION_EMPLEADO",
        DoConceptCodes.AfpPatronal or DoConceptCodes.SfsPatronal or DoConceptCodes.InfotepPatronal => "APORTE_PATRONAL",
        DoConceptCodes.Neto => "RESULTADO",
        _ => "OTRO"
    };
}
