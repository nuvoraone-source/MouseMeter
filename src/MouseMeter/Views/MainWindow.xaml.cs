using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MouseMeter.Infrastructure;
using MouseMeter.Infrastructure.Storage;

namespace MouseMeter.Views;

/// <summary>
/// Hauptfenster von MouseMeter: Kopfbereich, Zeitraum-Auswahl, Dashboard mit
/// den zehn Live-Kacheln, Diagrammfläche und Statusleiste.
///
/// Mausstrecke (v0.3.0), Geschwindigkeit, Durchschnitt und Rekord (v0.4.0),
/// Links-, Rechts-, Mittel- und Gesamtklicks (v0.5.0), Tastaturanschläge
/// (v0.6.0), Scrollrad-Ausschläge (v0.7.0) und deren Strecke (v0.7.1) werden
/// live erfasst. Seit v0.8.0 übersteht die heutige Statistik einen Neustart
/// von MouseMeter, und der persönliche Geschwindigkeitsrekord gilt dauerhaft.
/// Seit dieser Version (v0.9.0) werten Woche/Monat/Jahr die gespeicherten
/// Tagesdaten tatsächlich aus, statt nur den heutigen Tag zu wiederholen
/// (Gesamtkonzept Abschnitt 17-26). Beim Minimieren verschwindet MouseMeter
/// aus der Taskleiste und legt stattdessen ein Symbol in die Ablage neben
/// der Uhr.
/// </summary>
public partial class MainWindow : Window
{
    // Aktualisierungsintervall der Anzeige. Die Erfassung selbst laeuft ueber
    // den systemweiten Hook in Echtzeit - fuer die Oberflaeche reicht ein
    // deutlich selteneres Neuzeichnen, um die UI nicht unnoetig zu belasten
    // (Abschnitt 31, ressourcenschonender Betrieb). Dasselbe Intervall dient
    // als Messfenster fuer die aktuelle Geschwindigkeit.
    private static readonly TimeSpan DisplayRefreshInterval = TimeSpan.FromMilliseconds(200);

    // Wie oft der aktuelle Stand auf die Festplatte geschrieben wird. Selten
    // genug, um keine unnoetigen Schreibzugriffe zu erzeugen, haeufig genug,
    // um bei einem Absturz nur wenige Sekunden Daten zu verlieren.
    private static readonly TimeSpan PersistInterval = TimeSpan.FromSeconds(5);

    private readonly MouseDistanceTracker _distanceTracker = new();
    private readonly MouseClickTracker _clickTracker = new();
    private readonly KeystrokeTracker _keystrokeTracker = new();
    private readonly ScrollTracker _scrollTracker = new();
    private readonly TrayIconController _trayIcon;
    private readonly DispatcherTimer _displayTimer;
    private readonly DispatcherTimer _persistTimer;
    private readonly Stopwatch _sampleStopwatch = new();

    private readonly DataStore _dataStore = new();
    private readonly AppState _appState;
    private DateOnly _currentDay;

    // Momentaufnahme der Strecke beim letzten Neuzeichnen - Grundlage fuer
    // die aktuelle Geschwindigkeit (Distanz-Differenz je Messfenster).
    private double _lastSampleDistanceMeters;

    // Persönlicher Geschwindigkeitsrekord - gilt dauerhaft, nicht nur fuer
    // den aktuellen Tag oder die laufende Sitzung (Abschnitt 11).
    private double _recordSpeedMetersPerSecond;

    private DashboardPeriod _selectedPeriod = DashboardPeriod.Day;

    public MainWindow()
    {
        InitializeComponent();

        WindowTheme.ApplyDarkTitleBar(this);

        Title = $"{AppInfo.Name} {AppInfo.DisplayVersion}";
        VersionText.Text = AppInfo.DisplayVersion;

        _displayTimer = new DispatcherTimer { Interval = DisplayRefreshInterval };
        _displayTimer.Tick += (_, _) => RenderDashboard();

        _persistTimer = new DispatcherTimer { Interval = PersistInterval };
        _persistTimer.Tick += (_, _) => { HandleDayRollover(); SaveState(); };

        // Dasselbe Symbol wie das Anwendungsfenster - direkt aus der laufenden
        // EXE gelesen, kein separates Laden der Ressourcendatei nötig. Der
        // Controller übernimmt den Besitz und gibt das Icon-Handle erst beim
        // eigenen Dispose frei (ein "using" hier hätte es sofort zerstört).
        var appIcon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!)!;
        _trayIcon = new TrayIconController(this, appIcon);
        _trayIcon.RestoreRequested += OnTrayRestoreRequested;
        _trayIcon.ExitRequested += OnTrayExitRequested;

        // Gespeicherten Zustand laden und die Tracker damit auf den heutigen
        // Stand bringen, BEVOR sie unten in OnLoaded gestartet werden - so
        // läuft die heutige Statistik nach einem Neustart nahtlos weiter.
        _appState = _dataStore.Load();
        _currentDay = DateOnly.FromDateTime(DateTime.Now);
        if (_appState.Days.TryGetValue(DayKey(_currentDay), out DailyStats? today))
        {
            _distanceTracker.Seed(today.DistanceMeters, TimeSpan.FromSeconds(today.ActiveSeconds));
            _clickTracker.Seed(today.LeftClicks, today.RightClicks, today.MiddleClicks);
            _keystrokeTracker.Seed(today.Keystrokes);
            _scrollTracker.Seed(today.ScrollForward, today.ScrollBackward);
        }
        _lastSampleDistanceMeters = _distanceTracker.TotalDistanceMeters;
        _recordSpeedMetersPerSecond = _appState.AllTimeRecordSpeedMetersPerSecond;

        RenderDashboard();

        Loaded += OnLoaded;
        StateChanged += OnStateChanged;
        Closed += OnClosed;
    }

    private static string DayKey(DateOnly day) => day.ToString("yyyy-MM-dd");

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _distanceTracker.Start();
        _clickTracker.Start();
        _keystrokeTracker.Start();
        _scrollTracker.Start();
        _sampleStopwatch.Start();
        _displayTimer.Start();
        _persistTimer.Start();

        StatusText.Text = "Aktiv";
        StatusDot.Fill = (Brush)FindResource("StatusOkBrush");
    }

    /// <summary>
    /// Erkennt den Wechsel auf einen neuen Kalendertag (Gesamtkonzept
    /// Abschnitt 25) und schreibt in diesem Fall zunächst den bisherigen Tag
    /// abschließend fest, bevor alle Tageszähler automatisch auf Null
    /// zurückgesetzt werden. Der persönliche Rekord bleibt davon unberührt.
    /// </summary>
    private void HandleDayRollover()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (today == _currentDay)
        {
            return;
        }

        SaveState();

        _currentDay = today;
        _distanceTracker.ResetForNewDay();
        _clickTracker.ResetForNewDay();
        _keystrokeTracker.ResetForNewDay();
        _scrollTracker.ResetForNewDay();
        _lastSampleDistanceMeters = 0;
    }

    /// <summary>Momentaufnahme der heutigen Live-Werte im Speicherformat - dient sowohl der Anzeige als auch dem Sichern.</summary>
    private DailyStats CaptureLiveToday() => new()
    {
        DistanceMeters = _distanceTracker.TotalDistanceMeters,
        ActiveSeconds = _distanceTracker.Elapsed.TotalSeconds,
        LeftClicks = _clickTracker.LeftClicks,
        RightClicks = _clickTracker.RightClicks,
        MiddleClicks = _clickTracker.MiddleClicks,
        Keystrokes = _keystrokeTracker.TotalKeystrokes,
        ScrollForward = _scrollTracker.ForwardScrolls,
        ScrollBackward = _scrollTracker.BackwardScrolls,
    };

    /// <summary>Schreibt den aktuellen Stand des heutigen Tages und den Rekord auf die Festplatte.</summary>
    private void SaveState()
    {
        _appState.Days[DayKey(_currentDay)] = CaptureLiveToday();
        _appState.AllTimeRecordSpeedMetersPerSecond = _recordSpeedMetersPerSecond;

        _dataStore.Save(_appState);
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
        _persistTimer.Stop();

        // Letzten Stand noch sichern, damit beim naechsten Start nichts von
        // den Sekunden seit dem letzten periodischen Speichern fehlt.
        HandleDayRollover();
        SaveState();

        _distanceTracker.Dispose();
        _clickTracker.Dispose();
        _keystrokeTracker.Dispose();
        _scrollTracker.Dispose();
        _trayIcon.Dispose();
    }

    /// <summary>
    /// Schreibt die aktuellen Messwerte in die Dashboard-Kacheln. Bei "Tag"
    /// sind das die laufenden Live-Werte; bei Woche/Monat/Jahr die aus den
    /// gespeicherten Tagesdaten zusammengefassten Werte (Abschnitt 26). Im
    /// endgültigen Zahlenformat (Abschnitt 33: Messwerte mit zwei
    /// Nachkommastellen, Zähler ohne).
    /// </summary>
    private void RenderDashboard()
    {
        double totalDistanceToday = _distanceTracker.TotalDistanceMeters;

        // Aktuelle Geschwindigkeit: zurückgelegte Strecke seit dem letzten
        // Neuzeichnen, geteilt durch die dafür tatsächlich vergangene Zeit
        // (nicht das nominelle Intervall, das durch UI-Jitter leicht abweicht).
        // Läuft unabhängig von der Zeitraum-Auswahl immer mit, damit auch
        // beim Betrachten von Woche/Monat/Jahr weiter ein neuer Rekord
        // erkannt werden kann.
        double sampleSeconds = _sampleStopwatch.Elapsed.TotalSeconds;
        double currentSpeed = sampleSeconds > 0
            ? (totalDistanceToday - _lastSampleDistanceMeters) / sampleSeconds
            : 0;
        _lastSampleDistanceMeters = totalDistanceToday;
        _sampleStopwatch.Restart();

        _recordSpeedMetersPerSecond = Math.Max(_recordSpeedMetersPerSecond, currentSpeed);

        DailyStats display = PeriodAggregator.Aggregate(_appState, _currentDay, _selectedPeriod, CaptureLiveToday());
        double averageSpeed = display.ActiveSeconds > 0 ? display.DistanceMeters / display.ActiveSeconds : 0;
        bool isToday = _selectedPeriod == DashboardPeriod.Day;

        DistanceValueText.Text = Formatting.Distance(display.DistanceMeters);
        // Eine "aktuelle" Geschwindigkeit ergibt für einen zurückliegenden
        // Zeitraum keinen Sinn - nur für "Tag" wird hier ein Wert gezeigt.
        SpeedValueText.Text = isToday ? Formatting.Speed(currentSpeed) : "–";
        AverageValueText.Text = Formatting.Speed(averageSpeed);
        RecordValueText.Text = Formatting.Speed(_recordSpeedMetersPerSecond);

        LeftClicksValueText.Text = Formatting.Count(display.LeftClicks);
        RightClicksValueText.Text = Formatting.Count(display.RightClicks);
        MiddleClicksValueText.Text = Formatting.Count(display.MiddleClicks);
        TotalClicksValueText.Text = Formatting.Count(display.LeftClicks + display.RightClicks + display.MiddleClicks);

        KeystrokesValueText.Text = Formatting.Count(display.Keystrokes);

        ScrollDistanceValueText.Text = Formatting.Distance(ScrollTracker.ComputeDistance(display.ScrollForward + display.ScrollBackward));
        ScrollForwardValueText.Text = Formatting.Count(display.ScrollForward);
        ScrollBackwardValueText.Text = Formatting.Count(display.ScrollBackward);
    }

    private void OnPeriodChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { IsChecked: true } selected || ChartPlaceholderText is null)
        {
            return;
        }

        (_selectedPeriod, string periodLabel) = selected.Name switch
        {
            nameof(PeriodWeek) => (DashboardPeriod.Week, "die Woche"),
            nameof(PeriodMonth) => (DashboardPeriod.Month, "den Monat"),
            nameof(PeriodYear) => (DashboardPeriod.Year, "das Jahr"),
            _ => (DashboardPeriod.Day, "den Tag"),
        };

        var today = DateOnly.FromDateTime(DateTime.Now);
        PeriodRangeText.Text = _selectedPeriod == DashboardPeriod.Day
            ? string.Empty
            : FormatRange(PeriodAggregator.GetStart(_selectedPeriod, today), today);

        ChartPlaceholderText.Text = $"Diagramm für {periodLabel} folgt in einer der nächsten Versionen.";

        // Sofort mit den Werten des neu gewählten Zeitraums neuzeichnen, statt
        // auf den nächsten Anzeige-Tick zu warten.
        RenderDashboard();
    }

    private static string FormatRange(DateOnly start, DateOnly end) =>
        start == end
            ? start.ToString("dd.MM.yyyy")
            : $"{start:dd.MM.} – {end:dd.MM.yyyy}";

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        new SettingsWindow { Owner = this }.ShowDialog();
    }

    private void OnNuvoraClick(object sender, MouseButtonEventArgs e)
    {
        new ContactWindow { Owner = this }.ShowDialog();
    }
}
