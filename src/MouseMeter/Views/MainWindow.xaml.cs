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
/// Mausstrecke (v0.3.0), Geschwindigkeit, Durchschnitt und Rekord (v0.4.0),
/// Links-, Rechts-, Mittel- und Gesamtklicks (v0.5.0), Tastaturanschläge
/// (v0.6.0) sowie Scrollrad-Ausschläge (v0.7.0) werden bereits live erfasst -
/// alle Dashboard-Kacheln zeigen damit echte Werte (Gesamtkonzept Abschnitt
/// 36, mit vorgezogenem Ablage-Minimieren und Scrollrad-Erfassung in v0.7.0).
/// Beim Minimieren verschwindet MouseMeter aus der Taskleiste und legt
/// stattdessen ein Symbol in die Ablage neben der Uhr.
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
    private readonly MouseClickTracker _clickTracker = new();
    private readonly KeystrokeTracker _keystrokeTracker = new();
    private readonly ScrollTracker _scrollTracker = new();
    private readonly TrayIconController _trayIcon;
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

        // Dasselbe Symbol wie das Anwendungsfenster - direkt aus der laufenden
        // EXE gelesen, kein separates Laden der Ressourcendatei nötig. Der
        // Controller übernimmt den Besitz und gibt das Icon-Handle erst beim
        // eigenen Dispose frei (ein "using" hier hätte es sofort zerstört).
        var appIcon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!)!;
        _trayIcon = new TrayIconController(this, appIcon);
        _trayIcon.RestoreRequested += OnTrayRestoreRequested;
        _trayIcon.ExitRequested += OnTrayExitRequested;

        RenderDashboard();

        Loaded += OnLoaded;
        StateChanged += OnStateChanged;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _distanceTracker.Start();
        _clickTracker.Start();
        _keystrokeTracker.Start();
        _scrollTracker.Start();
        _sampleStopwatch.Start();
        _displayTimer.Start();

        StatusText.Text = "Aktiv";
        StatusDot.Fill = (Brush)FindResource("StatusOkBrush");
    }

    // Beim Minimieren verschwindet MouseMeter komplett aus der Taskleiste und
    // legt stattdessen nur ein Symbol in die Ablage neben der Uhr - die
    // Erfassung im Hintergrund läuft dabei unverändert weiter.
    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            ShowInTaskbar = false;
            _trayIcon.Show();
        }
    }

    private void OnTrayRestoreRequested()
    {
        _trayIcon.Hide();
        ShowInTaskbar = true;
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void OnTrayExitRequested() => Close();

    private void OnClosed(object? sender, EventArgs e)
    {
        _displayTimer.Stop();
        _distanceTracker.Dispose();
        _clickTracker.Dispose();
        _keystrokeTracker.Dispose();
        _scrollTracker.Dispose();
        _trayIcon.Dispose();
    }

    /// <summary>
    /// Schreibt die aktuellen Messwerte in die Dashboard-Kacheln. Alle neun
    /// Kacheln stammen jetzt aus der echten Erfassung, im endgültigen
    /// Zahlenformat (Abschnitt 33: Messwerte mit zwei Nachkommastellen,
    /// Zähler ohne).
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

        LeftClicksValueText.Text = Formatting.Count(_clickTracker.LeftClicks);
        RightClicksValueText.Text = Formatting.Count(_clickTracker.RightClicks);
        MiddleClicksValueText.Text = Formatting.Count(_clickTracker.MiddleClicks);
        TotalClicksValueText.Text = Formatting.Count(_clickTracker.TotalClicks);

        KeystrokesValueText.Text = Formatting.Count(_keystrokeTracker.TotalKeystrokes);

        ScrollDistanceValueText.Text = Formatting.Distance(_scrollTracker.DistanceMeters);
        ScrollForwardValueText.Text = Formatting.Count(_scrollTracker.ForwardScrolls);
        ScrollBackwardValueText.Text = Formatting.Count(_scrollTracker.BackwardScrolls);
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
