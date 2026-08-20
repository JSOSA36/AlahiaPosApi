using AlahiaPos.Payroll.Abstractions;

namespace AlahiaPos.Payroll.Core.Dag;

/// <summary>
/// Construye el DAG a partir de metadata de calculators (sin conocer ConceptCode legales).
/// </summary>
public static class DagBuilder
{
    public static ConceptDependencyGraph Build(IEnumerable<IConceptCalculatorMetadata> metadata)
    {
        var list = metadata?.ToList() ?? throw new ArgumentNullException(nameof(metadata));
        var graph = new ConceptDependencyGraph();
        var kinds = new Dictionary<string, ConceptDependencyKind>(StringComparer.Ordinal);

        foreach (var meta in list)
        {
            if (meta is null) continue;
            if (string.IsNullOrWhiteSpace(meta.ConceptCode))
                throw new PayrollValidationException("Calculator metadata without ConceptCode.");

            graph.AddNode(meta.ConceptCode);

            foreach (var dep in meta.DependsOn ?? Array.Empty<ConceptDependency>())
            {
                if (dep is null || string.IsNullOrWhiteSpace(dep.ConceptCode))
                    continue;

                // Soft deps on unknown nodes still appear so soft handling works at runtime;
                // hard deps on unknown nodes are validated separately.
                graph.AddEdge(dep.ConceptCode, meta.ConceptCode);

                var edgeKey = EdgeKey(meta.ConceptCode, dep.ConceptCode);
                // If both hard and soft declared, hard wins.
                if (!kinds.TryGetValue(edgeKey, out var existing) || existing == ConceptDependencyKind.Soft)
                    kinds[edgeKey] = dep.Kind;
            }
        }

        graph.SetDependencyKinds(kinds);
        return graph;
    }

    public static string EdgeKey(string concept, string prerequisite) =>
        concept + "\u001f" + prerequisite;
}
