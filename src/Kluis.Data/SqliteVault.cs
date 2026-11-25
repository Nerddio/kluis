using Kluis.Core.Vault;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Kluis.Data;

/// <summary>
/// The encrypted SQLite database file. After a successful create or unlock it keeps one open,
/// keyed connection for the lifetime of the app, which every DbContext shares.
/// </summary>
public sealed class SqliteVault(string databasePath) : IVault, IDisposable
{
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

        await MigrateAsync(connection, cancellationToken);
        Replace(connection);
        return UnlockResult.Unlocked;
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
