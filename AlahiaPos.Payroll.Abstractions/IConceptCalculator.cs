namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Vista de solo lectura de líneas ya calculadas (dependencias resueltas).
/// </summary>
public interface IResolvedLinesView
{
    IReadOnlyList<PayrollLine> All { get; }

    IReadOnlyList<PayrollLine> ForConcept(string conceptCode);

    decimal SumAmount(string conceptCode);
}

/// <summary>
/// Unidad de extensión del motor: un concepto (ALAHIA-PE-GOLDEN).
/// Sin BD, sin HTTP, sin servicios de infraestructura.
/// </summary>
public interface IConceptCalculator : IConceptCalculatorMetadata
{
    /// <summary>
    /// Ejecuta el cálculo de forma pura sobre el contexto y las líneas de dependencias.
    /// </summary>
    ConceptCalculationResult Execute(EvaluationContext context, IResolvedLinesView resolvedDependencies);
}
