namespace Kluis.Core.Vault;

public enum PassphraseProblem
{
    None,
    Empty,
    TooShort,
    Mismatch,
}

/// <summary>
/// Rules for a new passphrase. There is no recovery, so the checks only prevent a passphrase
/// that is short enough to guess, or mistyped so that the user locks themselves out.
/// </summary>
public static class PassphrasePolicy
{
    public const int MinimumLength = 12;

    public static PassphraseProblem Validate(string? passphrase, string? confirmation)
    {
        if (string.IsNullOrEmpty(passphrase))
        {
            return PassphraseProblem.Empty;
        }

        if (passphrase.Length < MinimumLength)
        {
            return PassphraseProblem.TooShort;
        }

        // Ordinal: the passphrase is key material, so no culture-aware or case-insensitive matching.
        if (!string.Equals(passphrase, confirmation, StringComparison.Ordinal))
        {
            return PassphraseProblem.Mismatch;
        }

        return PassphraseProblem.None;
    }
}
