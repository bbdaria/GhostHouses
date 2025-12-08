using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using WebServer.Controllers;
using WebServer.Data;
using WebServer.Models;
using WebServer.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace WebServer.Tests.Buildings
{
    public class GetBuildingByIdTests
    {
        private AppDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("GetBuildingByIdDb")
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task ReturnsNotFound_WhenBuildingDoesNotExist()
        {
            var db = CreateDb();

            var externalMock = new Mock<IExternalDataService>();
            var auditMock = new Mock<IAuditService>();
            var controller = new BuildingsController(db, externalMock.Object, auditMock.Object);

            // Act
            var result = await controller.GetBuilding(999, default);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task ReturnsBuilding_WhenBuildingExists()
        {
            var db = CreateDb();

            db.Buildings.Add(new Building { Id = 1, Name = "A" });
            db.SaveChanges();

            var externalMock = new Mock<IExternalDataService>();
            var auditMock = new Mock<IAuditService>();
            var controller = new BuildingsController(db, externalMock.Object, auditMock.Object);

            // Act
            var result = await controller.GetBuilding(1, default);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<BuildingDto>(ok.Value);

            Assert.Equal("A", dto.Name);
            Assert.Equal(1, dto.Id);
        }
    }
}

[Fact]
public async Task GetBuildings_Pagination_ReturnsCorrectPage()
{
    var db = CreateDb();

    // Insert 15 buildings
    for (int i = 1; i <= 15; i++)
    {
        db.Buildings.Add(new Building
        {
            FldId = "F" + i,
            StreetName = "Street" + i,
            HouseNumber = i.ToString(),
            BuildingName = "Building " + i,
            Neighbourhood = "Center",
            Bldisgvu = "A",
            ShikunStatus = BuildingStatus.Unknown,
            StatusSummary = ""
        });
    }
    await db.SaveChangesAsync();

    var controller = new BuildingsController(db, null, null);

    var filter = new BuildingFilterParameters(Page: 1, PageSize: 10);

    var result = await controller.GetBuildings(filter, default);

    var ok = Assert.IsType<OkObjectResult>(result.Result);
    var paginated = Assert.IsType<PaginatedResult<BuildingSummaryDto>>(ok.Value);

    // Assert page size
    Assert.Equal(10, paginated.Items.Count);

    // Assert total count is all 15 buildings
    Assert.Equal(15, paginated.Total);
}
