namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Dependencia explícita de un concepto hacia otro en el DAG (ALAHIA-PE-01 §8).
/// </summary>
public sealed record ConceptDependency(
    string ConceptCode,
    ConceptDependencyKind Kind = ConceptDependencyKind.Hard);
