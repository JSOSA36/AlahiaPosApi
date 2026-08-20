namespace AlahiaPos.Payroll.Core.Dag;

/// <summary>Orden topológico (Kahn). Prerrequisitos primero.</summary>
public static class TopologicalSorter
{
    public static IReadOnlyList<string> Sort(ConceptDependencyGraph graph)
    {
        var indegree = graph.Nodes.ToDictionary(
            n => n,
            n => graph.GetPrerequisites(n).Count,
            StringComparer.Ordinal);

        var queue = new SortedSet<string>(
            indegree.Where(kv => kv.Value == 0).Select(kv => kv.Key),
            StringComparer.Ordinal);

        var order = new List<string>(indegree.Count);

        while (queue.Count > 0)
        {
            var n = queue.Min!;
            queue.Remove(n);
            order.Add(n);

            foreach (var m in graph.GetDependents(n).OrderBy(x => x, StringComparer.Ordinal))
            {
                indegree[m]--;
                if (indegree[m] == 0)
                    queue.Add(m);
            }
        }

        if (order.Count != graph.Nodes.Count)
            throw new PayrollCycleException(new[] { "(unresolved cycle during topological sort)" });

        return order;
    }

    /// <summary>
    /// Niveles independientes (mismo nivel = sin dependencias cruzadas; paralelizable a futuro).
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<string>> SortLevels(ConceptDependencyGraph graph)
    {
        var order = Sort(graph);
        var levelOf = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var node in order)
        {
            var level = 0;
            foreach (var pre in graph.GetPrerequisites(node))
                level = Math.Max(level, levelOf[pre] + 1);
            levelOf[node] = level;
        }

        var max = levelOf.Count == 0 ? -1 : levelOf.Values.Max();
        var levels = Enumerable.Range(0, max + 1)
            .Select(_ => new List<string>())
            .ToList();

        foreach (var node in order)
            levels[levelOf[node]].Add(node);

        return levels;
    }
}
