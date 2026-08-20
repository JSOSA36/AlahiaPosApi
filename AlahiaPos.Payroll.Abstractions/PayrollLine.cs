namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Línea de liquidación producida por el motor (ALAHIA-PE-01).
/// </summary>
public sealed record PayrollLine(
    string ConceptCode,
    PayrollLineOrigin Origin,
    decimal Amount,
    string CurrencyCode,
    string? Description = null,
    IReadOnlyDictionary<string, string>? Attributes = null);
