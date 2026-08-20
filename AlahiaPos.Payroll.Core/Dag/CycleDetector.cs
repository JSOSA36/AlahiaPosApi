namespace AlahiaPos.Payroll.Core.Dag;

/// <summary>Detecta ciclos en el DAG (DFS).</summary>
public static class CycleDetector
{
    private enum Color { White, Gray, Black }

    public static void EnsureAcyclic(ConceptDependencyGraph graph)
    {
        var color = graph.Nodes.ToDictionary(n => n, _ => Color.White, StringComparer.Ordinal);
        var stack = new List<string>();

        foreach (var node in graph.Nodes.OrderBy(n => n, StringComparer.Ordinal))
        {
            if (color[node] == Color.White)
                Dfs(node, graph, color, stack);
        }
    }

    private static void Dfs(
        string node,
        ConceptDependencyGraph graph,
        Dictionary<string, Color> color,
        List<string> stack)
    {
        color[node] = Color.Gray;
        stack.Add(node);

        foreach (var next in graph.GetDependents(node).OrderBy(n => n, StringComparer.Ordinal))
        {
            if (color[next] == Color.Gray)
            {
                var idx = stack.IndexOf(next);
                var cycle = stack.Skip(idx).Append(next).ToList();
                throw new PayrollCycleException(cycle);
            }

            if (color[next] == Color.White)
                Dfs(next, graph, color, stack);
        }

        stack.RemoveAt(stack.Count - 1);
        color[node] = Color.Black;
    }
}
