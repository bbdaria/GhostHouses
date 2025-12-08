using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServer.Controllers;
using WebServer.Data;
using WebServer.Models;
using WebServer.Models.Dtos;
using WebServer.Services;

public class LogsControllerTests
{
    private readonly AppDbContext _context;
    private readonly LogsController _controller;

    public LogsControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        var audit = new Mock<IAuditService>().Object;
        _controller = new LogsController(_context, audit);
    }

    [Fact]
    public async Task GetLogs_FiltersByBuildingId()
    {
        // Arrange
        _context.Buildings.Add(new Building { Id = 1, StreetName = "A", HouseNumber = "1" });
        _context.Buildings.Add(new Building { Id = 2, StreetName = "B", HouseNumber = "2" });

        _context.BuildingLogs.AddRange(
            new BuildingLog { Id = 1, BuildingId = 1, Title = "Log1" },
            new BuildingLog { Id = 2, BuildingId = 2, Title = "Log2" }
        );
        await _context.SaveChangesAsync();

        var filter = new LogFilterParameters(BuildingId: 1);

        // Act
        var result = await _controller.GetLogs(filter);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var data = Assert.IsType<PaginatedResult<BuildingLogDto>>(ok.Value);

        // Assert
        Assert.Single(data.Items);
        Assert.Equal(1, data.Items.First().Id);
    }
}


[Fact]
public async Task GetLogs_FiltersByUserId()
{
    // Arrange
    var user1 = new AppUser { Id = Guid.NewGuid(), Username = "u1" };
    var user2 = new AppUser { Id = Guid.NewGuid(), Username = "u2" };
    _context.Users.AddRange(user1, user2);

    _context.Buildings.Add(new Building { Id = 1, StreetName = "A", HouseNumber = "1" });

    _context.BuildingLogs.AddRange(
        new BuildingLog { Id = 1, BuildingId = 1, CreatedByUserId = user1.Id, Title = "L1" },
        new BuildingLog { Id = 2, BuildingId = 1, CreatedByUserId = user2.Id, Title = "L2" }
    );
    await _context.SaveChangesAsync();

    var filter = new LogFilterParameters(UserId: user1.Id);

    // Act
    var result = await _controller.GetLogs(filter);
    var ok = Assert.IsType<OkObjectResult>(result.Result);
    var data = Assert.IsType<PaginatedResult<BuildingLogDto>>(ok.Value);

    // Assert
    Assert.Single(data.Items);
    Assert.Equal(1, data.Items.First().Id);
}




[Fact]
public async Task GetLogs_FiltersByDateRange()
{
    // Arrange
    var building = new Building { Id = 1, StreetName = "A", HouseNumber = "1" };
    _context.Buildings.Add(building);

    _context.BuildingLogs.AddRange(
        new BuildingLog { Id = 1, BuildingId = 1, Title = "Old", CreatedAt = DateTimeOffset.UtcNow.AddDays(-5) },
        new BuildingLog { Id = 2, BuildingId = 1, Title = "Inside", CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) }
    );
    await _context.SaveChangesAsync();

    var filter = new LogFilterParameters(
        From: DateTimeOffset.UtcNow.AddDays(-2),
        To: DateTimeOffset.UtcNow
    );

    // Act
    var result = await _controller.GetLogs(filter);
    var ok = Assert.IsType<OkObjectResult>(result.Result);
    var data = Assert.IsType<PaginatedResult<BuildingLogDto>>(ok.Value);

    // Assert
    Assert.Single(data.Items);
    Assert.Equal(2, data.Items.First().Id);
}


[Fact]
public async Task GetLogs_FiltersByStreet()
{
    // Arrange
    _context.Buildings.AddRange(
        new Building { Id = 1, StreetName = "Herzl", HouseNumber = "10" },
        new Building { Id = 2, StreetName = "Jabotinsky", HouseNumber = "5" }
    );

    _context.BuildingLogs.AddRange(
        new BuildingLog { Id = 1, BuildingId = 1, Title = "A" },
        new BuildingLog { Id = 2, BuildingId = 2, Title = "B" }
    );
    await _context.SaveChangesAsync();

    var filter = new LogFilterParameters(Street: "Her");

    // Act
    var result = await _controller.GetLogs(filter);
    var ok = Assert.IsType<OkObjectResult>(result.Result);
    var data = Assert.IsType<PaginatedResult<BuildingLogDto>>(ok.Value);

    // Assert
    Assert.Single(data.Items);
    Assert.Equal(1, data.Items.First().Id);
}

