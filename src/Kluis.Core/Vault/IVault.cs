namespace Kluis.Core.Vault;

/// <summary>
/// The encrypted database file that holds all of a user's data.
/// </summary>
public interface IVault
{
    /// <summary>True when a database file already exists, so the user unlocks rather than creates.</summary>
    bool Exists { get; }

    /// <summary>True once the database has been created or unlocked.</summary>
    bool IsUnlocked { get; }

    /// <summary>True when "remember on this PC" is on: a protected copy of the passphrase is stored.</summary>
    bool IsRemembered { get; }

    /// <summary>
    /// Creates a new encrypted database protected by <paramref name="passphrase"/> and leaves it unlocked.
    /// When <paramref name="remember"/> is true the passphrase is also stored for this Windows user.
    /// </summary>
    Task CreateAsync(string passphrase, bool remember = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the existing database with <paramref name="passphrase"/>. When it succeeds and
    /// <paramref name="remember"/> is true, the passphrase is stored for this Windows user.
    /// </summary>
    Task<UnlockResult> UnlockAsync(string passphrase, bool remember = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlocks with the remembered passphrase, if there is one. If it no longer unlocks the database,
    /// it is deleted and the caller falls back to asking for the passphrase.
    /// </summary>
    Task<UnlockResult> UnlockWithRememberedAsync(CancellationToken cancellationToken = default);

    /// <summary>Turns "remember on this PC" off by deleting the stored passphrase.</summary>
    void Forget();
}
