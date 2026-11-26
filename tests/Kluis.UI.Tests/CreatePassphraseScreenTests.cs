using Bunit;
using Kluis.Core.Vault;
using Microsoft.Extensions.DependencyInjection;

namespace Kluis.UI.Tests;

public class CreatePassphraseScreenTests : BunitContext
{
    private readonly FakeVault _vault = new();

    public CreatePassphraseScreenTests()
    {
        Services.AddSingleton<IVault>(_vault);
    }

    [Fact]
    public void The_screen_warns_that_a_forgotten_passphrase_cannot_be_recovered()
    {
        var cut = Render<CreatePassphraseScreen>();

        Assert.Contains("no way to recover a forgotten passphrase", cut.Find("[role=note]").TextContent);
    }

    [Fact]
    public void A_short_passphrase_is_refused_without_creating_anything()
    {
        var cut = Render<CreatePassphraseScreen>();

        cut.Find("#passphrase").Change("short");
        cut.Find("#confirmation").Change("short");
        cut.Find("form").Submit();

        Assert.Contains($"at least {PassphrasePolicy.MinimumLength} characters", cut.Find("[role=alert]").TextContent);
        Assert.Empty(_vault.Attempts);
    }

    [Fact]
    public void A_confirmation_that_does_not_match_is_refused()
    {
        var cut = Render<CreatePassphraseScreen>();

        cut.Find("#passphrase").Change("correct horse battery staple");
        cut.Find("#confirmation").Change("correct horse battery stapel");
        cut.Find("form").Submit();

        Assert.Equal("The two passphrases do not match.", cut.Find("[role=alert]").TextContent);
        Assert.Empty(_vault.Attempts);
    }

    [Fact]
    public void A_valid_passphrase_creates_the_database_and_raises_the_created_callback()
    {
        var created = false;
        var cut = Render<CreatePassphraseScreen>(parameters => parameters.Add(p => p.OnCreated, () => created = true));

        cut.Find("#passphrase").Change("correct horse battery staple");
        cut.Find("#confirmation").Change("correct horse battery staple");
        cut.Find("form").Submit();

        Assert.Equal([("correct horse battery staple", false)], _vault.Attempts);
        Assert.True(created);
    }

    [Fact]
    public void A_failure_while_creating_shows_a_generic_message_that_leaks_no_details()
    {
        _vault.ThrowOnAccess = true;
        var cut = Render<CreatePassphraseScreen>();

        cut.Find("#passphrase").Change("correct horse battery staple");
        cut.Find("#confirmation").Change("correct horse battery staple");
        cut.Find("form").Submit();

        Assert.Equal("Kluis could not create the database.", cut.Find("[role=alert]").TextContent);
        Assert.DoesNotContain("secret", cut.Markup);
    }
}
