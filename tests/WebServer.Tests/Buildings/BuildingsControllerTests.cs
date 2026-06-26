using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.IO.Compression;
using System.Xml.Linq;
using WebServer.Controllers;
using WebServer.Models;
using WebServer.Models.Dtos;
using WebServer.Services;
using WebServer.Tests.TestSupport;

namespace WebServer.Tests.Buildings;

public class BuildingsControllerTests
{
    private static BuildingsController CreateController(
        WebServer.Data.AppDbContext db,
        IGisSnapshotService? gisSnapshotService = null,
        IWebHostEnvironment? hostEnvironment = null)
    {
        return new BuildingsController(
            db,
            Mock.Of<IAuditService>(),
            hostEnvironment ?? Mock.Of<IWebHostEnvironment>(),
            gisSnapshotService ?? Mock.Of<IGisSnapshotService>());
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

    [Fact]
    public async Task ExportBuildingCard_KeepsGisSnapshotPlaceholder_WhenSnapshotExists()
    {
        await using var db = TestDb.Create();
        db.Buildings.Add(new Building
        {
            Id = 11,
            StreetName = "Main",
            HouseNumber = "1",
            BuildingName = "Card Building"
        });
        await db.SaveChangesAsync();

        var gisSnapshot = new Mock<IGisSnapshotService>();
        gisSnapshot
            .Setup(service => service.CreateBuildingSnapshotAsync(It.IsAny<Building>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOnePixelPng());

        var hostEnvironment = new Mock<IWebHostEnvironment>();
        hostEnvironment
            .SetupGet(env => env.ContentRootPath)
            .Returns(FindBackendContentRoot());

        var controller = CreateController(db, gisSnapshot.Object, hostEnvironment.Object);

        var result = await controller.ExportBuildingCard(11, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        using var pptxStream = new MemoryStream(file.FileContents);
        using var archive = new ZipArchive(pptxStream, ZipArchiveMode.Read);

        Assert.NotNull(archive.GetEntry("ppt/media/image2.png"));

        using var relStream = archive.GetEntry("ppt/slides/_rels/slide1.xml.rels")!.Open();
        var relDoc = XDocument.Load(relStream);
        XNamespace rel = "http://schemas.openxmlformats.org/package/2006/relationships";
        var mapRelationship = relDoc.Root!
            .Elements(rel + "Relationship")
            .SingleOrDefault(element => (string?)element.Attribute("Id") == "rId2");

        Assert.NotNull(mapRelationship);
        Assert.Equal("../media/image2.png", (string?)mapRelationship!.Attribute("Target"));
    }

    private static string FindBackendContentRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "project", "web-server", "backend");
            if (File.Exists(Path.Combine(candidate, "Data", "BuildingCardTemplate.pptx")))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate backend content root.");
    }

    private static byte[] CreateOnePixelPng() =>
        Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAFgwJ/lW1sMwAAAABJRU5ErkJggg==");
}
