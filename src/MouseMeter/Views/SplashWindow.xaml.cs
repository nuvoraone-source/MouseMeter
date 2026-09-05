using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using MouseMeter.Infrastructure;

namespace MouseMeter.Views;

/// <summary>
/// Kurzer Startbildschirm mit Logo, Wortmarke und Claim.
/// Meldet ueber <see cref="Finished"/>, wenn in das Hauptfenster gewechselt werden kann.
/// </summary>
public partial class SplashWindow : Window
{
    // Gesamtdauer der sichtbaren Anzeige. Bewusst kurz gehalten: der Splashscreen
    // soll den Programmstart nicht verzögern.
    private static readonly TimeSpan VisibleDuration = TimeSpan.FromMilliseconds(1700);

    private readonly DispatcherTimer _holdTimer;

    /// <summary>Wird ausgelöst, sobald der Startbildschirm ausgeblendet ist.</summary>
    public event EventHandler? Finished;

    public SplashWindow()
    {
        InitializeComponent();

        VersionText.Text = AppInfo.DisplayVersion;

        _holdTimer = new DispatcherTimer { Interval = VisibleDuration };
        _holdTimer.Tick += OnHoldElapsed;

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        BeginStoryboard((Storyboard)Resources["FadeIn"]);
        _holdTimer.Start();
    }

    private void OnHoldElapsed(object? sender, EventArgs e)
    {
        _holdTimer.Stop();
        _holdTimer.Tick -= OnHoldElapsed;

        var fadeOut = (Storyboard)Resources["FadeOut"];
        fadeOut.Completed += (_, _) => Finished?.Invoke(this, EventArgs.Empty);
        BeginStoryboard(fadeOut);
    }
}
