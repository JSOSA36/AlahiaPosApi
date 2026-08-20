using AlahiaPos.Payroll.Abstractions;

namespace AlahiaPos.Payroll.Core.Dag;

/// <summary>
/// Grafo de dependencias entre ConceptCode.
/// Arista A → B significa: A debe calcularse antes que B (B depende de A).
/// </summary>
public sealed class ConceptDependencyGraph
{
    private readonly Dictionary<string, HashSet<string>> _prerequisites = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _dependents = new(StringComparer.Ordinal);
    private readonly HashSet<string> _nodes = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> Nodes => _nodes;

    public void AddNode(string conceptCode)
    {
        if (string.IsNullOrWhiteSpace(conceptCode))
            throw new ArgumentException("ConceptCode is required.", nameof(conceptCode));

        _nodes.Add(conceptCode);
        if (!_prerequisites.ContainsKey(conceptCode))
            _prerequisites[conceptCode] = new HashSet<string>(StringComparer.Ordinal);
        if (!_dependents.ContainsKey(conceptCode))
            _dependents[conceptCode] = new HashSet<string>(StringComparer.Ordinal);
    }

    /// <summary>prerequisite must run before concept.</summary>
    public void AddEdge(string prerequisite, string concept)
    {
        AddNode(prerequisite);
        AddNode(concept);
        _prerequisites[concept].Add(prerequisite);
        _dependents[prerequisite].Add(concept);
    }

    public IReadOnlyCollection<string> GetPrerequisites(string conceptCode) =>
        _prerequisites.TryGetValue(conceptCode, out var set)
            ? set
            : Array.Empty<string>();

    public IReadOnlyCollection<string> GetDependents(string conceptCode) =>
        _dependents.TryGetValue(conceptCode, out var set)
            ? set
            : Array.Empty<string>();

    public IReadOnlyDictionary<string, ConceptDependencyKind> DependencyKinds { get; private set; }
        = new Dictionary<string, ConceptDependencyKind>(StringComparer.Ordinal);

    internal void SetDependencyKinds(IReadOnlyDictionary<string, ConceptDependencyKind> kinds) =>
        DependencyKinds = kinds;
}
