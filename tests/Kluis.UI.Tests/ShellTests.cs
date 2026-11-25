using Bunit;
using Kluis.Core.Vault;
using Microsoft.Extensions.DependencyInjection;

namespace Kluis.UI.Tests;

public class ShellTests : BunitContext
{
    private readonly FakeVault _vault = new();

    public ShellTests()
    {
        Services.AddSingleton<IVault>(_vault);
    }

    [Fact]
    public void First_run_shows_the_create_passphrase_screen()
    {
        var cut = Render<Shell>();

        Assert.Equal("Welcome to Kluis", cut.Find("h1").TextContent);
    }

    [Fact]
    public void An_existing_database_shows_the_unlock_screen()
    {
        _vault.StoredPassphrase = "correct horse battery staple";

        var cut = Render<Shell>();

        Assert.Equal("Unlock Kluis", cut.Find("h1").TextContent);
    }

    [Fact]
    public void Unlocking_moves_on_to_the_app()
    {
        _vault.StoredPassphrase = "correct horse battery staple";
        var cut = Render<Shell>();

        cut.Find("#passphrase").Change("correct horse battery staple");
        cut.Find("form").Submit();

        Assert.Equal("Kluis", cut.Find("h1").TextContent);
    }

    [Fact]
    public void Creating_the_database_moves_on_to_the_app()
    {
        var cut = Render<Shell>();

        cut.Find("#passphrase").Change("correct horse battery staple");
        cut.Find("#confirmation").Change("correct horse battery staple");
        cut.Find("form").Submit();

        Assert.Equal("Kluis", cut.Find("h1").TextContent);
    }
}
