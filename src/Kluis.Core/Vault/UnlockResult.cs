namespace Kluis.Core.Vault;

public enum UnlockResult
{
    Unlocked,
    WrongPassphrase,
    NotFound,

    /// <summary>"Remember on this PC" is off, or the stored passphrase could not be read.</summary>
    NotRemembered,

    /// <summary>The database needed an upgrade, but the backup taken first failed, so nothing was changed.</summary>
    BackupFailed,
}
