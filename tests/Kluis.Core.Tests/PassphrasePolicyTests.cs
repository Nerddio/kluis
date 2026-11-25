using Kluis.Core.Vault;

namespace Kluis.Core.Tests;

public class PassphrasePolicyTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    public void An_empty_passphrase_is_rejected(string? passphrase, string? confirmation)
    {
        Assert.Equal(PassphraseProblem.Empty, PassphrasePolicy.Validate(passphrase, confirmation));
    }

    [Fact]
    public void One_character_below_the_minimum_is_too_short()
    {
        var passphrase = new string('a', PassphrasePolicy.MinimumLength - 1);

        Assert.Equal(PassphraseProblem.TooShort, PassphrasePolicy.Validate(passphrase, passphrase));
    }

    [Fact]
    public void Exactly_the_minimum_length_is_accepted()
    {
        var passphrase = new string('a', PassphrasePolicy.MinimumLength);

        Assert.Equal(PassphraseProblem.None, PassphrasePolicy.Validate(passphrase, passphrase));
    }

    [Theory]
    [InlineData("correct horse battery", "correct horse batterY")]
    [InlineData("correct horse battery", "correct horse battery ")]
    [InlineData("correct horse battery", null)]
    public void A_confirmation_that_differs_in_any_way_is_a_mismatch(string passphrase, string? confirmation)
    {
        Assert.Equal(PassphraseProblem.Mismatch, PassphrasePolicy.Validate(passphrase, confirmation));
    }

    [Fact]
    public void Length_is_checked_before_the_confirmation()
    {
        Assert.Equal(PassphraseProblem.TooShort, PassphrasePolicy.Validate("short", "different"));
    }
}
