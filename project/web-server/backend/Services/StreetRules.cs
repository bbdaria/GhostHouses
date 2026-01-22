using System.Globalization;

namespace WebServer.Services;

public static class StreetRules
{
    public const int ReservedNoStreetId = -1;

    public sealed record ValidationIssue(string Field, string Message);
    public sealed record ValidationResult(
        int? StreetId,
        string? Name,
        List<string> MissingRequired,
        List<ValidationIssue> InvalidValues);

    public static int? TryParseStreetId(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (!int.TryParse(raw.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        {
            return null;
        }

        if (value <= 0 || value == ReservedNoStreetId)
        {
            return null;
        }

        return value;
    }

    public static ValidationResult ValidateValues(string? streetIdRaw, string? nameRaw, bool requireId)
    {
        var missing = new List<string>();
        var invalid = new List<ValidationIssue>();

        int? streetId = null;
        if (string.IsNullOrWhiteSpace(streetIdRaw))
        {
            if (requireId)
            {
                missing.Add("StreetId");
            }
        }
        else if (!int.TryParse(streetIdRaw.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedId))
        {
            invalid.Add(new ValidationIssue("StreetId", "מזהה רחוב חייב להיות מספר."));
        }
        else if (parsedId <= 0)
        {
            invalid.Add(new ValidationIssue("StreetId", "מזהה רחוב חייב להיות מספר חיובי."));
        }
        else if (parsedId == ReservedNoStreetId)
        {
            invalid.Add(new ValidationIssue("StreetId", "מזהה רחוב שמור ל\"ללא שם רחוב\" ואינו תקין ברחובות."));
        }
        else
        {
            streetId = parsedId;
        }

        var name = nameRaw?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            missing.Add("Name");
        }

        return new ValidationResult(streetId, string.IsNullOrWhiteSpace(name) ? null : name, missing, invalid);
    }
}
