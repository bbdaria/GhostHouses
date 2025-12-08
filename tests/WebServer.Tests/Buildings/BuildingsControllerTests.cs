[Fact]
public async Task GetBuildings_SortsByStreetAndHouseNumber()
{
    // Arrange
    _context.Buildings.AddRange(
        new Building { Id = 1, StreetName = "B Street", HouseNumber = "10" },
        new Building { Id = 2, StreetName = "A Street", HouseNumber = "20" },
        new Building { Id = 3, StreetName = "A Street", HouseNumber = "5" }
    );
    await _context.SaveChangesAsync();

    var filter = new BuildingFilterParameters { Page = 1, PageSize = 10 };

    // Act
    var result = await _controller.GetBuildings(filter, CancellationToken.None);
    var ok = Assert.IsType<OkObjectResult>(result.Result);
    var data = Assert.IsType<PaginatedResult<BuildingSummaryDto>>(ok.Value);

    // Assert
    Assert.Collection(data.Items,
        b => Assert.Equal(3, b.Id),  // A Street, 5
        b => Assert.Equal(2, b.Id),  // A Street, 20
        b => Assert.Equal(1, b.Id)); // B Street, 10
}





[Fact]
public async Task GetBuildings_PaginatesCorrectly()
{
    // Arrange
    for (int i = 1; i <= 15; i++)
    {
        _context.Buildings.Add(new Building
        {
            Id = i,
            StreetName = "Test",
            HouseNumber = i.ToString()
        });
    }
    await _context.SaveChangesAsync();

    var filter = new BuildingFilterParameters { Page = 2, PageSize = 5 };

    // Act
    var result = await _controller.GetBuildings(filter, CancellationToken.None);
    var ok = Assert.IsType<OkObjectResult>(result.Result);
    var data = Assert.IsType<PaginatedResult<BuildingSummaryDto>>(ok.Value);

    // Assert
    Assert.Equal(5, data.Items.Count);
    Assert.Equal(6, data.Items.First().Id);   // Should skip 1–5
    Assert.Equal(10, data.Items.Last().Id);   // Should return 6–10
}
