namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Origen normativo de una <see cref="PayrollLine"/> (ALAHIA-PE-01).
/// </summary>
public enum PayrollLineOrigin
{
    Assignment = 0,
    Fact = 1,
    Rule = 2
}
