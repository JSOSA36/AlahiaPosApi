namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Solicitud para construir un <see cref="EvaluationContext"/> (sin cálculo).
/// </summary>
public sealed record EvaluationContextBuildRequest(
    int IdEmpresa,
    int IdEmpleados,
    PayrollPeriod Period,
    string RulePackId,
    string RulePackVersion,
    MoneyPolicy Money,
    IReadOnlyDictionary<string, string> PackParameters);

/// <summary>
/// Ensambla el contexto inmutable de evaluación desde fuentes de infraestructura.
/// No calcula conceptos ni conoce RulePacks concretos.
/// </summary>
public interface IEvaluationContextBuilder
{
    Task<EvaluationContext> BuildAsync(
        EvaluationContextBuildRequest request,
        CancellationToken cancellationToken = default);
}
