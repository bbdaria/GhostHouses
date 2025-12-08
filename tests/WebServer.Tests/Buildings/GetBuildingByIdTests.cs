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
