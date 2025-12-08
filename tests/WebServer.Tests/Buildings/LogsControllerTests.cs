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
