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
