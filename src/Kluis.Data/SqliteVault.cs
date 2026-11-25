using Kluis.Core.Vault;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Kluis.Data;

/// <summary>
/// The encrypted SQLite database file. After a successful create or unlock it keeps one open,
/// keyed connection for the lifetime of the app, which every DbContext shares.
/// </summary>
public sealed class SqliteVault(string databasePath, TimeProvider? timeProvider = null) : IVault, IDisposable
{
    public const string BackupExtension = ".kluisbackup";

    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
    private SqliteConnection? _connection;

    /// <summary>
    /// %LOCALAPPDATA%\Kluis\kluis.db. Local rather than roaming, and outside Documents and the Desktop,
    /// which cloud backup tools such as OneDrive commonly sync off the machine.
    /// </summary>
    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kluis", "kluis.db");

    public string DatabasePath { get; } = databasePath;

    public bool Exists => File.Exists(DatabasePath);

    public bool IsUnlocked => _connection is not null;

    public async Task CreateAsync(string passphrase, CancellationToken cancellationToken = default)
    {
        if (Exists)
        {
            throw new InvalidOperationException("A database already exists; refusing to overwrite it.");
        }

        var connection = EncryptedConnection.Create(DatabasePath, passphrase);
        await MigrateAsync(connection, cancellationToken);
        Replace(connection);
    }

    public async Task<UnlockResult> UnlockAsync(string passphrase, CancellationToken cancellationToken = default)
    {
        if (!Exists)
        {
            return UnlockResult.NotFound;
        }

        var connection = EncryptedConnection.OpenExisting(DatabasePath, passphrase);
        if (connection is null)
        {
            return UnlockResult.WrongPassphrase;
        }

        if (await HasPendingMigrationsAsync(connection, cancellationToken) && !TryBackUp(connection))
        {
            // Never migrate without a backup: a migration that goes wrong must not cost the user their data.
            connection.Dispose();
            return UnlockResult.BackupFailed;
        }

        await MigrateAsync(connection, cancellationToken);
        Replace(connection);
        return UnlockResult.Unlocked;
    }

    /// <summary>
    /// Copies the encrypted file byte for byte next to the original, so the backup is protected by the
    /// same passphrase and opens exactly like the database did before the upgrade.
    /// </summary>
    private bool TryBackUp(SqliteConnection connection)
    {
        // A plain file copy is only a consistent snapshot in rollback-journal mode with no transaction
        // open, which is the state right after unlocking. In WAL mode recent writes would sit in a
        // separate -wal file and be missed, so refuse rather than produce an incomplete backup.
        using (var journalMode = connection.CreateCommand())
        {
            journalMode.CommandText = "PRAGMA journal_mode;";
            if (!string.Equals(journalMode.ExecuteScalar() as string, "delete", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        try
        {
            File.Copy(DatabasePath, BackupPathFor(_time.GetUtcNow()), overwrite: false);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>kluis-20261125T181500Z-before-migration.kluisbackup, in the database's own folder. UTC avoids daylight-saving duplicates.</summary>
    public string BackupPathFor(DateTimeOffset utcNow) => Path.Combine(
        Path.GetDirectoryName(Path.GetFullPath(DatabasePath))!,
        $"{Path.GetFileNameWithoutExtension(DatabasePath)}-{utcNow.UtcDateTime:yyyyMMdd'T'HHmmss'Z'}-before-migration{BackupExtension}");

    private static async Task<bool> HasPendingMigrationsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var context = new KluisDbContext(Options(connection));
        return (await context.Database.GetPendingMigrationsAsync(cancellationToken)).Any();
    }

    public KluisDbContext CreateDbContext() =>
        new(Options(_connection ?? throw new InvalidOperationException("The vault is locked.")));

    public void Dispose()
    {
        _connection?.Dispose();
        _connection = null;
    }

    private static async Task MigrateAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var context = new KluisDbContext(Options(connection));
        await context.Database.MigrateAsync(cancellationToken);
    }

    private static DbContextOptions<KluisDbContext> Options(SqliteConnection connection) =>
        new DbContextOptionsBuilder<KluisDbContext>().UseSqlite(connection).Options;

    private void Replace(SqliteConnection connection)
    {
        _connection?.Dispose();
        _connection = connection;
    }
}
