using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Kluis.EndToEndTests;

/// <summary>
/// End-to-end smoke pass over the critical paths that exist so far: the real app, a real encrypted
/// database in a temp folder, driven through the window the way a user would. Excluded from
/// build-and-test by its trait and run by the separate e2e job.
/// </summary>
[Trait("Category", "E2E")]
public sealed class SmokeTests : IDisposable
{
    // Synthetic test value, not anyone's real passphrase.
    private const string Passphrase = "synthetic e2e passphrase";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "kluis-e2e", Guid.NewGuid().ToString("N"));

    public SmokeTests() => Directory.CreateDirectory(_folder);

    private string DatabasePath => Path.Combine(_folder, "kluis.db");

    public void Dispose()
    {
        try
        {
            Directory.Delete(_folder, recursive: true);
        }
        catch (IOException)
        {
            // Best effort; the folder is in the temp directory.
        }
    }

    [Fact]
    public async Task First_run_creates_an_encrypted_database_and_a_restart_unlocks_it()
    {
        await using (var app = await KluisApp.StartAsync(DatabasePath))
        {
            var page = app.Page;
            await Expect(Heading(page)).ToHaveTextAsync("Welcome to Kluis");

            await TypeAsync(page, "#passphrase", Passphrase);
            await TypeAsync(page, "#confirmation", Passphrase);
            await page.ClickAsync("button[type=submit]");

            await Expect(Heading(page)).ToHaveTextAsync("Kluis");
        }

        Assert.True(File.Exists(DatabasePath));

        await using (var app = await KluisApp.StartAsync(DatabasePath))
        {
            var page = app.Page;
            await Expect(Heading(page)).ToHaveTextAsync("Unlock Kluis");

            await TypeAsync(page, "#passphrase", "not the passphrase");
            await page.ClickAsync("button[type=submit]");
            await Expect(page.GetByRole(AriaRole.Alert)).ToHaveTextAsync("That passphrase did not unlock the database.");

            await TypeAsync(page, "#passphrase", Passphrase);
            await page.ClickAsync("button[type=submit]");
            await Expect(Heading(page)).ToHaveTextAsync("Kluis");
        }
    }

    private static ILocator Heading(IPage page) => page.Locator("h1");

    /// <summary>
    /// Blazor's @bind listens for "change", which a browser only fires on blur; dispatching it
    /// explicitly makes the value reach the component before the click, with no race.
    /// </summary>
    private static async Task TypeAsync(IPage page, string selector, string value)
    {
        var field = page.Locator(selector);
        await field.FillAsync(value);
        await field.DispatchEventAsync("change");
    }
}
