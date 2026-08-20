namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// RulePack instalable (jurisdicción o variante). El Core no referencia packs concretos.
/// </summary>
public interface IPayrollRulePack
{
    RulePackMetadata Metadata { get; }

    /// <summary>Calculators registrados por este pack.</summary>
    IReadOnlyList<IConceptCalculator> Calculators { get; }

    /// <summary>Conceptos que el pack aporta al catálogo (sin lógica de cálculo).</summary>
    IReadOnlyList<ConceptMetadata> Concepts { get; }
}
