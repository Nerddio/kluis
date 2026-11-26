using Velopack;

namespace Kluis.App;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        // Must run first: when the Velopack installer or uninstaller starts the app with a hook argument,
        // this handles it and exits before any window opens. It makes no network call. Kluis never
        // creates an UpdateManager, so there is no update check (v1 makes no network calls at all).
        VelopackApp.Build().Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
