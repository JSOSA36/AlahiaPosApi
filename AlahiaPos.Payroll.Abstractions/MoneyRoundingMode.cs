namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Política de redondeo monetaria exigida en el EvaluationContext (D16).
/// </summary>
public enum MoneyRoundingMode
{
    /// <summary>Away from zero / típico contable comercial.</summary>
    AwayFromZero = 0,

    ToEven = 1,
    TowardZero = 2,
    ToPositiveInfinity = 3,
    ToNegativeInfinity = 4
}
