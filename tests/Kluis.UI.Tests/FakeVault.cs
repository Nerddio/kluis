using Kluis.Core.Vault;

namespace Kluis.UI.Tests;

/// <summary>
/// A fake: a working, in-memory stand-in for the real encrypted vault, so component tests exercise
/// the screens without touching disk or DPAPI. The real vault is covered by the integration tests.
/// </summary>
public sealed class FakeVault : IVault
{
    public string? StoredPassphrase { get; set; }

    public string? RememberedPassphrase { get; set; }

    public bool ThrowOnAccess { get; set; }

    public bool FailBackup { get; set; }

    public bool Exists => StoredPassphrase is not null;

    public bool IsUnlocked { get; private set; }

    public bool IsRemembered => RememberedPassphrase is not null;

    public List<(string Passphrase, bool Remember)> Attempts { get; } = [];

    public Task CreateAsync(string passphrase, bool remember = false, CancellationToken cancellationToken = default)
    {
        Attempts.Add((passphrase, remember));
        if (ThrowOnAccess)
        {
            throw new IOException(@"C:\secret\path\kluis.db is locked");
        }

        StoredPassphrase = passphrase;
        RememberedPassphrase = remember ? passphrase : null;
        IsUnlocked = true;
        return Task.CompletedTask;
    }

    public Task<UnlockResult> UnlockAsync(string passphrase, bool remember = false, CancellationToken cancellationToken = default)
    {
        Attempts.Add((passphrase, remember));
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
        if (IsUnlocked && remember)
        {
            RememberedPassphrase = passphrase;
        }

        return Task.FromResult(IsUnlocked ? UnlockResult.Unlocked : UnlockResult.WrongPassphrase);
    }

    public async Task<UnlockResult> UnlockWithRememberedAsync(CancellationToken cancellationToken = default)
    {
        if (RememberedPassphrase is null)
        {
            return UnlockResult.NotRemembered;
        }

        var result = await UnlockAsync(RememberedPassphrase, cancellationToken: cancellationToken);
        if (result == UnlockResult.WrongPassphrase)
        {
            RememberedPassphrase = null;
        }

        return result;
    }

    public void Forget() => RememberedPassphrase = null;
}
