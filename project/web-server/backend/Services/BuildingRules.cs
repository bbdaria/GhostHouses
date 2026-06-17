using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Reflection;
using WebServer.Data;
using WebServer.Models;

namespace WebServer.Services;

public static class BuildingRules
{
    public sealed record ValidationIssue(string ColumnName, string Message);

    public sealed record InvalidFieldValue(string Message);

    public static int? ResolveRehabSivugValue()
    {
        return SelectTables
            .GetOptions("Tbl_Sivug")
            .FirstOrDefault(option =>
                string.Equals(option.Label, "ריק ובהליך שיקום", StringComparison.Ordinal) ||
                (option.Label?.Contains("שיקום", StringComparison.Ordinal) ?? false))
            ?.Value;
    }

    public static int? TryParsePositiveInt(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return int.TryParse(raw.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : null;
    }

    public static int? TryParseStreetId(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return int.TryParse(raw.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    public static int? TryResolveSelectValue(string? raw, string tableName)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (int.TryParse(raw.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
        {
            return SelectTables
                .GetOptions(tableName)
                .Any(option => option.Value == value)
                ? value
                : null;
        }

        var normalizedRaw = raw.Trim().Replace("\"\"", "\"");
        return SelectTables
            .GetOptions(tableName)
            .FirstOrDefault(option =>
                string.Equals(option.Label?.Trim().Replace("\"\"", "\""), normalizedRaw, StringComparison.Ordinal))
            ?.Value;
    }

    public static List<string> GetMissingRequiredColumns(
        IReadOnlyDictionary<string, string?> values,
        int? rehabSivugValue,
        List<string> warnings,
        bool requireId)
    {
        var missing = new List<string>();

        values.TryGetValue("Id", out var idRaw);
        if (!string.IsNullOrWhiteSpace(idRaw))
        {
            if (!TryParsePositiveInt(idRaw).HasValue)
            {
                missing.Add("Id");
                warnings.Add("ID חייב להיות מספר חיובי.");
            }
        }
        else if (requireId)
        {
            missing.Add("Id");
        }

        values.TryGetValue("StreetId", out var streetRaw);
        if (string.IsNullOrWhiteSpace(streetRaw))
        {
            missing.Add("StreetId");
        }
        else if (!TryParseStreetId(streetRaw).HasValue)
        {
            missing.Add("StreetId");
            warnings.Add("קוד רחוב חייב להיות מספר.");
        }

        values.TryGetValue("BldNum", out var houseRaw);
        if (string.IsNullOrWhiteSpace(houseRaw))
        {
            missing.Add("BldNum");
        }

        values.TryGetValue("BldName", out var nameRaw);
        if (string.IsNullOrWhiteSpace(nameRaw))
        {
            missing.Add("BldName");
        }

        values.TryGetValue("BldSivug", out var sivugRaw);
        if (string.IsNullOrWhiteSpace(sivugRaw))
        {
            missing.Add("BldSivug");
        }

        var sivugValue = TryResolveSelectValue(sivugRaw, "Tbl_Sivug");
        if (!string.IsNullOrWhiteSpace(sivugRaw) && !sivugValue.HasValue)
        {
            missing.Add("BldSivug");
            warnings.Add("ערך סיווג אינו חוקי.");
        }

        if (rehabSivugValue.HasValue && sivugValue == rehabSivugValue.Value)
        {
            values.TryGetValue("ShikumStatus", out var shikumRaw);
            if (string.IsNullOrWhiteSpace(shikumRaw))
            {
                missing.Add("ShikumStatus");
            }
            else if (!TryResolveSelectValue(shikumRaw, "Tbl_StatusShikum").HasValue)
            {
                missing.Add("ShikumStatus");
                warnings.Add("ערך סטטוס שיקום אינו חוקי.");
            }
        }

        return missing;
    }

    public static List<ValidationIssue> GetInvalidFieldValues(
        IReadOnlyDictionary<string, string?> values,
        IReadOnlyDictionary<string, PropertyInfo> propertyByColumn)
    {
        var invalid = new List<ValidationIssue>();

        foreach (var (columnName, rawValue) in values)
        {
            if (string.IsNullOrWhiteSpace(columnName))
            {
                continue;
            }

            if (string.Equals(columnName, "StreetName", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!propertyByColumn.TryGetValue(columnName, out var property))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                continue;
            }

            var converted = ConvertFieldValue(rawValue, property);
            if (converted is InvalidFieldValue invalidValue)
            {
                invalid.Add(new ValidationIssue(columnName, invalidValue.Message));
            }
        }

        return invalid;
    }

    public static object? ConvertFieldValue(string? raw, PropertyInfo property)
    {
        var targetType = property.PropertyType;
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (string.IsNullOrWhiteSpace(raw))
        {
            if (underlying == typeof(string))
            {
                return string.Empty;
            }

            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) is null)
            {
                return Activator.CreateInstance(targetType);
            }

            return null;
        }

        raw = raw.Trim();

        if (underlying == typeof(string))
        {
            return raw;
        }

        if (underlying == typeof(int))
        {
            if (int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var i))
            {
                return i;
            }

            var fieldSpec = property.GetCustomAttribute<FieldSpecAttribute>();
            var selectTableName = fieldSpec?.SelectTableName?.Trim();
            if (!string.IsNullOrWhiteSpace(selectTableName))
            {
                var normalizedRaw = raw.Replace("\"\"", "\"");
                var option = SelectTables
                    .GetOptions(selectTableName)
                    .FirstOrDefault(o => string.Equals(o.Label?.Trim().Replace("\"\"", "\""), normalizedRaw, StringComparison.Ordinal));
                if (option != null)
                {
                    return option.Value;
                }

                return new InvalidFieldValue("ערך אינו חוקי.");
            }

            return new InvalidFieldValue("ערך חייב להיות מספר.");
        }

        if (underlying == typeof(double))
        {
            if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            {
                return d;
            }

            return new InvalidFieldValue("ערך חייב להיות מספר.");
        }

        if (underlying == typeof(decimal))
        {
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec))
            {
                return dec;
            }

            return new InvalidFieldValue("ערך חייב להיות מספר.");
        }

        if (underlying == typeof(Money))
        {
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
            {
                return new Money(amount);
            }

            return new InvalidFieldValue("ערך חייב להיות מספר.");
        }

        if (underlying == typeof(DateTime))
        {
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                return dt.Date;
            }

            if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var oa))
            {
                try
                {
                    return DateTime.FromOADate(oa).Date;
                }
                catch
                {
                    // ignored
                }
            }

            return new InvalidFieldValue("ערך חייב להיות תאריך.");
        }

        if (underlying.IsEnum)
        {
            if (int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var enumInt))
            {
                try
                {
                    return Enum.ToObject(underlying, enumInt);
                }
                catch
                {
                    return new InvalidFieldValue("ערך אינו חוקי.");
                }
            }

            var fieldSpec = property.GetCustomAttribute<FieldSpecAttribute>();
            var selectTableName = fieldSpec?.SelectTableName?.Trim();
            if (!string.IsNullOrWhiteSpace(selectTableName))
            {
                var normalizedRaw = raw.Replace("\"\"", "\"");
                var option = SelectTables
                    .GetOptions(selectTableName)
                    .FirstOrDefault(o => string.Equals(o.Label?.Trim().Replace("\"\"", "\""), normalizedRaw, StringComparison.Ordinal));
                if (option != null)
                {
                    return Enum.ToObject(underlying, option.Value);
                }

                return new InvalidFieldValue("ערך אינו חוקי.");
            }

            try
            {
                return Enum.Parse(underlying, raw, ignoreCase: true);
            }
            catch
            {
                return new InvalidFieldValue("ערך אינו חוקי.");
            }
        }

        return new InvalidFieldValue("סוג שדה לא נתמך.");
    }
}
