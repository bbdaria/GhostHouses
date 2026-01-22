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




[Fact]
public async Task GetLogs_FiltersByStatus()
{
    // Arrange
    _context.Buildings.AddRange(
        new Building { Id = 1, StreetName = "A", HouseNumber = "1", ShikumStatus = BuildingStatus.Good },
        new Building { Id = 2, StreetName = "B", HouseNumber = "2", ShikumStatus = BuildingStatus.Bad }
    );

    _context.BuildingLogs.AddRange(
        new BuildingLog { Id = 1, BuildingId = 1, Title = "GoodLog" },
        new BuildingLog { Id = 2, BuildingId = 2, Title = "BadLog" }
    );

    await _context.SaveChangesAsync();

    var filter = new LogFilterParameters(Status: BuildingStatus.Good);

    // Act
    var result = await _controller.GetLogs(filter);
    var ok = Assert.IsType<OkObjectResult>(result.Result);
    var data = Assert.IsType<PaginatedResult<BuildingLogDto>>(ok.Value);

    // Assert
    Assert.Single(data.Items);
    Assert.Equal(1, data.Items.First().Id);
}


[Fact]
public async Task GetLogs_FiltersByNeighborhood()
{
    // Arrange
    _context.Buildings.AddRange(
        new Building { Id = 1, StreetName = "A", HouseNumber = "1", Neighborhood = "Downtown" },
        new Building { Id = 2, StreetName = "B", HouseNumber = "2", Neighborhood = "Hadar" }
    );

    _context.BuildingLogs.AddRange(
        new BuildingLog { Id = 1, BuildingId = 1, Title = "in DT" },
        new BuildingLog { Id = 2, BuildingId = 2, Title = "in Hadar" }
    );

    await _context.SaveChangesAsync();

    var filter = new LogFilterParameters(Neighborhood: "Down");

    // Act
    var result = await _controller.GetLogs(filter);
    var ok = Assert.IsType<OkObjectResult>(result.Result);
    var data = Assert.IsType<PaginatedResult<BuildingLogDto>>(ok.Value);

    // Assert
    Assert.Single(data.Items);
    Assert.Equal(1, data.Items.First().Id);
}




[Fact]
public async Task GetLogs_FiltersByStatusSummary()
{
    // Arrange
    _context.Buildings.AddRange(
        new Building { Id = 1, StatusSummary = "Needs Repair" },
        new Building { Id = 2, StatusSummary = "All Good" }
    );

    _context.BuildingLogs.AddRange(
        new BuildingLog { Id = 1, BuildingId = 1, Title = "Repair log" },
        new BuildingLog { Id = 2, BuildingId = 2, Title = "Good log" }
    );

    await _context.SaveChangesAsync();

    var filter = new LogFilterParameters(StatusSummary: "Repair");

    // Act
    var result = await _controller.GetLogs(filter);
    var ok = Assert.IsType<OkObjectResult>(result.Result);
    var data = Assert.IsType<PaginatedResult<BuildingLogDto>>(ok.Value);

    // Assert
    Assert.Single(data.Items);
    Assert.Equal(1, data.Items.First().Id);
}




[Fact]
public async Task GetLogs_ExcludesDeletedLogs()
{
    // Arrange
    _context.Buildings.Add(new Building { Id = 1, StreetName = "X", HouseNumber = "1" });

    _context.BuildingLogs.AddRange(
        new BuildingLog { Id = 1, BuildingId = 1, Title = "Visible", IsDeleted = false },
        new BuildingLog { Id = 2, BuildingId = 1, Title = "Deleted", IsDeleted = true }
    );

    await _context.SaveChangesAsync();

    var filter = new LogFilterParameters();

    // Act
    var result = await _controller.GetLogs(filter);
    var ok = Assert.IsType<OkObjectResult>(result.Result);
    var data = Assert.IsType<PaginatedResult<BuildingLogDto>>(ok.Value);

    // Assert
    Assert.Single(data.Items);
    Assert.Equal(1, data.Items.First().Id);
}


[Fact]
public async Task CreateLog_ReturnsCreatedLogDto()
{
    // Arrange
    _context.Buildings.Add(new Building { Id = 1, StreetName = "A", HouseNumber = "10" });
    await _context.SaveChangesAsync();

    var request = new BuildingLogRequest
    {
        Title = "Test log",
        Message = "Message here",
        Category = "info",
        Severity = "low"
    };

    // Act
    var result = await _controller.CreateLog(1, request);
    var created = Assert.IsType<CreatedAtActionResult>(result.Result);
    var dto = Assert.IsType<BuildingLogDto>(created.Value);

    // Assert
    Assert.Equal("Test log", dto.Title);
    Assert.Equal("Message here", dto.Message);
    Assert.Equal("info", dto.Category);
    Assert.Equal("low", dto.Severity);
    Assert.Equal(1, dto.BuildingId);
}





[Fact]
public async Task UpdateLog_UpdatesFields()
{
    // Arrange
    _context.Buildings.Add(new Building { Id = 1, StreetName = "A", HouseNumber = "10" });
    _context.BuildingLogs.Add(new BuildingLog
    {
        Id = 5,
        BuildingId = 1,
        Title = "Old",
        Message = "Old msg",
        Category = "old",
        Severity = "old"
    });

    await _context.SaveChangesAsync();

    var request = new BuildingLogRequest
    {
        Title = "New",
        Message = "New msg",
        Category = "new",
        Severity = "critical"
    };

    // Act
    var result = await _controller.UpdateLog(5, request);

    // Assert
    Assert.IsType<NoContentResult>(result);

    var updated = await _context.BuildingLogs.FindAsync(5);
    Assert.Equal("New", updated.Title);
    Assert.Equal("New msg", updated.Message);
    Assert.Equal("new", updated.Category);
    Assert.Equal("critical", updated.Severity);
}




[Fact]
public async Task DeleteLog_SoftDeletesLog()
{
    // Arrange
    _context.Users.Add(new AppUser { Id = Guid.NewGuid(), Username = "admin", Role = UserRole.Admin });
    await _context.SaveChangesAsync();

    _context.Buildings.Add(new Building { Id = 1, StreetName = "A", HouseNumber = "10" });
    _context.BuildingLogs.Add(new BuildingLog { Id = 20, BuildingId = 1, Title = "t" });
    await _context.SaveChangesAsync();

    // Act
    var result = await _controller.DeleteLog(20);
    
    // Assert
    Assert.IsType<NoContentResult>(result);

    var log = await _context.BuildingLogs.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == 20);
    Assert.True(log.IsDeleted);
}




[Fact]
public async Task DeleteLog_ReturnsNotFound_WhenLogDoesNotExist()
{
    // Act
    var result = await _controller.DeleteLog(999);

    // Assert
    Assert.IsType<NotFoundResult>(result);
}


