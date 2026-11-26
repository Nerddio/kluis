using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Playwright;

namespace Kluis.EndToEndTests;

/// <summary>
/// Launches the real Kluis.App.exe against a throwaway database and attaches Playwright to its
/// WebView2 through the Chrome DevTools Protocol, so tests drive the actual window a user sees.
/// </summary>
public sealed class KluisApp : IAsyncDisposable
{
    private readonly Process _process;
    private readonly IPlaywright _playwright;
    private readonly IBrowser _browser;

    private KluisApp(Process process, IPlaywright playwright, IBrowser browser, IPage page)
    {
        _process = process;
        _playwright = playwright;
        _browser = browser;
        Page = page;
    }

    public IPage Page { get; }

    public static async Task<KluisApp> StartAsync(string databasePath)
    {
        var port = FreePort();
        var startInfo = new ProcessStartInfo(AppExecutable()) { UseShellExecute = false };
        startInfo.ArgumentList.Add("--database");
        startInfo.ArgumentList.Add(databasePath);

        // Bound to 127.0.0.1 by WebView2, and only for this test process: the shipped app never sets it.
        startInfo.Environment["WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS"] = $"--remote-debugging-port={port}";

        var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Kluis.App did not start.");
        try
        {
            var endpoint = $"http://127.0.0.1:{port}";
            await WaitForDevToolsAsync(endpoint, process);

            var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.ConnectOverCDPAsync(endpoint);
            var page = await FirstAppPageAsync(browser);
            return new KluisApp(process, playwright, browser, page);
        }
        catch
        {
            Kill(process);
            throw;
        }
    }

    /// <summary>
    /// The app to launch: KLUIS_APP_EXE when set (the release pipeline points it at the packaged build),
    /// otherwise the Kluis.App build output for the same configuration as these tests.
    /// </summary>
    public static string AppExecutable()
    {
        var overridden = Environment.GetEnvironmentVariable("KLUIS_APP_EXE");
        if (!string.IsNullOrEmpty(overridden))
        {
            return overridden;
        }

        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Kluis.slnx")))
        {
            root = root.Parent;
        }

        if (root is null)
        {
            throw new InvalidOperationException("Could not find the repository root (Kluis.slnx).");
        }

#if DEBUG
        const string configuration = "Debug";
#else
        const string configuration = "Release";
#endif
        var bin = Path.Combine(root.FullName, "src", "Kluis.App", "bin", configuration);
        // Newest build wins, so a stale output folder from an older target framework is ignored.
        return Directory.GetFiles(bin, "Kluis.App.exe", SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .First();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _browser.CloseAsync();
        }
        catch (PlaywrightException)
        {
            // The app may already be gone; closing the connection is best effort.
        }

        _playwright.Dispose();
        Kill(_process);
        _process.Dispose();
    }

    private static async Task<IPage> FirstAppPageAsync(IBrowser browser)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            var page = browser.Contexts.SelectMany(c => c.Pages).FirstOrDefault();
            if (page is not null)
            {
                return page;
            }

            await Task.Delay(200);
        }

        throw new TimeoutException("WebView2 exposed no page.");
    }

    private static async Task WaitForDevToolsAsync(string endpoint, Process process)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException($"Kluis.App exited during startup with code {process.ExitCode}.");
            }

            try
            {
                using var response = await http.GetAsync($"{endpoint}/json/version");
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // Not listening yet.
            }

            await Task.Delay(250);
        }

        throw new TimeoutException("WebView2 did not open its DevTools endpoint within 60 seconds.");
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static void Kill(Process process)
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(10_000);
        }
    }
}
