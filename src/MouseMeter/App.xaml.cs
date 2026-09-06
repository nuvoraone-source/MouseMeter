using System.Linq;
using System.Windows;
using MouseMeter.Infrastructure;
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
    private bool _startMinimized;

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

        // Wurde MouseMeter automatisch mit Windows gestartet, soll nur der
        // Startbildschirm kurz zu sehen sein - das Hauptfenster selbst
        // erscheint dann nicht, sondern MouseMeter liegt direkt in der
        // Ablage neben der Uhr (siehe AutostartManager.StartMinimizedArg).
        _startMinimized = e.Args.Contains(AutostartManager.StartMinimizedArg);

        var splash = new SplashWindow();
        splash.Finished += (_, _) => ShowMainWindow(splash);
        splash.Show();
    }

    private void ShowMainWindow(Window splash)
    {
        var main = new MainWindow(_startMinimized);
        MainWindow = main;
        main.Closed += (_, _) => Shutdown();
        main.Show();

        if (!_startMinimized)
        {
            main.Activate();
        }

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
