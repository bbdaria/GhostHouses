using Microsoft.AspNetCore.Mvc;
using WebServer.Controllers;
using WebServer.Models;
using WebServer.Tests.TestSupport;

namespace WebServer.Tests.Controllers;

public class HealthControllerTests
{
    [Fact]
    public async Task GetDatabaseHealth_ReturnsBuildingCount()
    {
        await using var db = TestDb.Create();
        db.Buildings.AddRange(
            new Building { Id = 1, StreetName = "A", HouseNumber = "1" },
            new Building { Id = 2, StreetName = "B", HouseNumber = "2" });
        await db.SaveChangesAsync();

        var result = await new HealthController(db).GetDatabaseHealth();

        var ok = Assert.IsType<OkObjectResult>(result);
        var countProperty = ok.Value!.GetType().GetProperty("count");

        Assert.NotNull(countProperty);
        Assert.Equal(2, countProperty.GetValue(ok.Value));
    }
}
