namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Contexto inmutable de evaluación por empleado (ALAHIA-PE-01 §7.5, D3, D14, D16).
/// Prohibido mutar durante el cálculo. Prohibido I/O desde calculators.
/// </summary>
public sealed record EvaluationContext(
    int IdEmpresa,
    int IdEmpleados,
    PayrollPeriod Period,
    string RulePackId,
    string RulePackVersion,
    MoneyPolicy Money,
    IReadOnlyDictionary<string, string> PackParameters,
    IReadOnlyList<ConceptAssignment> Assignments,
    IReadOnlyList<PayrollFact> Facts,
    IReadOnlyDictionary<string, string>? ContractAttributes = null);
