namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Hecho del período ya interpretado (asistencia, HE, préstamo, comisión, …).
/// El motor no interpreta marcaciones crudas.
/// </summary>
public sealed record PayrollFact(
    string FactType,
    string? ConceptCode,
    decimal Quantity,
    decimal Amount,
    IReadOnlyDictionary<string, string>? Attributes = null);
