using WebServer.Data;
using WebServer.Models;

public class ParseStatusTests
{
    [Fact]
    public void ParseStatus_ReturnsEnum_WhenValid()
    {
        var result = Invoke("UnderInspection");

        Assert.Equal(BuildingStatus.UnderInspection, result);
    }

    private BuildingStatus Invoke(string? value)
        => (BuildingStatus) typeof(AppDbContext)
            .GetMethod("ParseStatus", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, new object?[] { value })!;
}
