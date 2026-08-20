namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Trazabilidad del cálculo de un concepto (ALAHIA-PE-01 D17).
/// Forma parte del snapshot aprobado.
/// </summary>
public sealed record CalculationTrace(
    string ConceptCode,
    decimal? BaseAmount,
    string RuleDescription,
    decimal ResultAmount,
    string CalculatorId,
    string RulePackId,
    string RulePackVersion,
    IReadOnlyDictionary<string, string>? Details = null);
