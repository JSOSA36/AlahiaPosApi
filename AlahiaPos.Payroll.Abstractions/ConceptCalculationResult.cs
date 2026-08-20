namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Resultado de <see cref="IConceptCalculator"/>: una o más líneas + traces opcionales.
/// </summary>
public sealed record ConceptCalculationResult(
    IReadOnlyList<PayrollLine> Lines,
    IReadOnlyList<CalculationTrace>? Traces = null)
{
    public static ConceptCalculationResult Empty { get; } =
        new(Array.Empty<PayrollLine>(), Array.Empty<CalculationTrace>());

    public static ConceptCalculationResult FromLine(PayrollLine line, CalculationTrace? trace = null) =>
        new(
            new[] { line },
            trace is null ? Array.Empty<CalculationTrace>() : new[] { trace });
}
