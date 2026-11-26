using System.Text;
using Kluis.Core.Vault;
using Kluis.Data;

namespace Kluis.IntegrationTests;

/// <summary>
/// Real DPAPI, real files, real encrypted database. These run as whichever Windows user runs the tests,
/// so they prove "the same user can unlock"; a different user cannot be simulated inside one test run.
/// </summary>
public sealed class RememberOnThisPcTests : IDisposable
{
    private const string Passphrase = "correct horse battery staple";

    private readonly TempFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    private string DatabasePath => _folder.File("kluis.db");

    [Fact]
    public async Task Remember_is_off_by_default()
    {
        using var vault = new SqliteVault(DatabasePath);

        await vault.CreateAsync(Passphrase);

        Assert.False(vault.IsRemembered);
        Assert.False(RememberedPassphrase.For(DatabasePath).Exists);
    }

    [Fact]
    public async Task A_remembered_passphrase_unlocks_the_database_on_the_next_start_for_the_same_user()
    {
        using (var vault = new SqliteVault(DatabasePath))
        {
            await vault.CreateAsync(Passphrase, remember: true);
            Assert.True(vault.IsRemembered);
        }

        using var nextStart = new SqliteVault(DatabasePath);

        Assert.Equal(UnlockResult.Unlocked, await nextStart.UnlockWithRememberedAsync());
        Assert.True(nextStart.IsUnlocked);
    }

    [Fact]
    public async Task Choosing_remember_when_unlocking_stores_the_passphrase()
    {
        using (var vault = new SqliteVault(DatabasePath))
        {
            await vault.CreateAsync(Passphrase);
        }

        using var unlocked = new SqliteVault(DatabasePath);
        Assert.Equal(UnlockResult.Unlocked, await unlocked.UnlockAsync(Passphrase, remember: true));

        Assert.True(unlocked.IsRemembered);
    }

    [Fact]
    public async Task A_wrong_passphrase_with_remember_ticked_stores_nothing()
    {
        using (var vault = new SqliteVault(DatabasePath))
        {
            await vault.CreateAsync(Passphrase);
        }

        using var attempt = new SqliteVault(DatabasePath);
        Assert.Equal(UnlockResult.WrongPassphrase, await attempt.UnlockAsync("not the passphrase", remember: true));

        Assert.False(attempt.IsRemembered);
    }

    [Fact]
    public async Task The_protected_file_does_not_contain_the_passphrase_in_plain_form()
    {
        using var vault = new SqliteVault(DatabasePath);
        await vault.CreateAsync(Passphrase, remember: true);

        var bytes = await File.ReadAllBytesAsync(RememberedPassphrase.For(DatabasePath).Path);

        Assert.DoesNotContain(Passphrase, Encoding.UTF8.GetString(bytes));
        Assert.DoesNotContain(Passphrase, Encoding.Unicode.GetString(bytes));
        Assert.DoesNotContain("correct horse", Encoding.Latin1.GetString(bytes));
    }

    [Fact]
    public async Task Forget_deletes_the_stored_passphrase()
    {
        using var vault = new SqliteVault(DatabasePath);
        await vault.CreateAsync(Passphrase, remember: true);

        vault.Forget();

        Assert.False(vault.IsRemembered);
        Assert.False(File.Exists(RememberedPassphrase.For(DatabasePath).Path));
    }

    [Fact]
    public async Task A_remembered_passphrase_that_no_longer_unlocks_is_deleted_and_the_user_is_asked()
    {
        using (var vault = new SqliteVault(DatabasePath))
        {
            await vault.CreateAsync(Passphrase, remember: true);
        }

        // The database is replaced by one with a different passphrase; the remembered one is now stale.
        var remembered = await File.ReadAllBytesAsync(RememberedPassphrase.For(DatabasePath).Path);
        File.Delete(DatabasePath);
        using (var replacement = new SqliteVault(DatabasePath))
        {
            await replacement.CreateAsync("a completely different passphrase");
        }

        await File.WriteAllBytesAsync(RememberedPassphrase.For(DatabasePath).Path, remembered);

        using var nextStart = new SqliteVault(DatabasePath);
        Assert.Equal(UnlockResult.WrongPassphrase, await nextStart.UnlockWithRememberedAsync());
        Assert.False(nextStart.IsUnlocked);
        Assert.False(nextStart.IsRemembered);
    }

    [Fact]
    public async Task A_corrupted_remembered_file_is_treated_as_absent_and_deleted()
    {
        using (var vault = new SqliteVault(DatabasePath))
        {
            await vault.CreateAsync(Passphrase);
        }

        await File.WriteAllBytesAsync(RememberedPassphrase.For(DatabasePath).Path, [1, 2, 3, 4, 5]);

        using var nextStart = new SqliteVault(DatabasePath);
        Assert.Equal(UnlockResult.NotRemembered, await nextStart.UnlockWithRememberedAsync());
        Assert.False(nextStart.IsRemembered);
    }

    [Fact]
    public async Task Creating_without_remember_clears_a_leftover_remembered_file()
    {
        await File.WriteAllBytesAsync(RememberedPassphrase.For(DatabasePath).Path, [9, 9, 9]);
        using var vault = new SqliteVault(DatabasePath);

        await vault.CreateAsync(Passphrase, remember: false);

        Assert.False(vault.IsRemembered);
    }

    [Fact]
    public void The_remembered_file_lives_beside_its_database()
    {
        var remembered = RememberedPassphrase.For(DatabasePath);

        Assert.Equal(_folder.File("kluis.remembered"), remembered.Path);
    }
}
