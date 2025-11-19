using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebServer.Data;
using WebServer.Models;
using WebServer.Models.Dtos;

namespace WebServer.Services;

public interface IExternalDataService
{
    Task<BuildingExternalDataDto> GetBuildingDataAsync(int buildingId, CancellationToken cancellationToken = default);
    Task SnapshotAsync(int buildingId, CancellationToken cancellationToken = default);
}

public class ExternalDataService : IExternalDataService
{
    private readonly AppDbContext _context;

    public ExternalDataService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<BuildingExternalDataDto> GetBuildingDataAsync(int buildingId, CancellationToken cancellationToken = default)
    {
        var snapshots = await _context.ExternalSystemSnapshots
            .Where(s => s.BuildingId == buildingId)
            .ToListAsync(cancellationToken);

        ExternalSystemSnapshotDto Map(string system)
        {
            var snap = snapshots
                .Where(s => s.SystemName == system)
                .OrderByDescending(s => s.RetrievedAt)
                .FirstOrDefault();
            if (snap is null)
            {
                var payload = JsonSerializer.Serialize(new { status = "No data", at = DateTimeOffset.UtcNow });
                return new ExternalSystemSnapshotDto(system, payload, DateTimeOffset.UtcNow);
            }

            return new ExternalSystemSnapshotDto(system, snap.Payload, snap.RetrievedAt);
        }

        return new BuildingExternalDataDto(
            Map("GIS"),
            Map("Water"),
            Map("Electricity"),
            Map("Tax"),
            Map("CRM106"));
    }

    public async Task SnapshotAsync(int buildingId, CancellationToken cancellationToken = default)
    {
        var systems = new[] { "GIS", "Water", "Electricity", "Tax", "CRM106" };
        foreach (var system in systems)
        {
            var payload = JsonSerializer.Serialize(new
            {
                system,
                updatedAt = DateTimeOffset.UtcNow,
                status = "ok",
                notes = "Mocked integration payload"
            });

            _context.ExternalSystemSnapshots.Add(new ExternalSystemSnapshot
            {
                BuildingId = buildingId,
                SystemName = system,
                Payload = payload,
                RetrievedAt = DateTimeOffset.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
