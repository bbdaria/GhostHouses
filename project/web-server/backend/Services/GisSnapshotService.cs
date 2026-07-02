using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using WebServer.Data;
using WebServer.Models;

namespace WebServer.Services;

public interface IGisSnapshotService
{
    Task<byte[]?> CreateBuildingSnapshotAsync(Building building, CancellationToken cancellationToken);
}

public sealed class ArcGisSnapshotService : IGisSnapshotService
{
    private const int SnapshotWidth = 960;
    private const int SnapshotHeight = 540;
    private const int NearbyCandidateLimit = 200;
    private const int NearbyOverlayLimit = 25;
    private const double MinLongitudeSpan = 0.0025;
    private const double MinLatitudeSpan = 0.0016;
    private const string BaseMapExportUrl =
        "https://gisserver.haifa.muni.il/arcgiswebadaptor/rest/services/PublicSite/Haifa_BaseMap_2022/MapServer/export";

    private static readonly Uri RegulatedParcelLayer = new(
        "https://gisserver.haifa.muni.il/arcgiswebadaptor/rest/services/PublicSite/Haifa_Eng_Public/MapServer/5/query");
    private static readonly Uri TaxParcelLayer = new(
        "https://gisserver.haifa.muni.il/arcgiswebadaptor/rest/services/PublicSite/Haifa_Eng_Public/MapServer/4/query");
    private static readonly Uri AddressesLayer = new(
        "https://gisserver.haifa.muni.il/arcgiswebadaptor/rest/services/PublicSite/Haifa_Stat_Public/MapServer/0/query");
    private static readonly Uri PreservationBuildingsLayer = new(
        "https://gisserver.haifa.muni.il/arcgiswebadaptor/rest/services/PublicSite/Haifa_Shimur_Public/MapServer/0/query");

    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;
    private readonly ILogger<ArcGisSnapshotService> _logger;

    public ArcGisSnapshotService(
        HttpClient httpClient,
        AppDbContext context,
        ILogger<ArcGisSnapshotService> logger)
    {
        _httpClient = httpClient;
        _context = context;
        _logger = logger;
    }

    public async Task<byte[]?> CreateBuildingSnapshotAsync(Building building, CancellationToken cancellationToken)
    {
        try
        {
            var resolved = await ResolveGeometryAsync(building, cancellationToken);
            if (resolved is null)
            {
                return null;
            }

            var bbox = BuildSnapshotBounds(resolved.Geometry);
            var mapBytes = await ExportBaseMapAsync(bbox, cancellationToken);
            if (mapBytes is null || mapBytes.Length == 0)
            {
                return null;
            }

            var nearby = await ResolveNearbyDbBuildingGeometriesAsync(building.Id, bbox, cancellationToken);
            return DrawBuildingOverlay(mapBytes, bbox, resolved.Geometry, nearby);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex) { return LogSnapshotFailure(building.Id, ex); }
        catch (JsonException ex) { return LogSnapshotFailure(building.Id, ex); }
        catch (InvalidOperationException ex) { return LogSnapshotFailure(building.Id, ex); }
        catch (IOException ex) { return LogSnapshotFailure(building.Id, ex); }
        catch (NotSupportedException ex) { return LogSnapshotFailure(building.Id, ex); }
        catch (UnknownImageFormatException ex) { return LogSnapshotFailure(building.Id, ex); }
        catch (InvalidImageContentException ex) { return LogSnapshotFailure(building.Id, ex); }
    }

    private byte[]? LogSnapshotFailure(int buildingId, Exception ex)
    {
        _logger.LogWarning(ex, "Failed to create GIS snapshot for building {BuildingId}", buildingId);
        return null;
    }

    private async Task<ResolvedGisGeometry?> ResolveGeometryAsync(Building building, CancellationToken cancellationToken)
    {
        var regulated = await QueryPolygonAsync(
            RegulatedParcelLayer,
            BuildParcelWhere("Gush_Num", building.GushM, "Parcel", building.ParcelM),
            cancellationToken);
        if (regulated is not null) return new ResolvedGisGeometry(regulated, RegulatedParcelLayer, true);

        var tax = await QueryPolygonAsync(
            TaxParcelLayer,
            BuildParcelWhere("GUSH_NO", building.GushS, "PARCEL", building.ParcelS),
            cancellationToken);
        if (tax is not null) return new ResolvedGisGeometry(tax, TaxParcelLayer, true);

        var address = await QueryPointAsync(
            AddressesLayer,
            BuildAddressWhere(building.Street?.Name ?? building.StreetName, building.HouseNumber),
            cancellationToken);
        if (address is not null) return new ResolvedGisGeometry(address, AddressesLayer, false);

        var preservation = await QueryPolygonAsync(
            PreservationBuildingsLayer,
            BuildPreservationWhere(building.Street?.Name ?? building.StreetName, building.HouseNumber),
            cancellationToken);
        if (preservation is not null) return new ResolvedGisGeometry(preservation, PreservationBuildingsLayer, true);

        if (IsInsideHaifaBounds(building.Longitude, building.Latitude))
        {
            return new ResolvedGisGeometry(
                GisGeometry.Point(building.Longitude!.Value, building.Latitude!.Value),
                null,
                false);
        }

        return null;
    }

    private async Task<IReadOnlyList<GisGeometry>> ResolveNearbyDbBuildingGeometriesAsync(
        int buildingId,
        GisBounds bbox,
        CancellationToken cancellationToken)
    {
        var candidates = await _context.Buildings
            .AsNoTracking()
            .Where(building => building.Id != buildingId)
            .Where(building =>
                (building.Longitude.HasValue && building.Latitude.HasValue) ||
                (building.GushM.HasValue && building.ParcelM.HasValue) ||
                (building.GushS.HasValue && building.ParcelS.HasValue) ||
                (building.StreetName != null && building.StreetName != string.Empty &&
                 building.HouseNumber != null && building.HouseNumber != string.Empty))
            .OrderBy(building => building.Id)
            .Take(NearbyCandidateLimit)
            .ToListAsync(cancellationToken);

        var geometries = new List<GisGeometry>();
        foreach (var candidate in candidates)
        {
            var resolved = await ResolveGeometryAsync(candidate, cancellationToken);
            if (resolved is null || !GeometryIntersectsBounds(resolved.Geometry, bbox))
            {
                continue;
            }

            geometries.Add(resolved.Geometry);
            if (geometries.Count >= NearbyOverlayLimit)
            {
                break;
            }
        }

        return geometries;
    }

    private static bool HasDirectCoordinates(Building building) =>
        building.Longitude.HasValue && building.Latitude.HasValue;

    private static bool HasMunicipalParcel(Building building) =>
        building.GushM.HasValue && building.ParcelM.HasValue;

    private static bool HasTaxParcel(Building building) =>
        building.GushS.HasValue && building.ParcelS.HasValue;

    private static bool HasAddress(Building building) =>
        !string.IsNullOrWhiteSpace(building.StreetName) &&
        !string.IsNullOrWhiteSpace(building.HouseNumber);

    private async Task<GisGeometry?> QueryPolygonAsync(Uri layerUri, string? where, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(where))
        {
            return null;
        }

        var feature = await QueryFeatureAsync(layerUri, where, cancellationToken);
        if (feature is null || !feature.Value.TryGetProperty("geometry", out var geometry))
        {
            return null;
        }

        if (!geometry.TryGetProperty("rings", out var ringsElement) || ringsElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var rings = new List<IReadOnlyList<GisPoint>>();
        foreach (var ringElement in ringsElement.EnumerateArray())
        {
            var ring = new List<GisPoint>();
            foreach (var pointElement in ringElement.EnumerateArray())
            {
                if (pointElement.ValueKind != JsonValueKind.Array || pointElement.GetArrayLength() < 2)
                {
                    continue;
                }

                var longitude = pointElement[0].GetDouble();
                var latitude = pointElement[1].GetDouble();
                if (IsInsideHaifaBounds(longitude, latitude))
                {
                    ring.Add(new GisPoint(longitude, latitude));
                }
            }

            if (ring.Count > 1)
            {
                rings.Add(ring);
            }
        }

        return rings.Count > 0 ? GisGeometry.Polygon(rings) : null;
    }

    private async Task<GisGeometry?> QueryPointAsync(Uri layerUri, string? where, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(where))
        {
            return null;
        }

        var feature = await QueryFeatureAsync(layerUri, where, cancellationToken);
        if (feature is null || !feature.Value.TryGetProperty("geometry", out var geometry))
        {
            return null;
        }

        if (!geometry.TryGetProperty("x", out var xElement) ||
            !geometry.TryGetProperty("y", out var yElement))
        {
            return null;
        }

        var longitude = xElement.GetDouble();
        var latitude = yElement.GetDouble();
        return IsInsideHaifaBounds(longitude, latitude) ? GisGeometry.Point(longitude, latitude) : null;
    }

    private async Task<JsonElement?> QueryFeatureAsync(Uri layerUri, string where, CancellationToken cancellationToken)
    {
        var url = BuildQueryUrl(layerUri, new Dictionary<string, string>
        {
            ["f"] = "json",
            ["where"] = where,
            ["returnGeometry"] = "true",
            ["outFields"] = "*",
            ["outSR"] = "4326",
            ["resultRecordCount"] = "1"
        });

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var payload = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (payload.RootElement.TryGetProperty("error", out _))
        {
            return null;
        }

        if (!payload.RootElement.TryGetProperty("features", out var features) ||
            features.ValueKind != JsonValueKind.Array ||
            features.GetArrayLength() == 0)
        {
            return null;
        }

        return features[0].Clone();
    }

    private async Task<IReadOnlyList<GisGeometry>> QueryGeometriesInBoundsAsync(
        Uri layerUri,
        GisBounds bbox,
        bool polygonLayer,
        int limit,
        CancellationToken cancellationToken)
    {
        var geometryText = string.Join(',',
            FormatInvariant(bbox.MinLongitude),
            FormatInvariant(bbox.MinLatitude),
            FormatInvariant(bbox.MaxLongitude),
            FormatInvariant(bbox.MaxLatitude));

        var url = BuildQueryUrl(layerUri, new Dictionary<string, string>
        {
            ["f"] = "json",
            ["where"] = "1=1",
            ["geometry"] = geometryText,
            ["geometryType"] = "esriGeometryEnvelope",
            ["inSR"] = "4326",
            ["spatialRel"] = "esriSpatialRelIntersects",
            ["returnGeometry"] = "true",
            ["outFields"] = "*",
            ["outSR"] = "4326",
            ["resultRecordCount"] = limit.ToString(CultureInfo.InvariantCulture)
        });

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return Array.Empty<GisGeometry>();
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var payload = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (payload.RootElement.TryGetProperty("error", out _))
        {
            return Array.Empty<GisGeometry>();
        }

        if (!payload.RootElement.TryGetProperty("features", out var features) ||
            features.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<GisGeometry>();
        }

        var result = new List<GisGeometry>();
        foreach (var feature in features.EnumerateArray())
        {
            if (!feature.TryGetProperty("geometry", out var geometry))
            {
                continue;
            }

            var parsed = polygonLayer
                ? ParsePolygonGeometry(geometry)
                : ParsePointGeometry(geometry);
            if (parsed is not null)
            {
                result.Add(parsed);
            }
        }

        return result;
    }

    private static GisGeometry? ParsePolygonGeometry(JsonElement geometry)
    {
        if (!geometry.TryGetProperty("rings", out var ringsElement) || ringsElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var rings = new List<IReadOnlyList<GisPoint>>();
        foreach (var ringElement in ringsElement.EnumerateArray())
        {
            var ring = new List<GisPoint>();
            foreach (var pointElement in ringElement.EnumerateArray())
            {
                if (pointElement.ValueKind != JsonValueKind.Array || pointElement.GetArrayLength() < 2)
                {
                    continue;
                }

                var longitude = pointElement[0].GetDouble();
                var latitude = pointElement[1].GetDouble();
                if (IsInsideHaifaBounds(longitude, latitude))
                {
                    ring.Add(new GisPoint(longitude, latitude));
                }
            }

            if (ring.Count > 1)
            {
                rings.Add(ring);
            }
        }

        return rings.Count > 0 ? GisGeometry.Polygon(rings) : null;
    }

    private static GisGeometry? ParsePointGeometry(JsonElement geometry)
    {
        if (!geometry.TryGetProperty("x", out var xElement) ||
            !geometry.TryGetProperty("y", out var yElement))
        {
            return null;
        }

        var longitude = xElement.GetDouble();
        var latitude = yElement.GetDouble();
        return IsInsideHaifaBounds(longitude, latitude) ? GisGeometry.Point(longitude, latitude) : null;
    }

    private async Task<byte[]?> ExportBaseMapAsync(GisBounds bbox, CancellationToken cancellationToken)
    {
        var bboxText = string.Join(',',
            FormatInvariant(bbox.MinLongitude),
            FormatInvariant(bbox.MinLatitude),
            FormatInvariant(bbox.MaxLongitude),
            FormatInvariant(bbox.MaxLatitude));

        var url = BuildQueryUrl(new Uri(BaseMapExportUrl), new Dictionary<string, string>
        {
            ["bbox"] = bboxText,
            ["bboxSR"] = "4326",
            ["imageSR"] = "4326",
            ["size"] = $"{SnapshotWidth},{SnapshotHeight}",
            ["format"] = "png32",
            ["transparent"] = "false",
            ["f"] = "image"
        });

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private static byte[] DrawBuildingOverlay(
        byte[] mapBytes,
        GisBounds bbox,
        GisGeometry targetGeometry,
        IReadOnlyList<GisGeometry> nearbyGeometries)
    {
        using var image = Image.Load<Rgba32>(mapBytes);
        foreach (var nearbyGeometry in nearbyGeometries)
        {
            DrawGeometryOverlay(
                image,
                bbox,
                nearbyGeometry,
                new Rgba32(30, 113, 185, 220),
                new Rgba32(255, 255, 255, 210),
                lineThickness: 3,
                pointRadius: 9);
        }

        DrawGeometryOverlay(
            image,
            bbox,
            targetGeometry,
            new Rgba32(210, 38, 38, 255),
            new Rgba32(255, 255, 255, 230),
            lineThickness: 4,
            pointRadius: 11);

        using var output = new MemoryStream();
        image.Save(output, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
        return output.ToArray();
    }

    private static void DrawGeometryOverlay(
        Image<Rgba32> image,
        GisBounds bbox,
        GisGeometry geometry,
        Rgba32 primary,
        Rgba32 contrast,
        int lineThickness,
        int pointRadius)
    {
        if (geometry.Rings.Count > 0)
        {
            foreach (var ring in geometry.Rings)
            {
                for (var i = 0; i < ring.Count; i++)
                {
                    var current = ToPixel(ring[i], bbox, image.Width, image.Height);
                    var next = ToPixel(ring[(i + 1) % ring.Count], bbox, image.Width, image.Height);
                    DrawLine(image, current, next, primary, lineThickness);
                    DrawLine(image, current, next, contrast, 1);
                }
            }
        }
        else if (geometry.Center is not null)
        {
            var center = ToPixel(geometry.Center.Value, bbox, image.Width, image.Height);
            DrawCircle(image, center.X, center.Y, pointRadius + 5, contrast);
            DrawCircle(image, center.X, center.Y, pointRadius, primary);
            DrawCircle(image, center.X, center.Y, 4, new Rgba32(255, 255, 255, 255));
        }
    }

    private static GisBounds BuildSnapshotBounds(GisGeometry geometry)
    {
        var points = geometry.AllPoints.ToList();
        if (points.Count == 0 && geometry.Center is not null)
        {
            points.Add(geometry.Center.Value);
        }

        var minLon = points.Min(point => point.Longitude);
        var maxLon = points.Max(point => point.Longitude);
        var minLat = points.Min(point => point.Latitude);
        var maxLat = points.Max(point => point.Latitude);

        var lonSpan = Math.Max(maxLon - minLon, MinLongitudeSpan);
        var latSpan = Math.Max(maxLat - minLat, MinLatitudeSpan);
        var centerLon = (minLon + maxLon) / 2;
        var centerLat = (minLat + maxLat) / 2;

        lonSpan *= 1.55;
        latSpan *= 1.55;

        var targetAspect = SnapshotWidth / (double)SnapshotHeight;
        if (lonSpan / latSpan < targetAspect)
        {
            lonSpan = latSpan * targetAspect;
        }
        else
        {
            latSpan = lonSpan / targetAspect;
        }

        return new GisBounds(
            centerLon - lonSpan / 2,
            centerLat - latSpan / 2,
            centerLon + lonSpan / 2,
            centerLat + latSpan / 2);
    }

    private static bool GeometryIntersectsBounds(GisGeometry geometry, GisBounds bbox)
    {
        var points = geometry.AllPoints.ToList();
        if (points.Count == 0)
        {
            return false;
        }

        var minLon = points.Min(point => point.Longitude);
        var maxLon = points.Max(point => point.Longitude);
        var minLat = points.Min(point => point.Latitude);
        var maxLat = points.Max(point => point.Latitude);

        return maxLon >= bbox.MinLongitude &&
               minLon <= bbox.MaxLongitude &&
               maxLat >= bbox.MinLatitude &&
               minLat <= bbox.MaxLatitude;
    }

    private static PixelPoint ToPixel(GisPoint point, GisBounds bbox, int width, int height)
    {
        var x = (point.Longitude - bbox.MinLongitude) / (bbox.MaxLongitude - bbox.MinLongitude) * (width - 1);
        var y = (bbox.MaxLatitude - point.Latitude) / (bbox.MaxLatitude - bbox.MinLatitude) * (height - 1);
        return new PixelPoint((int)Math.Round(x), (int)Math.Round(y));
    }

    private static void DrawCircle(Image<Rgba32> image, int centerX, int centerY, int radius, Rgba32 color)
    {
        var radiusSquared = radius * radius;
        for (var y = centerY - radius; y <= centerY + radius; y++)
        {
            if (y < 0 || y >= image.Height) continue;
            for (var x = centerX - radius; x <= centerX + radius; x++)
            {
                if (x < 0 || x >= image.Width) continue;
                var dx = x - centerX;
                var dy = y - centerY;
                if (dx * dx + dy * dy <= radiusSquared)
                {
                    image[x, y] = Blend(image[x, y], color);
                }
            }
        }
    }

    private static void DrawLine(Image<Rgba32> image, PixelPoint from, PixelPoint to, Rgba32 color, int thickness)
    {
        var dx = Math.Abs(to.X - from.X);
        var dy = Math.Abs(to.Y - from.Y);
        var sx = from.X < to.X ? 1 : -1;
        var sy = from.Y < to.Y ? 1 : -1;
        var err = dx - dy;
        var x = from.X;
        var y = from.Y;
        var radius = Math.Max(0, thickness / 2);

        while (true)
        {
            DrawCircle(image, x, y, radius, color);
            if (x == to.X && y == to.Y) break;
            var e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }
    }

    private static Rgba32 Blend(Rgba32 background, Rgba32 foreground)
    {
        var alpha = foreground.A / 255f;
        return new Rgba32(
            (byte)Math.Clamp(foreground.R * alpha + background.R * (1 - alpha), 0, 255),
            (byte)Math.Clamp(foreground.G * alpha + background.G * (1 - alpha), 0, 255),
            (byte)Math.Clamp(foreground.B * alpha + background.B * (1 - alpha), 0, 255),
            255);
    }

    private static string? BuildParcelWhere(string gushColumn, int? gush, string parcelColumn, int? parcel)
    {
        if (!gush.HasValue || !parcel.HasValue)
        {
            return null;
        }

        return $"{gushColumn} = {gush.Value} AND {parcelColumn} = {parcel.Value}";
    }

    private static string? BuildAddressWhere(string? streetName, string? houseNumber)
    {
        var street = EscapeSqlString(streetName);
        var parsed = ParseHouseNumber(houseNumber);
        if (string.IsNullOrWhiteSpace(street) || parsed is null)
        {
            return null;
        }

        var letterFilter = string.IsNullOrWhiteSpace(parsed.Value.Letter)
            ? string.Empty
            : $" AND BLDG_LETTE = '{EscapeSqlString(parsed.Value.Letter)}'";
        return $"STREET_NAM = '{street}' AND BLDG_NUM = {parsed.Value.Number}{letterFilter}";
    }

    private static string? BuildPreservationWhere(string? streetName, string? houseNumber)
    {
        var street = EscapeSqlString(streetName);
        var number = EscapeSqlString(houseNumber);
        if (string.IsNullOrWhiteSpace(street) || string.IsNullOrWhiteSpace(number))
        {
            return null;
        }

        return $"street = '{street}' AND bldg_num = '{number}'";
    }

    private static HouseNumberParts? ParseHouseNumber(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        var match = System.Text.RegularExpressions.Regex.Match(normalized, @"^(\d+)\s*([א-תA-Za-z]?)$");
        return match.Success
            ? new HouseNumberParts(int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), match.Groups[2].Value)
            : null;
    }

    private static string EscapeSqlString(string? value) => (value ?? string.Empty).Replace("'", "''").Trim();

    private static bool IsInsideHaifaBounds(double? longitude, double? latitude)
    {
        return longitude is >= 34.94 and <= 35.08 &&
               latitude is >= 32.75 and <= 32.86;
    }

    private static Uri BuildQueryUrl(Uri baseUri, IReadOnlyDictionary<string, string> parameters)
    {
        var query = string.Join("&", parameters.Select(parameter =>
            $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));
        return new Uri($"{baseUri}?{query}");
    }

    private static string FormatInvariant(double value) => value.ToString("G17", CultureInfo.InvariantCulture);

    private readonly record struct HouseNumberParts(int Number, string Letter);
    private readonly record struct GisPoint(double Longitude, double Latitude);
    private readonly record struct PixelPoint(int X, int Y);
    private readonly record struct GisBounds(double MinLongitude, double MinLatitude, double MaxLongitude, double MaxLatitude);
    private sealed record ResolvedGisGeometry(GisGeometry Geometry, Uri? NearbyLayer, bool NearbyLayerHasPolygons);

    private sealed record GisGeometry(GisPoint? Center, IReadOnlyList<IReadOnlyList<GisPoint>> Rings)
    {
        public IEnumerable<GisPoint> AllPoints => Rings.Count > 0
            ? Rings.SelectMany(ring => ring)
            : Center is null
                ? Enumerable.Empty<GisPoint>()
                : new[] { Center.Value };

        public static GisGeometry Point(double longitude, double latitude) =>
            new(new GisPoint(longitude, latitude), Array.Empty<IReadOnlyList<GisPoint>>());

        public static GisGeometry Polygon(IReadOnlyList<IReadOnlyList<GisPoint>> rings) =>
            new(null, rings);
    }
}
