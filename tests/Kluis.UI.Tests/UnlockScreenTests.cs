using Bunit;
using Kluis.Core.Vault;
using Microsoft.Extensions.DependencyInjection;

namespace Kluis.UI.Tests;

public class UnlockScreenTests : BunitContext
{
    private const string Passphrase = "correct horse battery staple";

    private readonly FakeVault _vault = new() { StoredPassphrase = Passphrase };

    public UnlockScreenTests()
    {
        Services.AddSingleton<IVault>(_vault);
    }

    [Fact]
    public void Submitting_an_empty_passphrase_asks_for_one_without_trying_to_unlock()
    {
        var cut = Render<UnlockScreen>();

        cut.Find("form").Submit();

        Assert.Equal("Enter your passphrase.", cut.Find("[role=alert]").TextContent);
        Assert.Empty(_vault.Attempts);
    }

    [Fact]
    public void A_wrong_passphrase_shows_an_error_and_clears_the_field()
    {
        var cut = Render<UnlockScreen>();

        cut.Find("#passphrase").Change("not the passphrase");
        cut.Find("form").Submit();

        Assert.Equal("That passphrase did not unlock the database.", cut.Find("[role=alert]").TextContent);
        Assert.Equal("", cut.Find("#passphrase").GetAttribute("value"));
    }

    [Fact]
    public void The_right_passphrase_raises_the_unlocked_callback()
    {
        var unlocked = false;
        var cut = Render<UnlockScreen>(parameters => parameters.Add(p => p.OnUnlocked, () => unlocked = true));

        cut.Find("#passphrase").Change(Passphrase);
        cut.Find("form").Submit();

        Assert.True(unlocked);
        Assert.Empty(cut.FindAll("[role=alert]"));
    }

    [Fact]
    public void A_missing_database_file_is_explained()
    {
        _vault.StoredPassphrase = null;
        var cut = Render<UnlockScreen>();

        cut.Find("#passphrase").Change(Passphrase);
        cut.Find("form").Submit();

        Assert.Contains("no longer there", cut.Find("[role=alert]").TextContent);
    }

    [Fact]
    public void A_failed_backup_before_an_upgrade_is_explained_and_does_not_unlock()
    {
        _vault.FailBackup = true;
        var unlocked = false;
        var cut = Render<UnlockScreen>(parameters => parameters.Add(p => p.OnUnlocked, () => unlocked = true));

        cut.Find("#passphrase").Change(Passphrase);
        cut.Find("form").Submit();

        Assert.Contains("could not back it up first, so it changed nothing", cut.Find("[role=alert]").TextContent);
        Assert.False(unlocked);
    }

    [Fact]
    public void An_unexpected_failure_shows_a_generic_message_that_leaks_no_details()
    {
        _vault.ThrowOnAccess = true;
        var cut = Render<UnlockScreen>();

        cut.Find("#passphrase").Change(Passphrase);
        cut.Find("form").Submit();

        Assert.Equal("Kluis could not open the database.", cut.Find("[role=alert]").TextContent);
        Assert.DoesNotContain("secret", cut.Markup);
    }
}
