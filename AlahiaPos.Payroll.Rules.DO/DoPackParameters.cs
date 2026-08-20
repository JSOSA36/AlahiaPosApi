namespace AlahiaPos.Payroll.Rules.DO;

/// <summary>
/// Claves y defaults versionados de DO-2026.01.
/// Tasas operativas de ejemplo — no oficiales hasta validación TSS/DGII.
/// </summary>
public static class DoPackParameters
{
    public const string PackId = "DO-2026.01";
    public const string JurisdictionCode = "DO";
    public const string Version = "2026.01";

    public const string AfpEmployeeRate = "AFP_EMPLOYEE_RATE";
    public const string SfsEmployeeRate = "SFS_EMPLOYEE_RATE";
    public const string AfpEmployerRate = "AFP_EMPLOYER_RATE";
    public const string SfsEmployerRate = "SFS_EMPLOYER_RATE";
    public const string InfotepRate = "INFOTEP_RATE";
    public const string IsrTableVersion = "ISR_TABLE_VERSION";
    public const string OvertimeIncludedInTssBase = "OVERTIME_INCLUDED_IN_TSS_BASE";
    public const string OvertimeIncludedInIsrBase = "OVERTIME_INCLUDED_IN_ISR_BASE";

    /// <summary>Versión de tabla ISR embebida en este pack.</summary>
    public const string DefaultIsrTableVersion = "DO-2026.01-SIMPLE";

    public static IReadOnlyDictionary<string, string> DefaultParameters { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // Tasas ilustrativas (fracción decimal). Ajustables por versión de pack.
            [AfpEmployeeRate] = "0.0287",
            [SfsEmployeeRate] = "0.0304",
            [AfpEmployerRate] = "0.0710",
            [SfsEmployerRate] = "0.0709",
            [InfotepRate] = "0.0100",
            [IsrTableVersion] = DefaultIsrTableVersion,
            [OvertimeIncludedInTssBase] = "true",
            [OvertimeIncludedInIsrBase] = "true"
        };
}
