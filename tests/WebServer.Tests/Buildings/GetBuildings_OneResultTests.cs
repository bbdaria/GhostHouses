using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using WebServer.Controllers;
using WebServer.Data;
using WebServer.Models;
using WebServer.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace WebServer.Tests.Buildings;

public class GetBuildings_OneResultTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("OneResultDb")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetBuildings_ReturnsSingleBuilding()
    {
        var db = CreateDb();
        db.Buildings.Add(new Building
        {
            FldId = "F1",
            StreetName = "Main",
            HouseNumber = "10",
            BuildingName = "Test Bld",
            Neighborhood = "Center",
            BldSivug = "A",
            ShikumStatus = BuildingStatus.Unknown,
            StatusSummary = ""
        });
        await db.SaveChangesAsync();

        var ctrl = new BuildingsController(db, null!, null!);
        var filter = new BuildingFilterParameters();

        var result = await ctrl.GetBuildings(filter, default);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<PaginatedResult<BuildingSummaryDto>>(ok.Value);

        Assert.Single(payload.Items);
        Assert.Equal(1, payload.Total);
        Assert.Equal("Main", payload.Items.First().StreetName);
    }
}
