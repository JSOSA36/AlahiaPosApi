using System.Security.Cryptography;
using System.Text;
using AlahiaPos.Payroll.Abstractions;
using AlahiaPos.Payroll.Core.Dag;

namespace AlahiaPos.Payroll.Core.Engine;

/// <summary>
/// Ejecuta el orden topológico para un único <see cref="EvaluationContext"/>.
/// </summary>
public sealed class CalculationPipeline
{
    private readonly IConceptCalculatorFactory _factory;
    private readonly ConceptDependencyGraph _graph;
    private readonly IReadOnlyList<string> _order;

    public CalculationPipeline(
        IConceptCalculatorFactory factory,
        ConceptDependencyGraph graph,
        IReadOnlyList<string> topologicalOrder)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        _order = topologicalOrder ?? throw new ArgumentNullException(nameof(topologicalOrder));
    }

    public (IReadOnlyList<PayrollLine> Lines, IReadOnlyList<CalculationTrace> Traces) Execute(
        EvaluationContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        // Captura identidad del record: calculators no pueden mutar el contexto del pipeline.
        var frozen = context;

        var buffer = new ResolvedLinesBuffer();
        var traces = new List<CalculationTrace>();

        // Solo conceptos con calculator registrado se ejecutan (nodos solo-dep soft se omiten).
        foreach (var conceptCode in _order)
        {
            if (!_factory.TryGet(conceptCode, out var calculator) || calculator is null)
                continue;

            EnsureHardDependenciesSatisfied(calculator, buffer);

            var depCodes = (calculator.DependsOn ?? Array.Empty<ConceptDependency>())
                .Select(d => d.ConceptCode);
            var depView = buffer.ForDependencies(depCodes);

            var result = calculator.Execute(frozen, depView)
                ?? ConceptCalculationResult.Empty;

            // Defensa: el contexto pasado es el mismo record inmutable.
            if (!ReferenceEquals(frozen, context) || frozen != context)
                throw new PayrollEngineException("EvaluationContext integrity violation.");

            if (result.Lines is { Count: > 0 })
            {
                var stamped = result.Lines.Select(l => StampEmployee(l, frozen.IdEmpleados)).ToList();
                buffer.AddRange(stamped);
            }

            if (result.Traces is { Count: > 0 })
                traces.AddRange(result.Traces);
        }

        return (buffer.All.ToList(), traces);
    }

    private void EnsureHardDependenciesSatisfied(
        IConceptCalculator calculator,
        ResolvedLinesBuffer buffer)
    {
        foreach (var dep in calculator.DependsOn ?? Array.Empty<ConceptDependency>())
        {
            if (dep.Kind != ConceptDependencyKind.Hard)
                continue;

            var lines = buffer.ForConcept(dep.ConceptCode);
            if (lines.Count == 0)
                throw new PayrollDependencyException(calculator.ConceptCode, dep.ConceptCode);
        }
    }

    private static PayrollLine StampEmployee(PayrollLine line, int idEmpleados)
    {
        var attrs = line.Attributes is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(line.Attributes, StringComparer.Ordinal);

        attrs["IdEmpleados"] = idEmpleados.ToString();
        return line with { Attributes = attrs };
    }
}

/// <summary>Hash determinista de snapshot para aprobación.</summary>
public static class PayrollSnapshotHasher
{
    public static string Compute(PayrollRun run)
    {
        var sb = new StringBuilder();
        sb.Append(run.IdEmpresa).Append('|')
          .Append(run.PayrollRunId).Append('|')
          .Append(run.ExecutionKey.PeriodKey).Append('|')
          .Append(run.ExecutionKey.Intent).Append('|')
          .Append(run.RulePackId).Append('|')
          .Append(run.RulePackVersion).Append('|')
          .Append(run.Money.CurrencyCode).Append('|')
          .Append(run.Money.DecimalPlaces).Append('|');

        foreach (var line in run.Lines.OrderBy(l => l.ConceptCode).ThenBy(l => l.Amount))
        {
            sb.Append(line.ConceptCode).Append('=')
              .Append(line.Amount).Append('@')
              .Append(line.Origin).Append(';');
        }

        foreach (var t in run.Traces.OrderBy(t => t.ConceptCode).ThenBy(t => t.ResultAmount))
        {
            sb.Append("T:").Append(t.ConceptCode).Append('=')
              .Append(t.ResultAmount).Append('/');
            sb.Append(t.RuleDescription).Append(';');
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
