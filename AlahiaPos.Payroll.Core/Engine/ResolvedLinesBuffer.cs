using AlahiaPos.Payroll.Abstractions;

namespace AlahiaPos.Payroll.Core.Engine;

/// <summary>
/// Vista mutable internamente; expuesta a calculators solo como lectura.
/// </summary>
internal sealed class ResolvedLinesBuffer : IResolvedLinesView
{
    private readonly List<PayrollLine> _lines = new();

    public IReadOnlyList<PayrollLine> All => _lines;

    public void AddRange(IEnumerable<PayrollLine> lines) => _lines.AddRange(lines);

    public IReadOnlyList<PayrollLine> ForConcept(string conceptCode) =>
        _lines.Where(l => string.Equals(l.ConceptCode, conceptCode, StringComparison.Ordinal)).ToList();

    public decimal SumAmount(string conceptCode) =>
        ForConcept(conceptCode).Sum(l => l.Amount);

    /// <summary>Vista filtrada solo a un subconjunto de conceptos (dependencias).</summary>
    public IResolvedLinesView ForDependencies(IEnumerable<string> conceptCodes)
    {
        var set = new HashSet<string>(conceptCodes, StringComparer.Ordinal);
        return new FilteredView(_lines, set);
    }

    private sealed class FilteredView : IResolvedLinesView
    {
        private readonly List<PayrollLine> _source;
        private readonly HashSet<string> _codes;

        public FilteredView(List<PayrollLine> source, HashSet<string> codes)
        {
            _source = source;
            _codes = codes;
        }

        public IReadOnlyList<PayrollLine> All =>
            _source.Where(l => _codes.Contains(l.ConceptCode)).ToList();

        public IReadOnlyList<PayrollLine> ForConcept(string conceptCode) =>
            !_codes.Contains(conceptCode)
                ? Array.Empty<PayrollLine>()
                : _source.Where(l => string.Equals(l.ConceptCode, conceptCode, StringComparison.Ordinal)).ToList();

        public decimal SumAmount(string conceptCode) => ForConcept(conceptCode).Sum(l => l.Amount);
    }
}
