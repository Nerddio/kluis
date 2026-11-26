using Bunit;
using Kluis.Core.Vault;
using Microsoft.Extensions.DependencyInjection;

namespace Kluis.UI.Tests;

public class RememberOnThisPcTests : BunitContext
{
    private const string Passphrase = "correct horse battery staple";

    private readonly FakeVault _vault = new();

    public RememberOnThisPcTests()
    {
        Services.AddSingleton<IVault>(_vault);
    }

    [Fact]
    public void The_option_is_unticked_by_default_and_states_the_trade_off_on_both_screens()
    {
        var create = Render<CreatePassphraseScreen>();
        _vault.StoredPassphrase = Passphrase;
        var unlock = Render<UnlockScreen>();

        foreach (var screen in new IRenderedComponent<Microsoft.AspNetCore.Components.IComponent>[] { create, unlock })
        {
            Assert.False(screen.Find("#remember").HasAttribute("checked"));
            Assert.Contains("including malware, can open your Kluis data without the passphrase", screen.Markup);
        }
    }

    [Fact]
    public void Ticking_remember_while_creating_passes_the_choice_to_the_vault()
    {
        var cut = Render<CreatePassphraseScreen>();

        cut.Find("#passphrase").Change(Passphrase);
        cut.Find("#confirmation").Change(Passphrase);
        cut.Find("#remember").Change(true);
        cut.Find("form").Submit();

        Assert.Equal([(Passphrase, true)], _vault.Attempts);
    }

    [Fact]
    public void Ticking_remember_while_unlocking_passes_the_choice_to_the_vault()
    {
        _vault.StoredPassphrase = Passphrase;
        var cut = Render<UnlockScreen>();

        cut.Find("#passphrase").Change(Passphrase);
        cut.Find("#remember").Change(true);
        cut.Find("form").Submit();

        Assert.Equal([(Passphrase, true)], _vault.Attempts);
        Assert.True(_vault.IsRemembered);
    }

    [Fact]
    public void A_remembered_passphrase_skips_the_unlock_screen_at_startup()
    {
        _vault.StoredPassphrase = Passphrase;
        _vault.RememberedPassphrase = Passphrase;

        var cut = Render<Shell>();

        Assert.Equal("Kluis", cut.Find("h1").TextContent);
    }

    [Fact]
    public void A_remembered_passphrase_that_no_longer_works_falls_back_to_asking_and_says_why()
    {
        _vault.StoredPassphrase = Passphrase;
        _vault.RememberedPassphrase = "an old passphrase that changed";

        var cut = Render<Shell>();

        Assert.Equal("Unlock Kluis", cut.Find("h1").TextContent);
        Assert.Contains("no longer unlocks the database, so it was removed", cut.Find("[role=alert]").TextContent);
        Assert.False(_vault.IsRemembered);
    }

    [Fact]
    public void Turning_remember_off_on_the_home_screen_forgets_the_passphrase()
    {
        _vault.StoredPassphrase = Passphrase;
        _vault.RememberedPassphrase = Passphrase;
        var cut = Render<Shell>();

        cut.Find("#forget").Click();

        Assert.False(_vault.IsRemembered);
        Assert.Contains("is off", cut.Find(".settings").TextContent);
    }
}
