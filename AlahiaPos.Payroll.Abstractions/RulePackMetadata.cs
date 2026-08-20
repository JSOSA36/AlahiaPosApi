namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Metadata inmutable de un RulePack (ALAHIA-PE-01).
/// El Core no interpreta <see cref="JurisdictionCode"/> para ramificar lógica.
/// </summary>
public sealed record RulePackMetadata(
    string PackId,
    string JurisdictionCode,
    string Version,
    MoneyPolicy DefaultMoneyPolicy,
    IReadOnlyDictionary<string, string> Parameters);
