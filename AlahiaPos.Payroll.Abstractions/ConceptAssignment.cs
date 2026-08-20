namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Asignación de concepto vigente resuelta en el contexto (sin I/O).
/// </summary>
public sealed record ConceptAssignment(
    string ConceptCode,
    decimal? FixedAmount,
    decimal? Rate,
    string? FormulaOrRuleId,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    IReadOnlyDictionary<string, string>? Attributes = null);
