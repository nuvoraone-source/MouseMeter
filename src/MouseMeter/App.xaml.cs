using System.Windows;
using MouseMeter.Views;

namespace MouseMeter;

/// <summary>
/// Einstiegspunkt der Anwendung: Einzelinstanz sichern, Splashscreen zeigen,
/// anschließend in das Hauptfenster wechseln.
/// </summary>
public partial class App : Application
{
    // Eindeutiger Name pro Windows-Sitzung. Verhindert, dass MouseMeter mehrfach
    // läuft und dieselbe Aktivität doppelt erfasst.
    private const string InstanceMutexName = @"Local\MouseMeter.SingleInstance";

    private Mutex? _instanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _instanceMutex = new Mutex(initiallyOwned: true, InstanceMutexName, out bool isFirstInstance);
        if (!isFirstInstance)
        {
            _instanceMutex.Dispose();
            _instanceMutex = null;
            Shutdown();
            return;
        }

        var splash = new SplashWindow();
        splash.Finished += (_, _) => ShowMainWindow(splash);
        splash.Show();
    }

    private void ShowMainWindow(Window splash)
    {
        var main = new MainWindow();
        MainWindow = main;
        main.Closed += (_, _) => Shutdown();
        main.Show();
        main.Activate();

        // Erst schließen, wenn das Hauptfenster steht - sonst blitzt kurz der Desktop durch.
        splash.Close();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _instanceMutex?.Dispose();
        _instanceMutex = null;
        base.OnExit(e);
    }
}
