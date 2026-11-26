using Bunit;
using Kluis.Core.Vault;
using Microsoft.Extensions.DependencyInjection;

namespace Kluis.UI.Tests;

public class HomeTests : BunitContext
{
    private readonly FakeVault _vault = new() { StoredPassphrase = "correct horse battery staple" };

    public HomeTests()
    {
        Services.AddSingleton<IVault>(_vault);
    }

    [Fact]
    public void With_remember_off_the_home_screen_says_so_and_offers_nothing_to_forget()
    {
        var cut = Render<Home>();

        Assert.Contains("is off", cut.Find(".settings").TextContent);
        Assert.Empty(cut.FindAll("#forget"));
    }

    [Fact]
    public void With_remember_on_the_home_screen_repeats_the_trade_off_next_to_the_off_switch()
    {
        _vault.RememberedPassphrase = _vault.StoredPassphrase;

        var cut = Render<Home>();

        Assert.Contains("including malware", cut.Find(".settings").TextContent);
        Assert.NotNull(cut.Find("#forget"));
    }
}
