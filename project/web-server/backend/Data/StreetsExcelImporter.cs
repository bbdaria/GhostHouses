using System.Globalization;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using WebServer.Models;

namespace WebServer.Data;

/// <summary>
/// Reads streets from the second worksheet (sheet2) of the seed Excel file.
/// Expected headers (row 3): "שם רחוב *" (street name) in column A, "מזהה רחוב *" (street id) in column B.
/// </summary>
public static class StreetsExcelImporter
{
    public static IReadOnlyList<Street> ReadStreetsFromStream(Stream stream, out string? error)
    {
        error = null;
        try
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            return ReadStreetsFromArchive(archive, ref error);
        }
        catch (InvalidDataException ex)
        {
            error = $"Failed to read streets Excel: {ex.Message}";
            return Array.Empty<Street>();
        }
        catch (IOException ex)
        {
            error = $"Failed to read streets Excel: {ex.Message}";
            return Array.Empty<Street>();
        }
        catch (XmlException ex)
        {
            error = $"Failed to read streets Excel: {ex.Message}";
            return Array.Empty<Street>();
        }
    }

    public static async Task SeedFromFileAsync(AppDbContext context, string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[StreetsExcelImporter] Seed file not found at '{filePath}', skipping street seeding.");
                return;
            }

            using var stream = File.OpenRead(filePath);
            var streets = ReadStreetsFromStream(stream, out _);
            if (streets.Count == 0)
            {
                Console.WriteLine($"[StreetsExcelImporter] No streets found in '{filePath}', skipping street seeding.");
                return;
            }

            var existingIds = context.Streets.Select(s => s.StreetId).ToHashSet();
            var newStreets = streets.Where(s => !existingIds.Contains(s.StreetId)).ToList();

            if (newStreets.Count == 0)
            {
                Console.WriteLine("[StreetsExcelImporter] No new streets to seed.");
                return;
            }

            await context.Streets.AddRangeAsync(newStreets, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            Console.WriteLine($"[StreetsExcelImporter] Seeded {newStreets.Count} streets from '{filePath}'.");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"[StreetsExcelImporter] Error while seeding streets from '{filePath}': {ex}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"[StreetsExcelImporter] Error while seeding streets from '{filePath}': {ex}");
        }
        catch (InvalidDataException ex)
        {
            Console.WriteLine($"[StreetsExcelImporter] Error while seeding streets from '{filePath}': {ex}");
        }
        catch (XmlException ex)
        {
            Console.WriteLine($"[StreetsExcelImporter] Error while seeding streets from '{filePath}': {ex}");
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"[StreetsExcelImporter] Error while seeding streets from '{filePath}': {ex}");
        }
    }

    private static List<Street> ReadStreetsFromArchive(ZipArchive archive, ref string? error)
    {
        var sharedStrings = ReadSharedStrings(archive);
        var sheetEntry = archive.GetEntry("xl/worksheets/sheet2.xml");
        if (sheetEntry == null)
        {
            error = "Worksheet 'sheet2.xml' not found in Excel file.";
            return new List<Street>();
        }

        using var sheetStream = sheetEntry.Open();
        var sheetDoc = XDocument.Load(sheetStream);

        var ns = sheetDoc.Root?.GetDefaultNamespace() ?? XNamespace.None;
        var sheetDataElement = sheetDoc.Root?.Element(ns + "sheetData");
        if (sheetDataElement == null)
        {
            error = "<sheetData> not found in worksheet.";
            return new List<Street>();
        }

        var rows = sheetDataElement.Elements(ns + "row").ToList();
        if (rows.Count == 0)
        {
            error = "Worksheet has no rows.";
            return new List<Street>();
        }

        // Header is row 3.
        var headerRow = rows.FirstOrDefault(r => (string?)r.Attribute("r") == "3");
        if (headerRow == null)
        {
            error = "Header row not found in client streets template.";
            return new List<Street>();
        }

        var headerMap = BuildHeaderMap(headerRow, sharedStrings, ns);
        if (!headerMap.ContainsValue("שם רחוב *") || !headerMap.ContainsValue("מזהה רחוב *"))
        {
            error = "Client streets headers do not match the expected template.";
            return new List<Street>();
        }

        var result = new List<Street>();

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

            string? name = null;
            int? streetId = null;

            foreach (var cell in row.Elements(ns + "c"))
            {
                var cellRef = (string?)cell.Attribute("r");
                if (string.IsNullOrWhiteSpace(cellRef))
                {
                    continue;
                }

                var colIndex = GetColumnIndex(cellRef);
                if (!headerMap.TryGetValue(colIndex, out var headerText))
                {
                    continue;
                }

                var raw = ReadCellText(cell, sharedStrings, ns)?.Trim();
                if (string.IsNullOrEmpty(raw))
                {
                    continue;
                }

                if (headerText == "שם רחוב *")
                {
                    name = raw;
                }
                else if (headerText == "מזהה רחוב *" &&
                         int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var sid))
                {
                    streetId = sid;
                }
            }

            if (!streetId.HasValue || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            result.Add(new Street
            {
                StreetId = streetId.Value,
                Name = name
            });
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

        return raw;
    }

    private static int GetColumnIndex(string cellReference)
    {
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
}
