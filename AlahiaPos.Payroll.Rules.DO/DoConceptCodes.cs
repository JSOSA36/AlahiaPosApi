namespace AlahiaPos.Payroll.Rules.DO;

/// <summary>ConceptCodes estables del pack DO (contrato público — ALAHIA-PE-01 D15).</summary>
public static class DoConceptCodes
{
    public const string SueldoBase = "SUELDO_BASE";
    public const string HorasExtra = "HORAS_EXTRA";
    public const string AfpEmpleado = "AFP_EMPLEADO";
    public const string SfsEmpleado = "SFS_EMPLEADO";
    public const string IsrEmpleado = "ISR_EMPLEADO";
    public const string AfpPatronal = "AFP_PATRONAL";
    public const string SfsPatronal = "SFS_PATRONAL";
    public const string InfotepPatronal = "INFOTEP_PATRONAL";
    public const string Neto = "NETO";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        SueldoBase,
        HorasExtra,
        AfpEmpleado,
        SfsEmpleado,
        IsrEmpleado,
        AfpPatronal,
        SfsPatronal,
        InfotepPatronal,
        Neto
    };

    public static IReadOnlyList<string> EmployeeDeductions { get; } = new[]
    {
        AfpEmpleado,
        SfsEmpleado,
        IsrEmpleado
    };

    public static IReadOnlyList<string> Incomes { get; } = new[]
    {
        SueldoBase,
        HorasExtra
    };
}
