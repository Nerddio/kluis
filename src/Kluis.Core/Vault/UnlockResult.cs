namespace Kluis.Core.Vault;

public enum UnlockResult
{
    Unlocked,
    WrongPassphrase,
    NotFound,

    /// <summary>The database needed an upgrade, but the backup taken first failed, so nothing was changed.</summary>
    BackupFailed,
}
