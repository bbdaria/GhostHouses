using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using WebServer.Controllers;
using WebServer.Data;
using WebServer.Models;
using WebServer.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace WebServer.Tests.Buildings;

public class GetBuildingsTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("BuildingsTestDb")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetBuildings_ReturnsEmptyList_WhenNoBuildingsExist()
    {
        var db = CreateDb();
        var ctrl = new BuildingsController(db, null!, null!);

        var filter = new BuildingFilterParameters();
        var result = await ctrl.GetBuildings(filter, default);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<PaginatedResult<BuildingSummaryDto>>(ok.Value);

        Assert.Empty(payload.Items);
        Assert.Equal(0, payload.Total);
    }
}

[Fact]
public async Task GetBuildings_ReturnsOneResult_WhenSingleBuildingExists()
{
    var db = CreateDb();

    // Arrange: insert 1 building
    db.Buildings.Add(new Building { Id = 1, Name = "Test1" });
    db.SaveChanges();

    var externalMock = new Mock<IExternalDataService>();
    var auditMock = new Mock<IAuditService>();
    var controller = new BuildingsController(db, externalMock.Object, auditMock.Object);

    var filter = new BuildingFilterParameters(Page: 1, PageSize: 10);
    var token = CancellationToken.None;

    // Act
    var result = await controller.GetBuildings(filter, token);

    // Assert
    var ok = Assert.IsType<OkObjectResult>(result.Result);
    var paginated = Assert.IsType<PaginatedResult<BuildingSummaryDto>>(ok.Value);

    Assert.Single(paginated.Items);
    Assert.Equal(1, paginated.Total);
}


