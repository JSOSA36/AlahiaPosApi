namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Metadata de un concepto en el catálogo del run/pack.
/// <see cref="ConceptCode"/> es contrato público estable (ALAHIA-PE-01 D15).
/// Sin significados legales de país en este ensamblado.
/// </summary>
public sealed record ConceptMetadata(
    string ConceptCode,
    string? DisplayName,
    string? Category,
    IReadOnlyList<ConceptDependency> DependsOn,
    bool IsActive = true);
