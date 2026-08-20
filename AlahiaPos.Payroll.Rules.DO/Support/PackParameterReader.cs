using System.Globalization;
using AlahiaPos.Payroll.Abstractions;

namespace AlahiaPos.Payroll.Rules.DO.Support;

public static class PackParameterReader
{
    public static decimal RequireRate(EvaluationContext context, string key)
    {
        if (!context.PackParameters.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
            throw new DoRulePackException($"Pack parameter '{key}' is required.");

        if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate))
            throw new DoRulePackException($"Pack parameter '{key}' is not a valid decimal: '{raw}'.");

        if (rate < 0m)
            throw new DoRulePackException($"Pack parameter '{key}' must be >= 0.");

        return rate;
    }

    public static string RequireString(EvaluationContext context, string key)
    {
        if (!context.PackParameters.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
            throw new DoRulePackException($"Pack parameter '{key}' is required.");
        return raw;
    }

    public static bool GetBool(EvaluationContext context, string key, bool defaultValue = false)
    {
        if (!context.PackParameters.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
            return defaultValue;

        if (bool.TryParse(raw, out var b))
            return b;

        if (raw == "1" || string.Equals(raw, "yes", StringComparison.OrdinalIgnoreCase))
            return true;
        if (raw == "0" || string.Equals(raw, "no", StringComparison.OrdinalIgnoreCase))
            return false;

        throw new DoRulePackException($"Pack parameter '{key}' is not a valid boolean: '{raw}'.");
    }
}
