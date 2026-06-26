using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebServer.Controllers;
using WebServer.Models;
using WebServer.Models.Dtos;
using WebServer.Services;
using WebServer.Tests.TestSupport;

namespace WebServer.Tests.Buildings;

public class BuildingsControllerTests
{
    private static BuildingsController CreateController(WebServer.Data.AppDbContext db)
    {
        return new BuildingsController(
            db,
            Mock.Of<IAuditService>(),
            Mock.Of<IWebHostEnvironment>());
    }

    [Fact]
    public async Task GetBuildings_ReturnsEmptyPage_WhenDatabaseHasNoBuildings()
    {
        await using var db = TestDb.Create();
        var controller = CreateController(db);

        var result = await controller.GetBuildings(new BuildingFilterParameters(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<PaginatedResult<BuildingSummaryDto>>(ok.Value);
        Assert.Empty(payload.Items);
        Assert.Equal(0, payload.Total);
        Assert.Equal(1, payload.Page);
        Assert.Equal(20, payload.PageSize);
    }

    [Fact]
    public async Task GetBuildings_OrdersByStreetAndHouseNumberAndPaginates()
    {
        await using var db = TestDb.Create();
        db.Buildings.AddRange(
            new Building { Id = 1, StreetName = "B Street", HouseNumber = "10", BuildingName = "B" },
            new Building { Id = 2, StreetName = "A Street", HouseNumber = "20", BuildingName = "A20" },
            new Building { Id = 3, StreetName = "A Street", HouseNumber = "5", BuildingName = "A5" });
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.GetBuildings(
            new BuildingFilterParameters(Page: 1, PageSize: 2),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<PaginatedResult<BuildingSummaryDto>>(ok.Value);
        var items = payload.Items.ToList();

        Assert.Equal(3, payload.Total);
        Assert.Equal(new[] { 2, 3 }, items.Select(b => b.Id).ToArray());
    }

    [Fact]
    public async Task GetBuildings_FiltersByStatusWithoutDatabaseSpecificStringFunctions()
    {
        await using var db = TestDb.Create();
        db.Buildings.AddRange(
            new Building { Id = 1, StreetName = "A", HouseNumber = "1", ShikumStatus = BuildingStatus.InExecution },
            new Building { Id = 2, StreetName = "B", HouseNumber = "2", ShikumStatus = BuildingStatus.Unknown });
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.GetBuildings(
            new BuildingFilterParameters(Status: BuildingStatus.InExecution),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<PaginatedResult<BuildingSummaryDto>>(ok.Value);
        var item = Assert.Single(payload.Items);
        Assert.Equal(1, item.Id);
        Assert.Equal(BuildingStatus.InExecution, item.ShikumStatus);
    }

    [Fact]
    public async Task GetBuilding_ReturnsNotFound_WhenBuildingDoesNotExist()
    {
        await using var db = TestDb.Create();
        var controller = CreateController(db);

        var result = await controller.GetBuilding(999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetBuilding_ReturnsDetailWithRecentLogsAndGisLocation()
    {
        await using var db = TestDb.Create();
        db.Buildings.Add(new Building
        {
            Id = 10,
            StreetName = "Ahavat Zion",
            HouseNumber = "10",
            BuildingName = "Haifa House",
            Neighborhood = "Hadar",
            Latitude = 32.8,
            Longitude = 35.0,
            StatusSummary = "Open issue"
        });
        db.BuildingLogs.Add(new BuildingLog
        {
            Id = 1,
            BuildingId = 10,
            Title = "Created",
            Message = "Created from test"
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.GetBuilding(10, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var detail = Assert.IsType<BuildingDetailDto>(ok.Value);

        Assert.Equal(10, detail.Summary.Id);
        Assert.Equal("Haifa House", detail.Summary.BuildingName);
        Assert.Equal(32.8, detail.GisLocation.Latitude);
        Assert.Equal(35.0, detail.GisLocation.Longitude);
        Assert.Single(detail.RecentLogs);
        Assert.NotEmpty(detail.Fields);
    }

    [Fact]
    public async Task GetGisCandidates_ReturnsAllBuildingsWithLocationPayload()
    {
        await using var db = TestDb.Create();
        db.Buildings.Add(new Building
        {
            Id = 7,
            StreetName = "Main",
            HouseNumber = "1",
            BuildingName = "GIS Building",
            Latitude = 32.81,
            Longitude = 34.99
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.GetGisCandidates(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var candidates = Assert.IsAssignableFrom<IEnumerable<BuildingGisCandidateDto>>(ok.Value).ToList();
        var candidate = Assert.Single(candidates);

        Assert.Equal(7, candidate.Id);
        Assert.Equal("GIS Building", candidate.BuildingName);
        Assert.Equal(32.81, candidate.GisLocation.Latitude);
        Assert.Equal(34.99, candidate.GisLocation.Longitude);
    }
}
