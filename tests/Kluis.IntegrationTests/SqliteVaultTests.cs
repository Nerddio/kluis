using System.Text;
using Kluis.Core.Accounts;
using Kluis.Core.Vault;
using Kluis.Data;
using Microsoft.EntityFrameworkCore;

namespace Kluis.IntegrationTests;

/// <summary>
/// Runs against a real SQLite3 Multiple Ciphers file on disk. No in-memory provider and no mock:
/// encryption, key checking and migrations only exist in the real thing.
/// </summary>
public sealed class SqliteVaultTests : IDisposable
{
    private const string Passphrase = "correct horse battery staple";

    private readonly TempFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    [Fact]
    public async Task Create_applies_migrations_to_a_fresh_encrypted_file()
    {
        using var vault = new SqliteVault(_folder.File("kluis.db"));

        await vault.CreateAsync(Passphrase);

        await using var context = vault.CreateDbContext();
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Contains("InitialCreate", (await context.Database.GetAppliedMigrationsAsync()).Single());
    }

    [Fact]
    public async Task Data_written_after_create_is_readable_after_unlocking_with_the_same_passphrase()
    {
        var path = _folder.File("kluis.db");
        using (var vault = new SqliteVault(path))
        {
            await vault.CreateAsync(Passphrase);
            await using var context = vault.CreateDbContext();
            context.Accounts.Add(new Account { Name = "Current account" });
            await context.SaveChangesAsync();
        }

        using var reopened = new SqliteVault(path);
        var result = await reopened.UnlockAsync(Passphrase);

        Assert.Equal(UnlockResult.Unlocked, result);
        await using var readBack = reopened.CreateDbContext();
        var account = Assert.Single(await readBack.Accounts.ToListAsync());
        Assert.Equal("Current account", account.Name);
        Assert.Equal("EUR", account.Currency);
    }

    [Fact]
    public async Task Unlocking_with_the_wrong_passphrase_fails_cleanly_and_leaves_the_file_intact()
    {
        var path = _folder.File("kluis.db");
        using (var vault = new SqliteVault(path))
        {
            await vault.CreateAsync(Passphrase);
        }

        var before = await File.ReadAllBytesAsync(path);

        using var reopened = new SqliteVault(path);
        var result = await reopened.UnlockAsync("not the passphrase at all");

        Assert.Equal(UnlockResult.WrongPassphrase, result);
        Assert.False(reopened.IsUnlocked);
        Assert.Equal(before, await File.ReadAllBytesAsync(path));
        Assert.Equal(UnlockResult.Unlocked, await reopened.UnlockAsync(Passphrase));
    }

    [Fact]
    public async Task The_file_on_disk_does_not_start_with_the_plain_SQLite_header()
    {
        var path = _folder.File("kluis.db");
        using (var vault = new SqliteVault(path))
        {
            await vault.CreateAsync(Passphrase);
        }

        var header = new byte[16];
        await using (var file = File.OpenRead(path))
        {
            await file.ReadExactlyAsync(header);
        }

        Assert.NotEqual("SQLite format 3\0"u8.ToArray(), header);
    }

    [Fact]
    public async Task Stored_text_never_appears_in_plain_form_in_the_file()
    {
        const string marker = "PLAINTEXT-MARKER-7f3a";
        var path = _folder.File("kluis.db");
        using (var vault = new SqliteVault(path))
        {
            await vault.CreateAsync(Passphrase);
            await using var context = vault.CreateDbContext();
            context.Accounts.Add(new Account { Name = marker });
            await context.SaveChangesAsync();
        }

        var contents = Encoding.ASCII.GetString(await File.ReadAllBytesAsync(path));

        Assert.DoesNotContain(marker, contents);
        Assert.DoesNotContain("accounts", contents);
    }

    [Theory]
    [InlineData("it's got 'single quotes'")]
    [InlineData("\"double\" and ; semicolon -- dash")]
    [InlineData("ünïcödé paßphrase ✓")]
    public async Task Passphrases_with_quotes_and_non_ASCII_characters_work_and_are_exact(string passphrase)
    {
        var path = _folder.File("kluis.db");
        using (var vault = new SqliteVault(path))
        {
            await vault.CreateAsync(passphrase);
        }

        using var reopened = new SqliteVault(path);
        Assert.Equal(UnlockResult.WrongPassphrase, await reopened.UnlockAsync(passphrase + " "));
        Assert.Equal(UnlockResult.Unlocked, await reopened.UnlockAsync(passphrase));
    }

    [Fact]
    public async Task Unlocking_when_no_database_exists_reports_not_found_and_creates_nothing()
    {
        var path = _folder.File("kluis.db");
        using var vault = new SqliteVault(path);

        Assert.Equal(UnlockResult.NotFound, await vault.UnlockAsync(Passphrase));
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task Create_refuses_to_overwrite_an_existing_database()
    {
        var path = _folder.File("kluis.db");
        using (var vault = new SqliteVault(path))
        {
            await vault.CreateAsync(Passphrase);
        }

        using var second = new SqliteVault(path);
        await Assert.ThrowsAsync<InvalidOperationException>(() => second.CreateAsync("another passphrase"));
    }

    [Fact]
    public async Task The_cipher_scheme_is_explicitly_SQLCipher_version_4()
    {
        var path = _folder.File("kluis.db");
        using (var vault = new SqliteVault(path))
        {
            await vault.CreateAsync(Passphrase);
        }

        using var connection = EncryptedConnection.OpenExisting(path, Passphrase)!;
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA cipher;";
        Assert.Equal("sqlcipher", command.ExecuteScalar() as string);
        command.CommandText = "PRAGMA legacy;";
        Assert.Equal(4L, Convert.ToInt64(command.ExecuteScalar()));
    }

    [Fact]
    public async Task A_passphrase_containing_a_NUL_character_is_rejected_before_any_file_is_created()
    {
        var path = _folder.File("kluis.db");
        using var vault = new SqliteVault(path);

        await Assert.ThrowsAsync<ArgumentException>(() => vault.CreateAsync("before\0after-the-nul"));
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task Create_makes_the_containing_folder_when_it_does_not_exist()
    {
        var path = Path.Combine(_folder.Path, "Kluis", "kluis.db");
        using var vault = new SqliteVault(path);

        await vault.CreateAsync(Passphrase);

        Assert.True(File.Exists(path));
    }
}
