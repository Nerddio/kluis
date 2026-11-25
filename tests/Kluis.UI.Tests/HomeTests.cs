using Bunit;
using Kluis.UI;

namespace Kluis.UI.Tests;

public class HomeTests : BunitContext
{
    [Fact]
    public void Home_shows_the_app_name_as_its_heading()
    {
        var cut = Render<Home>();

        Assert.Equal("Kluis", cut.Find("h1").TextContent);
    }
}
