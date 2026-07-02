using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using WebServer.Models;
using WebServer.Models.Dtos;

namespace WebServer.Data;

/// <summary>
/// Reads buildings from the Excel seed file and inserts them into the database.
/// Uses only BCL (ZipArchive + LINQ to XML) to avoid extra dependencies.
/// </summary>
public static class BuildingsExcelImporter
{
    private static readonly IReadOnlyDictionary<string, BuildingStatus> ShikumStatusByLabel =
        new Dictionary<string, BuildingStatus>(StringComparer.Ordinal)
        {
            { "מיפוי החסמים וגיבוש פתרון", BuildingStatus.MappingBarriersAndSolution },
            { "העברת בעלות", BuildingStatus.OwnershipTransfer },
            { "חסמים המונעים פיתוח", BuildingStatus.DevelopmentBarriers },
            { "הבעלים בוחן אפיק פעולה לשיקום", BuildingStatus.OwnerConsideringAction },
            { "הכנת תכנית שיקום", BuildingStatus.PreparingRehabPlan },
            { "תכנית מאושרת, הכנה לביצוע", BuildingStatus.PlanApprovedPreparingExecution },
            { "בביצוע", BuildingStatus.InExecution },
            { "הליך אכלוס", BuildingStatus.OccupancyProcess }
        };

    private static readonly IReadOnlyDictionary<string, string> HeaderAliases =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // Excel header -> canonical FieldSpec.FieldName (from Data.csv)
            { "תמונת מצב", "תמצית מצב" },
            { "תאריך עדכון סטטוס", "תאריך עדכון תמצית מצב" },
            { "אחוז המבנה שעומד ניזוק", "אחוז המבנה שמוגדר ניזוק" },
            { "שטח החלקה (מ\"ר)", "שטח החלקה (מ״ר)" },
            { "סה\"כ זכויות בניה מאושרות (מ\"ר)", "סה\"כ זכויות בניה מאושרות (מ״ר)" },
            { "סה\"כ שטח בנוי (מ\"ר)", "סה\"כ שטח בנוי (מ״ר)" },
            { "פרטי מחזיק", "פרטי מחזיקים" },
            { "צריכת מים ב-6 החודשים האחרונים", "האם הייתה צריכת מים ב־6 החודשים האחרונים" },
            { "צריכת חשמל ב-6 החודשים האחרונים", "האם הייתה צריכת חשמל ב־6 החודשים האחרונים" },
            { "א\"ס", "אזור סטטיסטי" },
            { "ID נכס לצורך מערכת זו בלבד", "ID" },
            { "ציון", "ציון עמידה בסטנדרט" }
        };

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SelectLabelAliasesByTable =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            // Excel label -> canonical SelectTables label
            ["Tbl_SugBaalut"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "ממשלתי", "ממשלה" },
                { "עיריית חיפה", "רשות מקומית" },
                { "פרטי", "פרטי (בודד)" }
            }
        };

    public static async Task SeedFromFileAsync(AppDbContext context, string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[BuildingsExcelImporter] Seed file not found at '{filePath}', skipping building seeding.");
                return;
            }

            using var stream = File.OpenRead(filePath);
            var buildings = ReadBuildingsFromStream(stream, out _);

            Console.WriteLine($"[BuildingsExcelImporter] Parsed {buildings.Count} building rows from '{filePath}'.");

            if (buildings.Count == 0)
            {
                Console.WriteLine($"[BuildingsExcelImporter] No usable building rows found in '{filePath}', skipping building seeding.");
                return;
            }

            var streetLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var streets = await context.Streets.AsNoTracking().ToListAsync(cancellationToken);
            foreach (var street in streets)
            {
                var name = street.Name?.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                // Keep the first StreetId encountered for a given name; skip duplicates with the same name.
                if (!streetLookup.ContainsKey(name))
                {
                    streetLookup[name] = street.StreetId;
                }
            }

            foreach (var building in buildings)
            {
                if (building.StreetCode.HasValue)
                {
                    continue;
                }

                var streetName = building.StreetName?.Trim();
                if (string.IsNullOrEmpty(streetName))
                {
                    continue;
                }

                if (streetLookup.TryGetValue(streetName, out var sid))
                {
                    building.StreetCode = sid;
                    building.StreetName = streetName;
                }
            }

            await context.Buildings.AddRangeAsync(buildings, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            Console.WriteLine($"[BuildingsExcelImporter] Seeded {buildings.Count} buildings from '{filePath}'.");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"[BuildingsExcelImporter] Error while seeding buildings from '{filePath}': {ex}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"[BuildingsExcelImporter] Error while seeding buildings from '{filePath}': {ex}");
        }
        catch (InvalidDataException ex)
        {
            Console.WriteLine($"[BuildingsExcelImporter] Error while seeding buildings from '{filePath}': {ex}");
        }
        catch (XmlException ex)
        {
            Console.WriteLine($"[BuildingsExcelImporter] Error while seeding buildings from '{filePath}': {ex}");
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"[BuildingsExcelImporter] Error while seeding buildings from '{filePath}': {ex}");
        }
    }

    public static IReadOnlyList<Building> ReadBuildingsFromStream(Stream stream, out string? error)
    {
        error = null;
        try
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            return ReadBuildingsFromArchive(archive, ref error);
        }
        catch (InvalidDataException ex)
        {
            error = $"Failed to read buildings Excel: {ex.Message}";
            return Array.Empty<Building>();
        }
        catch (IOException ex)
        {
            error = $"Failed to read buildings Excel: {ex.Message}";
            return Array.Empty<Building>();
        }
        catch (XmlException ex)
        {
            error = $"Failed to read buildings Excel: {ex.Message}";
            return Array.Empty<Building>();
        }
    }

    private static List<Building> ReadBuildingsFromArchive(ZipArchive archive, ref string? error)
    {

        var sharedStrings = ReadSharedStrings(archive);
        var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml");
        if (sheetEntry == null)
        {
            error = "Worksheet 'sheet1.xml' not found in Excel file.";
            return new List<Building>();
        }

        using var sheetStream = sheetEntry.Open();
        var sheetDoc = XDocument.Load(sheetStream);

        var ns = sheetDoc.Root?.GetDefaultNamespace() ?? XNamespace.None;
        var sheetDataElement = sheetDoc.Root?.Element(ns + "sheetData");
        if (sheetDataElement == null)
        {
            error = "<sheetData> not found in worksheet.";
            return new List<Building>();
        }

        var rows = sheetDataElement.Elements(ns + "row").ToList();
        if (rows.Count == 0)
        {
            error = "Worksheet has no rows.";
            return new List<Building>();
        }

        // In this file row 3 is the header row (row 1+2 are group titles), as indicated by the autoFilter range A3:CF6.
        var headerRow = rows.FirstOrDefault(r => (string?)r.Attribute("r") == "3");
        if (headerRow == null)
        {
            error = "Header row not found in client buildings template.";
            return new List<Building>();
        }

        var headerByColumnIndex = BuildHeaderMap(headerRow, sharedStrings, ns);
        if (headerByColumnIndex.Count == 0)
        {
            error = "Client buildings headers do not match the expected template.";
            return new List<Building>();
        }

        var propertyByHeader = BuildPropertyMap(headerByColumnIndex.Values);

        var result = new List<Building>();

        foreach (var row in rows)
        {
            var rValue = (string?)row.Attribute("r");
            if (!int.TryParse(rValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rowNumber))
            {
                continue;
            }

            // Data rows start at row 4.
            if (rowNumber < 4)
            {
                continue;
            }

            var building = new Building();
            var setCount = 0;

            foreach (var cell in row.Elements(ns + "c"))
            {
                var cellRef = (string?)cell.Attribute("r");
                if (string.IsNullOrWhiteSpace(cellRef))
                {
                    continue;
                }

                var colIndex = GetColumnIndex(cellRef);
                if (!headerByColumnIndex.TryGetValue(colIndex, out var headerText))
                {
                    continue;
                }

                headerText = headerText.Trim();
                if (headerText.Length == 0)
                {
                    continue;
                }

                if (!propertyByHeader.TryGetValue(headerText, out var property))
                {
                    continue;
                }

                var raw = ReadCellText(cell, sharedStrings, ns)?.Trim();
                if (string.IsNullOrEmpty(raw))
                {
                    continue;
                }

                var value = ConvertCellValue(raw, property);
                if (value == null)
                {
                    continue;
                }

                property.SetValue(building, value);
                setCount++;
            }

            if (setCount == 0 || IsBuildingEmpty(building))
            {
                continue;
            }

            result.Add(building);
        }

        return result;
    }

    private static Dictionary<int, string> BuildHeaderMap(XElement headerRow, List<string> sharedStrings, XNamespace ns)
    {
        var headers = new Dictionary<int, string>();

        foreach (var cell in headerRow.Elements(ns + "c"))
        {
            var cellRef = (string?)cell.Attribute("r");
            if (string.IsNullOrWhiteSpace(cellRef))
            {
                continue;
            }

            var colIndex = GetColumnIndex(cellRef);
            var text = ReadCellText(cell, sharedStrings, ns);
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            text = text.Trim();
            if (HeaderAliases.TryGetValue(text, out var canonical))
            {
                text = canonical;
            }

            headers[colIndex] = text;
        }

        return headers;
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null)
        {
            return new List<string>();
        }

        using var stream = entry.Open();
        var doc = XDocument.Load(stream);
        var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

        // Concatenate all <t> nodes inside each <si> to handle rich text.
        return doc.Root?
                   .Elements(ns + "si")
                   .Select(si => string.Concat(si.Descendants(ns + "t").Select(t => (string?)t ?? string.Empty)))
                   .ToList()
               ?? new List<string>();
    }

    private static string? ReadCellText(XElement cell, List<string> sharedStrings, XNamespace ns)
    {
        var vElement = cell.Element(ns + "v");
        if (vElement == null)
        {
            return null;
        }

        var raw = (string?)vElement;
        var type = (string?)cell.Attribute("t");

        if (string.Equals(type, "s", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) &&
                index >= 0 && index < sharedStrings.Count)
            {
                return sharedStrings[index];
            }

            return null;
        }

        // For numeric / date / boolean cells Excel stores the raw value here.
        return raw;
    }

    private static Dictionary<string, PropertyInfo> BuildPropertyMap(IEnumerable<string> headerTexts)
    {
        var uniqueHeaders = new HashSet<string>(headerTexts.Where(h => !string.IsNullOrWhiteSpace(h)).Select(h => h.Trim()),
            StringComparer.Ordinal);

        var result = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);
        var buildingType = typeof(Building);

        foreach (var property in buildingType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanWrite)
            {
                continue;
            }

            var fieldSpec = property.GetCustomAttribute<FieldSpecAttribute>();
            var fieldName = fieldSpec?.FieldName?.Trim();

            if (string.IsNullOrEmpty(fieldName))
            {
                continue;
            }

            if (!uniqueHeaders.Contains(fieldName))
            {
                continue;
            }

            // Map using the exact FieldName from the CSV / Excel header.
            result[fieldName] = property;
        }

        return result;
    }

    private static int GetColumnIndex(string cellReference)
    {
        // Convert Excel column letters (e.g., "A", "Z", "AA") to a zero-based index.
        var i = 0;
        while (i < cellReference.Length && !char.IsDigit(cellReference[i]))
        {
            i++;
        }

        var letters = cellReference[..i].ToUpperInvariant();

        var index = 0;
        foreach (var ch in letters)
        {
            index = (index * 26) + (ch - 'A' + 1);
        }

        return index - 1;
    }

    private static object? ConvertCellValue(string raw, PropertyInfo property)
    {
        var targetType = property.PropertyType;
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying == typeof(string))
        {
            return raw;
        }

        if (underlying == typeof(int))
        {
            var normalizedRaw = raw.Trim().Replace("\"\"", "\"");
            if (int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var i))
            {
                if (property.Name == nameof(Building.DamagePercentage) && i is >= 0 and <= 1)
                {
                    // Excel percentage cells can show 100% as "1" (stored as a fraction).
                    return i * 100;
                }

                return i;
            }

            if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            {
                if (property.Name == nameof(Building.DamagePercentage))
                {
                    // Excel percentage cells are stored as fractions (e.g., 0.873... = 87.3%).
                    if (d is >= 0 and <= 1)
                    {
                        return (int)Math.Round(d * 100, MidpointRounding.AwayFromZero);
                    }

                    return (int)Math.Round(d, MidpointRounding.AwayFromZero);
                }

                return (int)d;
            }

            var fieldSpec = property.GetCustomAttribute<FieldSpecAttribute>();
            var selectTableName = fieldSpec?.SelectTableName?.Trim();
            if (!string.IsNullOrWhiteSpace(selectTableName))
            {
                var option = SelectTables
                    .GetOptions(selectTableName)
                    .FirstOrDefault(o => string.Equals(o.Label?.Trim().Replace("\"\"", "\""), normalizedRaw, StringComparison.Ordinal));

                if (option != null)
                {
                    return option.Value;
                }

                if (SelectLabelAliasesByTable.TryGetValue(selectTableName, out var aliases) &&
                    aliases.TryGetValue(normalizedRaw, out var canonicalLabel))
                {
                    option = SelectTables
                        .GetOptions(selectTableName)
                        .FirstOrDefault(o => string.Equals(o.Label?.Trim().Replace("\"\"", "\""), canonicalLabel, StringComparison.Ordinal));

                    if (option != null)
                    {
                        return option.Value;
                    }
                }
            }

            return null;
        }

        if (underlying == typeof(double))
        {
            if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            {
                return d;
            }

            return null;
        }

        if (underlying == typeof(decimal))
        {
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec))
            {
                return dec;
            }

            if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            {
                return Convert.ToDecimal(d);
            }

            return null;
        }

        if (underlying == typeof(Money))
        {
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec))
            {
                return new Money(dec);
            }

            if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            {
                return new Money(Convert.ToDecimal(d));
            }

            return null;
        }

        if (underlying == typeof(DateTime))
        {
            if (TryParseCommonDateString(raw, out var dt))
            {
                return dt;
            }

            if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) &&
                d >= -657435.0 &&
                d <= 2958465.99999999)
            {
                return DateTime.FromOADate(d);
            }

            return null;
        }

        if (underlying.IsEnum)
        {
            if (underlying == typeof(BuildingStatus))
            {
                if (int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var statusInt) &&
                    Enum.IsDefined(underlying, statusInt))
                {
                    return (BuildingStatus)statusInt;
                }

                if (ShikumStatusByLabel.TryGetValue(raw, out var status))
                {
                    return status;
                }

                // Fall through to generic enum parsing as a last resort.
            }

            if (int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var enumInt) &&
                Enum.IsDefined(underlying, enumInt))
            {
                return Enum.ToObject(underlying, enumInt);
            }

            try
            {
                var parsed = Enum.Parse(underlying, raw, ignoreCase: true);
                return parsed;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        return null;
    }

    private static bool TryParseCommonDateString(string raw, out DateTime dt)
    {
        raw = raw.Trim();

        // Common Israeli/Excel formats from the seed file (e.g., "22.12.2024", "22.1.25").
        var formats = new[]
        {
            "d.M.yyyy",
            "dd.MM.yyyy",
            "d.M.yy",
            "dd.MM.yy",
            "d/M/yyyy",
            "dd/MM/yyyy",
            "d/M/yy",
            "dd/MM/yy",
            "yyyy-MM-dd"
        };

        if (DateTime.TryParseExact(raw, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
        {
            return true;
        }

        // Last resort: try parsing with he-IL and invariant culture.
        if (DateTime.TryParse(raw, CultureInfo.GetCultureInfo("he-IL"), DateTimeStyles.None, out dt))
        {
            return true;
        }

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
        {
            return true;
        }

        return false;
    }

    private static IReadOnlyList<BuildingFieldDto> BuildFieldsSnapshot(Building building)
    {
        return typeof(Building)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead)
            .Where(p =>
            {
                if (p.Name is nameof(Building.Street))
                {
                    return false;
                }

                if (p.Name is nameof(Building.Neighborhood) or nameof(Building.FldId))
                {
                    return false;
                }

                if (typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType) &&
                    p.PropertyType != typeof(string))
                {
                    return false;
                }

                return p.GetCustomAttribute<ColumnAttribute>() is not null;
            })
            .Select(p =>
            {
                var columnAttribute = p.GetCustomAttribute<ColumnAttribute>();
                var columnName = string.IsNullOrWhiteSpace(columnAttribute?.Name) ? p.Name : columnAttribute!.Name!;
                var fieldSpec = p.GetCustomAttribute<FieldSpecAttribute>();
                var displayAttribute = p.GetCustomAttribute<DisplayAttribute>();

                var category = fieldSpec?.Category ?? "כללי";
                var fieldName = fieldSpec?.FieldName ?? displayAttribute?.Name ?? p.Name;
                var selectTableName = fieldSpec?.SelectTableName;
                var includeInEventLog = fieldSpec?.IncludeInEventLog ?? false;

                var raw = p.GetValue(building);
                int? rawInt = raw switch
                {
                    null => null,
                    int i => i,
                    BuildingStatus s => (int)s,
                    _ => null
                };

                string? value = null;
                if (raw is null)
                {
                    value = null;
                }
                else if (!string.IsNullOrWhiteSpace(selectTableName) && rawInt.HasValue)
                {
                    var label = SelectTables
                        .GetOptions(selectTableName)
                        .FirstOrDefault(o => o.Value == rawInt.Value)
                        ?.Label;
                    value = label ?? raw.ToString();
                }
                else if (raw is DateTime dtValue)
                {
                    value = dtValue.ToString("yyyy-MM-dd");
                }
                else if (raw is DateTimeOffset dto)
                {
                    value = dto.ToString("O");
                }
                else
                {
                    value = raw.ToString();
                }

                return new BuildingFieldDto(
                    category,
                    fieldName,
                    columnName,
                    selectTableName,
                    includeInEventLog,
                    value,
                    rawInt);
            })
            .OrderBy(f => f.Category)
            .ThenBy(f => f.FieldName)
            .ToList();
    }

    private static bool IsBuildingEmpty(Building building)
    {
        if (building.Id > 0)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(building.StreetName))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(building.BuildingName))
        {
            return false;
        }

        return true;
    }
}
