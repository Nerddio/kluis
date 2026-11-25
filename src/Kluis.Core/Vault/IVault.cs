namespace Kluis.Core.Vault;

/// <summary>
/// The encrypted database file that holds all of a user's data.
/// </summary>
public interface IVault
{
    /// <summary>True when a database file already exists, so the user unlocks rather than creates.</summary>
    bool Exists { get; }

    /// <summary>True once <see cref="CreateAsync"/> or <see cref="UnlockAsync"/> has succeeded.</summary>
    bool IsUnlocked { get; }

    /// <summary>Creates a new encrypted database protected by <paramref name="passphrase"/> and leaves it unlocked.</summary>
    Task CreateAsync(string passphrase, CancellationToken cancellationToken = default);

    /// <summary>Opens the existing database with <paramref name="passphrase"/>.</summary>
    Task<UnlockResult> UnlockAsync(string passphrase, CancellationToken cancellationToken = default);
}
