using Kluis.Core.Vault;

namespace Kluis.UI.Tests;

/// <summary>
/// A fake: a working, in-memory stand-in for the real encrypted vault, so component tests exercise
/// the screens without touching disk. The real vault is covered by the integration tests.
/// </summary>
public sealed class FakeVault : IVault
{
    public string? StoredPassphrase { get; set; }

    public bool ThrowOnAccess { get; set; }

    public bool FailBackup { get; set; }

    public bool Exists => StoredPassphrase is not null;

    public bool IsUnlocked { get; private set; }

    public List<string> Attempts { get; } = [];

    public Task CreateAsync(string passphrase, CancellationToken cancellationToken = default)
    {
        Attempts.Add(passphrase);
        if (ThrowOnAccess)
        {
            throw new IOException(@"C:\secret\path\kluis.db is locked");
        }

        StoredPassphrase = passphrase;
        IsUnlocked = true;
        return Task.CompletedTask;
    }

    public Task<UnlockResult> UnlockAsync(string passphrase, CancellationToken cancellationToken = default)
    {
        Attempts.Add(passphrase);
        if (ThrowOnAccess)
        {
            throw new IOException(@"C:\secret\path\kluis.db is locked");
        }

        if (StoredPassphrase is null)
        {
            return Task.FromResult(UnlockResult.NotFound);
        }

        if (FailBackup && passphrase == StoredPassphrase)
        {
            return Task.FromResult(UnlockResult.BackupFailed);
        }

        IsUnlocked = passphrase == StoredPassphrase;
        return Task.FromResult(IsUnlocked ? UnlockResult.Unlocked : UnlockResult.WrongPassphrase);
    }
}
