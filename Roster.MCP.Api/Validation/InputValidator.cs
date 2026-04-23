using System.Text.RegularExpressions;

namespace Roster.MCP.Api.Validation;

public static partial class InputValidator
{
    private static readonly HashSet<string> ValidCategories =
        new(["chinese", "english", "sundayschool"], StringComparer.OrdinalIgnoreCase);

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}$")]
    private static partial Regex DatePattern();

    /// <summary>
    /// Returns a non-null error string when the category is invalid.
    /// A null category is always valid (it is optional).
    /// </summary>
    public static string? ValidateCategory(string? category)
    {
        if (category is null) return null;
        return ValidCategories.Contains(category)
            ? null
            : $"Invalid category: '{category}'. Must be one of ['chinese','english','sundayschool'].";
    }

    /// <summary>Returns a non-null error string when the date string does not match YYYY-MM-DD.</summary>
    public static string? ValidateDate(string? date, string paramName)
    {
        if (date is null) return null;
        return DatePattern().IsMatch(date)
            ? null
            : $"Invalid date format for '{paramName}': '{date}'. Expected YYYY-MM-DD.";
    }

    /// <summary>Returns a non-null error string when from > to (both must be non-null).</summary>
    public static string? ValidateDateRange(string from, string to)
    {
        // Lexicographic comparison works for YYYY-MM-DD strings.
        return string.Compare(from, to, StringComparison.Ordinal) > 0
            ? $"Invalid date range: 'from' date ({from}) must be before or equal to 'to' date ({to})."
            : null;
    }

    /// <summary>Returns a non-null error string when any required write field is missing.</summary>
    public static string? ValidateWriteRequest(string? date, string? category)
    {
        if (string.IsNullOrWhiteSpace(date))
            return "Field 'date' is required.";
        if (string.IsNullOrWhiteSpace(category))
            return "Field 'category' is required.";
        return null;
    }
}
