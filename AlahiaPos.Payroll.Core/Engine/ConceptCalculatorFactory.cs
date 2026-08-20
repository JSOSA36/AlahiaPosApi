using AlahiaPos.Payroll.Abstractions;

namespace AlahiaPos.Payroll.Core.Engine;

/// <summary>
/// Factory en memoria construida desde un <see cref="IPayrollRulePack"/> (sin I/O).
/// </summary>
public sealed class ConceptCalculatorFactory : IConceptCalculatorFactory
{
    private readonly Dictionary<string, IConceptCalculator> _byCode;

    public ConceptCalculatorFactory(IEnumerable<IConceptCalculator> calculators)
    {
        _byCode = new Dictionary<string, IConceptCalculator>(StringComparer.Ordinal);
        foreach (var calc in calculators ?? throw new ArgumentNullException(nameof(calculators)))
        {
            if (calc is null) continue;
            if (string.IsNullOrWhiteSpace(calc.ConceptCode))
                throw new PayrollValidationException("Calculator without ConceptCode.");
            if (!_byCode.TryAdd(calc.ConceptCode, calc))
                throw new PayrollValidationException($"Duplicate calculator for concept '{calc.ConceptCode}'.");
        }
    }

    public static ConceptCalculatorFactory FromRulePack(IPayrollRulePack rulePack)
    {
        if (rulePack is null) throw new ArgumentNullException(nameof(rulePack));
        return new ConceptCalculatorFactory(rulePack.Calculators);
    }

    public IReadOnlyList<IConceptCalculatorMetadata> AllMetadata =>
        _byCode.Values.Cast<IConceptCalculatorMetadata>().ToList();

    public bool TryGet(string conceptCode, out IConceptCalculator? calculator)
    {
        if (_byCode.TryGetValue(conceptCode, out var found))
        {
            calculator = found;
            return true;
        }

        calculator = null;
        return false;
    }

    public IConceptCalculator GetRequired(string conceptCode)
    {
        if (TryGet(conceptCode, out var calc) && calc is not null)
            return calc;
        throw new PayrollValidationException($"No calculator registered for concept '{conceptCode}'.");
    }
}
