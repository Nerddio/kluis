using Kluis.Core.Vault;
using Kluis.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Kluis.IntegrationTests;

public sealed class BackupBeforeMigrationTests : IDisposable
{
    private const string Passphrase = "correct horse battery staple";

    private static readonly DateTimeOffset Now = new(2026, 3, 29, 1, 30, 0, TimeSpan.Zero);

    private readonly TempFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    private string DatabasePath => _folder.File("kluis.db");

    [Fact]
    public async Task A_database_with_a_pending_migration_is_backed_up_before_it_is_migrated()
    {
        await CreateDatabaseThatLooksOlderThanThisVersionAsync();
        using var vault = new SqliteVault(DatabasePath, new FixedTime(Now));

        var result = await vault.UnlockAsync(Passphrase);

        Assert.Equal(UnlockResult.Unlocked, result);
        var backupPath = vault.BackupPathFor(Now);
        Assert.Equal(_folder.File("kluis-20260329T013000Z-before-migration.kluisbackup"), backupPath);

        // The backup is the pre-migration database: same passphrase, old rows, no migration applied.
        using var backup = EncryptedConnection.OpenExisting(backupPath, Passphrase);
        Assert.NotNull(backup);
        Assert.Equal("kept from the old version", Scalar(backup, "SELECT note FROM legacy_marker;"));
        Assert.Equal(0L, Scalar(backup, "SELECT count(*) FROM __EFMigrationsHistory;"));

        // The database itself has been migrated.
        await using var context = vault.CreateDbContext();
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task No_backup_is_written_when_there_are_no_pending_migrations()
    {
        using (var vault = new SqliteVault(DatabasePath))
        {
            await vault.CreateAsync(Passphrase);
        }

        using var reopened = new SqliteVault(DatabasePath, new FixedTime(Now));
        Assert.Equal(UnlockResult.Unlocked, await reopened.UnlockAsync(Passphrase));

        Assert.Empty(Directory.GetFiles(_folder.Path, "*" + SqliteVault.BackupExtension));
    }

    [Fact]
    public async Task When_the_backup_fails_the_migration_is_not_applied_and_the_vault_stays_locked()
    {
        await CreateDatabaseThatLooksOlderThanThisVersionAsync();
        using var vault = new SqliteVault(DatabasePath, new FixedTime(Now));

        // A folder already sitting at the backup's path makes the copy fail.
        Directory.CreateDirectory(vault.BackupPathFor(Now));

        var result = await vault.UnlockAsync(Passphrase);

        Assert.Equal(UnlockResult.BackupFailed, result);
        Assert.False(vault.IsUnlocked);
        using var untouched = EncryptedConnection.OpenExisting(DatabasePath, Passphrase)!;
        Assert.Equal(0L, Scalar(untouched, "SELECT count(*) FROM __EFMigrationsHistory;"));
        Assert.Equal("kept from the old version", Scalar(untouched, "SELECT note FROM legacy_marker;"));
    }

    /// <summary>
    /// Builds a database as an earlier version of Kluis would have left it: a marker row of "old" data,
    /// and no migrations applied yet, so this version sees InitialCreate as pending.
    /// </summary>
    private async Task CreateDatabaseThatLooksOlderThanThisVersionAsync()
    {
        using (var vault = new SqliteVault(DatabasePath))
        {
            await vault.CreateAsync(Passphrase);
        }

        using var connection = EncryptedConnection.OpenExisting(DatabasePath, Passphrase)!;
        Execute(connection, "CREATE TABLE legacy_marker (note TEXT NOT NULL);");
        Execute(connection, "INSERT INTO legacy_marker (note) VALUES ('kept from the old version');");
        Execute(connection, "DROP TABLE accounts;");
        Execute(connection, "DELETE FROM __EFMigrationsHistory;");
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static object? Scalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar();
    }

    /// <summary>A clock frozen at one instant, so the backup's timestamped name is predictable.</summary>
    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
