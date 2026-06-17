using Xunit;

namespace WebServer.Tests;

public class SampleTests
{
    [Fact]
    public void SampleTest_ShouldPass()
    {
        Assert.Equal(2, 1 + 1);
    }
}
