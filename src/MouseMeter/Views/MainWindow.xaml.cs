using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MouseMeter.Infrastructure;

namespace MouseMeter.Views;

/// <summary>
/// Hauptfenster von MouseMeter: Kopfbereich, Zeitraum-Auswahl, Dashboard mit
/// den neun Live-Kacheln, Diagrammfläche und Statusleiste.
///
/// Mausstrecke (v0.3.0), Geschwindigkeit, Durchschnitt und Rekord (v0.4.0)
/// werden bereits live erfasst. Klicks und Tastaturanschläge folgen in den
/// nächsten Versionen (siehe Gesamtkonzept Abschnitt 36) und zeigen bis
/// dahin weiterhin Null.
/// </summary>
public partial class MainWindow : Window
{
    // Aktualisierungsintervall der Anzeige. Die Erfassung selbst laeuft ueber
    // den systemweiten Hook in Echtzeit - fuer die Oberflaeche reicht ein
    // deutlich selteneres Neuzeichnen, um die UI nicht unnoetig zu belasten
    // (Abschnitt 31, ressourcenschonender Betrieb). Dasselbe Intervall dient
    // als Messfenster fuer die aktuelle Geschwindigkeit.
    private static readonly TimeSpan DisplayRefreshInterval = TimeSpan.FromMilliseconds(200);

    private readonly MouseDistanceTracker _distanceTracker = new();
    private readonly DispatcherTimer _displayTimer;
    private readonly Stopwatch _sampleStopwatch = new();

    // Momentaufnahme der Strecke beim letzten Neuzeichnen - Grundlage fuer
    // die aktuelle Geschwindigkeit (Distanz-Differenz je Messfenster).
    private double _lastSampleDistanceMeters;

    // Persönlicher Geschwindigkeitsrekord dieser Sitzung. Eine dauerhafte
    // Speicherung über Programmstarts hinweg folgt mit v0.7.0.
    private double _recordSpeedMetersPerSecond;

    public MainWindow()
    {
        InitializeComponent();

        WindowTheme.ApplyDarkTitleBar(this);

        Title = $"{AppInfo.Name} {AppInfo.DisplayVersion}";
        VersionText.Text = AppInfo.DisplayVersion;

        _displayTimer = new DispatcherTimer { Interval = DisplayRefreshInterval };
        _displayTimer.Tick += (_, _) => RenderDashboard();

        RenderDashboard();

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _distanceTracker.Start();
        _sampleStopwatch.Start();
        _displayTimer.Start();

        StatusText.Text = "Aktiv";
        StatusDot.Fill = (Brush)FindResource("StatusOkBrush");
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _displayTimer.Stop();
        _distanceTracker.Dispose();
    }

    /// <summary>
    /// Schreibt die aktuellen Messwerte in die Dashboard-Kacheln.
    /// Mausstrecke, Geschwindigkeit, Durchschnitt und Rekord stammen aus der
    /// echten Erfassung. Klicks und Tastaturanschläge zeigen noch Nullwerte,
    /// bis ihre Erfassung in einer späteren Version dazukommt - aber bereits
    /// im endgültigen Zahlenformat (Abschnitt 33).
    /// </summary>
    private void RenderDashboard()
    {
        double totalDistance = _distanceTracker.TotalDistanceMeters;

        // Aktuelle Geschwindigkeit: zurückgelegte Strecke seit dem letzten
        // Neuzeichnen, geteilt durch die dafür tatsächlich vergangene Zeit
        // (nicht das nominelle Intervall, das durch UI-Jitter leicht abweicht).
        double sampleSeconds = _sampleStopwatch.Elapsed.TotalSeconds;
        double currentSpeed = sampleSeconds > 0
            ? (totalDistance - _lastSampleDistanceMeters) / sampleSeconds
            : 0;
        _lastSampleDistanceMeters = totalDistance;
        _sampleStopwatch.Restart();

        _recordSpeedMetersPerSecond = Math.Max(_recordSpeedMetersPerSecond, currentSpeed);

        double totalSeconds = _distanceTracker.Elapsed.TotalSeconds;
        double averageSpeed = totalSeconds > 0 ? totalDistance / totalSeconds : 0;

        DistanceValueText.Text = Formatting.Distance(totalDistance);
        SpeedValueText.Text = Formatting.Speed(currentSpeed);
        AverageValueText.Text = Formatting.Speed(averageSpeed);
        RecordValueText.Text = Formatting.Speed(_recordSpeedMetersPerSecond);

        LeftClicksValueText.Text = Formatting.Count(0);
        RightClicksValueText.Text = Formatting.Count(0);
        MiddleClicksValueText.Text = Formatting.Count(0);
        TotalClicksValueText.Text = Formatting.Count(0);

        KeystrokesValueText.Text = Formatting.Count(0);
    }

    private void OnPeriodChanged(object sender, RoutedEventArgs e)
    {
        // Noch ohne gespeicherte Historie: die Auswahl wird bereits entgegengenommen,
        // wirkt sich aber erst ab v0.7.0/v0.8.0 (Datenspeicherung, Zeiträume) aus.
        if (sender is not RadioButton { IsChecked: true } selected || ChartPlaceholderText is null)
        {
            return;
        }

        string period = selected.Name switch
        {
            nameof(PeriodWeek) => "die Woche",
            nameof(PeriodMonth) => "den Monat",
            nameof(PeriodYear) => "das Jahr",
            _ => "den Tag",
        };

        ChartPlaceholderText.Text =
            $"Diagramm für {period} folgt, sobald echte Messwerte erfasst und gespeichert werden.";
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        new SettingsWindow { Owner = this }.ShowDialog();
    }

    private void OnNuvoraClick(object sender, MouseButtonEventArgs e)
    {
        new ContactWindow { Owner = this }.ShowDialog();
    }
}
