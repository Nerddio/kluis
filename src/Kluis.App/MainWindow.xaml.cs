using System.Windows;
using Kluis.Core.Vault;
using Kluis.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Kluis.App;

public partial class MainWindow : Window
{
    private readonly ServiceProvider _services;

    public MainWindow()
    {
        var services = new ServiceCollection();
        services.AddWpfBlazorWebView();
        services.AddSingleton(_ => new SqliteVault(DatabasePathFrom(Environment.GetCommandLineArgs())));
        services.AddSingleton<IVault>(provider => provider.GetRequiredService<SqliteVault>());
        _services = services.BuildServiceProvider();
        Resources.Add("services", _services);

        InitializeComponent();

        // Disposing the provider disposes the vault, which closes the keyed connection.
        Closed += (_, _) => _services.Dispose();
    }

    /// <summary>
    /// `--database &lt;path&gt;` points the app at another file, used by end-to-end tests and manual
    /// testing so they never touch the real database in %LOCALAPPDATA%\Kluis.
    /// </summary>
    private static string DatabasePathFrom(string[] args)
    {
        var index = Array.IndexOf(args, "--database");
        return index >= 0 && index + 1 < args.Length ? System.IO.Path.GetFullPath(args[index + 1]) : SqliteVault.DefaultPath;
    }
}
