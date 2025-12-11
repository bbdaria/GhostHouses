using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Xml.Linq;
using WebServer.Models;

namespace WebServer.Data;

/// <summary>
/// Reads buildings from the Excel seed file and inserts them into the database.
/// Uses only BCL (ZipArchive + LINQ to XML) to avoid extra dependencies.
/// </summary>
public static class BuildingsExcelImporter
{
    public static async Task SeedFromFileAsync(AppDbContext context, string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[BuildingsExcelImporter] Seed file not found at '{filePath}', skipping building seeding.");
                return;
            }

            var buildings = ReadBuildingsFromExcel(filePath);

            if (buildings.Count == 0)
            {
                Console.WriteLine($"[BuildingsExcelImporter] No usable building rows found in '{filePath}', skipping building seeding.");
                return;
            }

            await context.Buildings.AddRangeAsync(buildings, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            Console.WriteLine($"[BuildingsExcelImporter] Seeded {buildings.Count} buildings from '{filePath}'.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BuildingsExcelImporter] Error while seeding buildings from '{filePath}': {ex}");
        }
    }

    private static List<Building> ReadBuildingsFromExcel(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);

        var sharedStrings = ReadSharedStrings(archive);
        var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml");
        if (sheetEntry == null)
        {
            Console.WriteLine("[BuildingsExcelImporter] Worksheet 'sheet1.xml' not found in Excel file.");
            return new List<Building>();
        }

        using var sheetStream = sheetEntry.Open();
        var sheetDoc = XDocument.Load(sheetStream);

        var ns = sheetDoc.Root?.GetDefaultNamespace() ?? XNamespace.None;
        var sheetDataElement = sheetDoc.Root?.Element(ns + "sheetData");
        if (sheetDataElement == null)
        {
            Console.WriteLine("[BuildingsExcelImporter] <sheetData> not found in worksheet.");
            return new List<Building>();
        }

        var rows = sheetDataElement.Elements(ns + "row").ToList();
        if (rows.Count == 0)
        {
            Console.WriteLine("[BuildingsExcelImporter] Worksheet has no rows.");
            return new List<Building>();
        }

        // In this file row 3 is the header row (row 1+2 are group titles), as indicated by the autoFilter range A3:CF6.
        var headerRow = rows.FirstOrDefault(r => (string?)r.Attribute("r") == "3");
        if (headerRow == null)
        {
            Console.WriteLine("[BuildingsExcelImporter] Header row (r=\"3\") not found.");
            return new List<Building>();
        }

        var headerByColumnIndex = BuildHeaderMap(headerRow, sharedStrings, ns);
        if (headerByColumnIndex.Count == 0)
        {
            Console.WriteLine("[BuildingsExcelImporter] Could not build header map from Excel sheet.");
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

                var value = ConvertCellValue(raw, property.PropertyType);
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

            headers[colIndex] = text.Trim();
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

    private static object? ConvertCellValue(string raw, Type targetType)
    {
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

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

            if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            {
                return (int)d;
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
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                return dt;
            }

            if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            {
                try
                {
                    return DateTime.FromOADate(d);
                }
                catch
                {
                    // Ignore invalid OADate values.
                }
            }

            return null;
        }

        if (underlying.IsEnum)
        {
            if (int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var enumInt))
            {
                if (Enum.IsDefined(underlying, enumInt))
                {
                    return Enum.ToObject(underlying, enumInt);
                }
            }

            try
            {
                var parsed = Enum.Parse(underlying, raw, ignoreCase: true);
                return parsed;
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static bool IsBuildingEmpty(Building building)
    {
        if (building.FldId.HasValue)
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

