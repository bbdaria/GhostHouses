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


[Fact]
public async Task GetBuildings_ReturnsCorrectNumberOfItems_WhenMultipleBuildingsExist()
{
    var db = CreateDb();

    // Arrange: add 3 buildings
    db.Buildings.AddRange(
        new Building { Id = 1, Name = "B1" },
        new Building { Id = 2, Name = "B2" },
        new Building { Id = 3, Name = "B3" }
    );
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

    Assert.Equal(3, paginated.Items.Count());
    Assert.Equal(3, paginated.Total);
}

[Fact]
public async Task GetBuildings_FilterByStreetName_ReturnsOnlyMatches()
{
    var db = CreateDb();

    db.Buildings.AddRange(
        new Building { FldId = "1", StreetName = "Main", HouseNumber = "10", BuildingName = "A" },
        new Building { FldId = "2", StreetName = "Main", HouseNumber = "11", BuildingName = "B" },
        new Building { FldId = "3", StreetName = "Other", HouseNumber = "5", BuildingName = "C" }
    );

    await db.SaveChangesAsync();

    var controller = new BuildingsController(db, null, null);

    var filter = new BuildingFilterParameters(StreetName: "Main");

    var result = await controller.GetBuildings(filter, default);

    var ok = Assert.IsType<OkObjectResult>(result.Result);
    var payload = Assert.IsType<PaginatedResult<BuildingSummaryDto>>(ok.Value);

    // Two results expected
    Assert.Equal(2, payload.Items.Count);
    Assert.Equal(2, payload.Total);

    // Both results must have StreetName = "Main"
    Assert.All(payload.Items, item => Assert.Equal("Main", item.StreetName));
}

[Fact]
public async Task GetBuildings_FiltersByNeighbourhood()
{
    var db = CreateDb();
    db.Buildings.AddRange(
        new Building { FldId = "1", Neighbourhood = "Center", StreetName = "A" },
        new Building { FldId = "2", Neighbourhood = "North", StreetName = "B" }
    );
    await db.SaveChangesAsync();

    var ctrl = new BuildingsController(db, null, null);

    var filter = new BuildingFilterParameters { Neighbourhood = "Center" };

    var result = await ctrl.GetBuildings(filter, default);

    var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
    var payload = Assert.IsType<PaginatedResult<BuildingSummaryDto>>(ok.Value);

    Assert.Single(payload.Items);
    Assert.Equal("Center", payload.Items.First().Neighbourhood);
}

[Fact]
public async Task GetBuildings_FiltersByStatus()
{
    var db = CreateDb();
    db.Buildings.AddRange(
        new Building {
            FldId = "1",
            ShikunStatus = BuildingStatus.Renovated,
            StreetName = "A"
        },
        new Building {
            FldId = "2",
            ShikunStatus = BuildingStatus.Unknown,
            StreetName = "B"
        }
    );
    await db.SaveChangesAsync();

    var ctrl = new BuildingsController(db, null, null);
    var filter = new BuildingFilterParameters { ShikunStatus = BuildingStatus.Renovated };

    var result = await ctrl.GetBuildings(filter, default);

    var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
    var payload = Assert.IsType<PaginatedResult<BuildingSummaryDto>>(ok.Value);

    Assert.Single(payload.Items);
    Assert.Equal(BuildingStatus.Renovated, payload.Items.First().ShikunStatus);
}

[Fact]
public async Task GetBuildings_FiltersByNeighborhood()
{
    var db = CreateDb();
    db.Buildings.AddRange(
        new Building { FldId = "1", Neighborhood = "Ramat" },
        new Building { FldId = "2", Neighborhood = "Neve" }
    );
    await db.SaveChangesAsync();

    var ctrl = new BuildingsController(db, null, null);
    var filter = new BuildingFilterParameters { Neighborhood = "Ramat" };

    var result = await ctrl.GetBuildings(filter, default);

    var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
    var data = Assert.IsType<PaginatedResult<BuildingSummaryDto>>(ok.Value);

    Assert.Single(data.Items);
    Assert.Equal("Ramat", data.Items.First().Neighborhood);
}

[Fact]
public async Task GetBuildings_FiltersByStatusSummary()
{
    var db = CreateDb();
    db.Buildings.AddRange(
        new Building { FldId = "1", StatusSummary = "Needs repair" },
        new Building { FldId = "2", StatusSummary = "All good" }
    );
    await db.SaveChangesAsync();

    var ctrl = new BuildingsController(db, null, null);
    var filter = new BuildingFilterParameters { StatusSummary = "repair" };

    var result = await ctrl.GetBuildings(filter, default);

    var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
    var data = Assert.IsType<PaginatedResult<BuildingSummaryDto>>(ok.Value);

    Assert.Single(data.Items);
    Assert.Contains("repair", data.Items.First().StatusSummary, StringComparison.OrdinalIgnoreCase);
}


[Fact]
public async Task GetBuildings_PaginatesCorrectly()
{
    var db = CreateDb();
    for (int i = 1; i <= 30; i++)
    {
        db.Buildings.Add(new Building { FldId = i.ToString(), StreetName = "X" });
    }
    await db.SaveChangesAsync();

    var ctrl = new BuildingsController(db, null, null);

    var filter = new BuildingFilterParameters
    {
        Page = 2,
        PageSize = 10
    };

    var result = await ctrl.GetBuildings(filter, default);

    var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
    var data = Assert.IsType<PaginatedResult<BuildingSummaryDto>>(ok.Value);

    Assert.Equal(10, data.Items.Count());
    Assert.Equal(30, data.Total);
    Assert.Equal(2, data.Page);
}

[Fact]
public async Task GetBuildings_ReturnsEmpty_WhenNoMatches()
{
    var db = CreateDb();
    db.Buildings.Add(new Building { FldId = "1", StreetName = "ABC" });
    await db.SaveChangesAsync();

    var ctrl = new BuildingsController(db, null, null);

    var filter = new BuildingFilterParameters { Street = "ZZZ" };

    var result = await ctrl.GetBuildings(filter, default);

    var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
    var data = Assert.IsType<PaginatedResult<BuildingSummaryDto>>(ok.Value);

    Assert.Empty(data.Items);
}
