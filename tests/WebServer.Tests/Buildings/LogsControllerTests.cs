using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebServer.Controllers;
using WebServer.Models;
using WebServer.Models.Dtos;
using WebServer.Models.Users;
using WebServer.Services;
using WebServer.Tests.TestSupport;

namespace WebServer.Tests.Buildings;

public class LogsControllerTests
{
    private static LogsController CreateController(
        WebServer.Data.AppDbContext db,
        Mock<IAuditService>? audit = null)
    {
        return new LogsController(db, (audit ?? new Mock<IAuditService>()).Object);
    }

    private static LogsController CreateAuthenticatedController(
        WebServer.Data.AppDbContext db,
        Guid userId,
        Mock<IAuditService>? audit = null)
    {
        var controller = CreateController(db, audit);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                    "TestAuth"))
            }
        };

        return controller;
    }

    [Fact]
    public async Task GetLogs_FiltersByBuildingIdAndIncludesBuildingFields()
    {
        await using var db = TestDb.Create();
        db.Buildings.AddRange(
            new Building { Id = 1, StreetName = "Herzl", HouseNumber = "10", BuildingName = "A" },
            new Building { Id = 2, StreetName = "Allenby", HouseNumber = "20", BuildingName = "B" });
        db.BuildingLogs.AddRange(
            new BuildingLog { Id = 1, BuildingId = 1, Title = "First", Message = "One" },
            new BuildingLog { Id = 2, BuildingId = 2, Title = "Second", Message = "Two" });
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.GetLogs(
            new LogFilterParameters(BuildingId: 1),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<PaginatedResult<BuildingLogDto>>(ok.Value);
        var item = Assert.Single(payload.Items);

        Assert.Equal(1, item.Id);
        Assert.Equal("Herzl", item.BuildingStreet);
        Assert.Equal("10", item.BuildingHouseNumber);
    }

    [Fact]
    public async Task GetLogs_FiltersByUserId()
    {
        await using var db = TestDb.Create();
        var userId = Guid.NewGuid();
        db.Users.Add(new AppUser { Id = userId, Username = "editor", Email = "editor@example.com" });
        db.Buildings.Add(new Building { Id = 1, StreetName = "Herzl", HouseNumber = "10" });
        db.BuildingLogs.AddRange(
            new BuildingLog { Id = 1, BuildingId = 1, CreatedByUserId = userId, Title = "Mine" },
            new BuildingLog { Id = 2, BuildingId = 1, CreatedByUserId = Guid.NewGuid(), Title = "Other" });
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.GetLogs(
            new LogFilterParameters(UserId: userId),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<PaginatedResult<BuildingLogDto>>(ok.Value);
        var item = Assert.Single(payload.Items);

        Assert.Equal(1, item.Id);
        Assert.Equal("editor", item.CreatedBy);
    }

    [Fact]
    public async Task GetBuildingLogs_ReturnsLogsNewestFirst()
    {
        await using var db = TestDb.Create();
        db.Buildings.Add(new Building { Id = 1, StreetName = "Herzl", HouseNumber = "10" });
        db.BuildingLogs.AddRange(
            new BuildingLog
            {
                Id = 1,
                BuildingId = 1,
                Title = "Old",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-2)
            },
            new BuildingLog
            {
                Id = 2,
                BuildingId = 1,
                Title = "New",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
            });
        await db.SaveChangesAsync();

        var controller = CreateController(db);
        var result = await controller.GetBuildingLogs(1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var logs = Assert.IsAssignableFrom<IEnumerable<BuildingLogDto>>(ok.Value).ToList();

        Assert.Equal(new[] { 2, 1 }, logs.Select(l => l.Id).ToArray());
    }

    [Fact]
    public async Task CreateLog_ReturnsNotFound_WhenBuildingDoesNotExist()
    {
        await using var db = TestDb.Create();
        var controller = CreateController(db);

        var result = await controller.CreateLog(
            404,
            new BuildingLogRequest { Title = "Missing", Message = "No building" },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateLog_PersistsLogAndRecordsAudit()
    {
        await using var db = TestDb.Create();
        var userId = Guid.NewGuid();
        db.Users.Add(new AppUser { Id = userId, Username = "editor", Email = "editor@example.com" });
        db.Buildings.Add(new Building { Id = 1, StreetName = "Herzl", HouseNumber = "10" });
        await db.SaveChangesAsync();

        var audit = new Mock<IAuditService>();
        var controller = CreateAuthenticatedController(db, userId, audit);

        var result = await controller.CreateLog(
            1,
            new BuildingLogRequest
            {
                Title = "Manual note",
                Message = "Checked by inspector",
                Category = "Inspection",
                Severity = "info"
            },
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<BuildingLogDto>(created.Value);

        Assert.Equal("Manual note", dto.Title);
        Assert.Equal("Checked by inspector", dto.Message);
        Assert.Equal("editor", dto.CreatedBy);
        Assert.Equal(1, db.BuildingLogs.Count());
        audit.Verify(a => a.RecordAsync(
                userId,
                nameof(BuildingLog),
                It.IsAny<string>(),
                "Create",
                It.IsAny<object?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
