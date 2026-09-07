using System.Diagnostics;
using System.Linq;
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
/// Seit v0.9.0 werten Woche/Monat/Jahr die gespeicherten Tagesdaten
/// tatsächlich aus. Seit v0.10.0 gibt es dazu ein echtes Balkendiagramm mit
/// wählbarer Kennzahl und sichtbaren Werten je Balken. Seit dieser Version
/// (v0.11.0) startet MouseMeter automatisch mit Windows (bis der Nutzer das
/// in den Einstellungen abschaltet) und erklärt dort auch, welche Daten
/// erfasst werden (Gesamtkonzept Abschnitt 5, 28, 30). Beim Minimieren
/// verschwindet MouseMeter aus der Taskleiste und legt stattdessen ein
/// Symbol in die Ablage neben der Uhr - seit v1.0.2 gilt das beim
/// automatischen Windows-Start von Anfang an, ohne dass das Fenster jemals
/// sichtbar aufblitzt (siehe <see cref="App"/> und
/// <see cref="Infrastructure.AutostartManager.StartMinimizedArg"/>). Seit
/// v1.1.0 werden die Tageswerte zusätzlich stundengenau erfasst
/// (<see cref="Infrastructure.Storage.DailyStats.Hours"/>), sodass auch
/// "Tag" ein echtes Balkendiagramm zeigt statt eines erklärenden
/// Hinweistextes.
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

    // Stundengenaue Aufschlüsselung des Tages (Abschnitt 5 des Screenshots:
    // Diagramm für "Tag"): die Tracker liefern nur kumulierte Tageswerte, ein
    // Stunden-Balken ergibt sich deshalb aus der Differenz zum Stand bei
    // Beginn der jeweiligen Stunde. _currentHour und die _hourStart*-Felder
    // halten diesen Bezugspunkt fest und werden bei jedem Stundenwechsel in
    // CaptureLiveToday() auf den dann aktuellen Stand verschoben.
    private int _currentHour;
    private double _hourStartDistanceMeters;
    private double _hourStartActiveSeconds;
    private long _hourStartLeftClicks;
    private long _hourStartRightClicks;
    private long _hourStartMiddleClicks;
    private long _hourStartKeystrokes;
    private long _hourStartScrollForward;
    private long _hourStartScrollBackward;

    // Momentaufnahme der Strecke beim letzten Neuzeichnen - Grundlage fuer
    // die aktuelle Geschwindigkeit (Distanz-Differenz je Messfenster).
    private double _lastSampleDistanceMeters;

    // Persönlicher Geschwindigkeitsrekord - gilt dauerhaft, nicht nur fuer
    // den aktuellen Tag oder die laufende Sitzung (Abschnitt 11).
    private double _recordSpeedMetersPerSecond;

    private DashboardPeriod _selectedPeriod = DashboardPeriod.Day;
    private ChartMetric _selectedMetric = ChartMetric.Distance;

    // Bei automatischem Windows-Start (siehe AutostartManager.StartMinimizedArg)
    // soll nur der Startbildschirm zu sehen sein - das Hauptfenster selbst
    // legt sich direkt in die Ablage, statt sichtbar zu erscheinen.
    private readonly bool _startMinimized;

    public MainWindow() : this(startMinimized: false)
    {
    }

    public MainWindow(bool startMinimized)
    {
        _startMinimized = startMinimized;

        InitializeComponent();

        if (_startMinimized)
        {
            // Vor dem Show()-Aufruf gesetzt, damit das Fenster nie sichtbar
            // aufblitzt - weder auf dem Desktop noch in der Taskleiste.
            WindowState = WindowState.Minimized;
            ShowInTaskbar = false;
        }

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

        // Beim allerersten Start Autostart automatisch aktivieren (Abschnitt
        // 5) - danach nie wieder anfassen, damit eine spätere Abwahl durch
        // den Nutzer in den Einstellungen bestehen bleibt.
        if (!_appState.AutostartInitialized)
        {
            try
            {
                AutostartManager.SetEnabled(true);
            }
            catch (UnauthorizedAccessException)
            {
                // Kein Zugriff auf die Registry - Autostart bleibt einfach aus.
            }
            _appState.AutostartInitialized = true;
        }
        else
        {
            // Ist Autostart bereits aktiv, aber der hinterlegte Registry-Eintrag
            // stammt noch aus einer älteren Version (z. B. ohne
            // StartMinimizedArg), reparieren wir ihn hier - sonst startet
            // MouseMeter beim automatischen Windows-Start dauerhaft sichtbar
            // statt direkt in der Ablage.
            try
            {
                AutostartManager.RepairIfEnabled();
            }
            catch (UnauthorizedAccessException)
            {
                // Kein Zugriff auf die Registry - nichts zu reparieren.
            }
        }

        _currentDay = DateOnly.FromDateTime(DateTime.Now);
        _currentHour = DateTime.Now.Hour;
        if (_appState.Days.TryGetValue(DayKey(_currentDay), out DailyStats? today))
        {
            _distanceTracker.Seed(today.DistanceMeters, TimeSpan.FromSeconds(today.ActiveSeconds));
            _clickTracker.Seed(today.LeftClicks, today.RightClicks, today.MiddleClicks);
            _keystrokeTracker.Seed(today.Keystrokes);
            _scrollTracker.Seed(today.ScrollForward, today.ScrollBackward);

            // Bezugspunkt für die laufende Stunde wiederherstellen: die
            // bereits für sie gespeicherte Teilsumme vom kumulierten
            // Tagesstand abziehen, damit ihr Balken nach einem Neustart
            // mitten in der Stunde nahtlos weiterläuft statt bei Null oder
            // beim vollen Tagesstand neu anzusetzen.
            HourlyStats savedHour = today.Hours[_currentHour];
            _hourStartDistanceMeters = today.DistanceMeters - savedHour.DistanceMeters;
            _hourStartActiveSeconds = today.ActiveSeconds - savedHour.ActiveSeconds;
            _hourStartLeftClicks = today.LeftClicks - savedHour.LeftClicks;
            _hourStartRightClicks = today.RightClicks - savedHour.RightClicks;
            _hourStartMiddleClicks = today.MiddleClicks - savedHour.MiddleClicks;
            _hourStartKeystrokes = today.Keystrokes - savedHour.Keystrokes;
            _hourStartScrollForward = today.ScrollForward - savedHour.ScrollForward;
            _hourStartScrollBackward = today.ScrollBackward - savedHour.ScrollBackward;
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

        if (_startMinimized)
        {
            MinimizeToTray();
        }
    }

    /// <summary>Blendet das Fenster aus, entfernt es aus der Taskleiste und zeigt stattdessen das Ablage-Symbol.</summary>
    private void MinimizeToTray()
    {
        Hide();
        ShowInTaskbar = false;
        _trayIcon.Show();
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

        _currentHour = DateTime.Now.Hour;
        _hourStartDistanceMeters = 0;
        _hourStartActiveSeconds = 0;
        _hourStartLeftClicks = 0;
        _hourStartRightClicks = 0;
        _hourStartMiddleClicks = 0;
        _hourStartKeystrokes = 0;
        _hourStartScrollForward = 0;
        _hourStartScrollBackward = 0;
    }

    /// <summary>
    /// Momentaufnahme der heutigen Live-Werte im Speicherformat - dient
    /// sowohl der Anzeige als auch dem Sichern. Schreibt dabei auch den
    /// Stunden-Balken der gerade laufenden Stunde fort (siehe die
    /// _hourStart*-Felder) und übernimmt die bereits abgeschlossenen Stunden
    /// unverändert aus dem zuletzt gesicherten Stand.
    /// </summary>
    private DailyStats CaptureLiveToday()
    {
        var stats = new DailyStats
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

        stats.Hours = _appState.Days.TryGetValue(DayKey(_currentDay), out DailyStats? existing)
            ? (HourlyStats[])existing.Hours.Clone()
            : new HourlyStats[24];

        int hour = DateTime.Now.Hour;
        if (hour != _currentHour)
        {
            // Stunde gewechselt - die jetzt beginnende Stunde startet bei
            // Null, ihr Bezugspunkt ist also einfach der aktuelle Tagesstand.
            _hourStartDistanceMeters = stats.DistanceMeters;
            _hourStartActiveSeconds = stats.ActiveSeconds;
            _hourStartLeftClicks = stats.LeftClicks;
            _hourStartRightClicks = stats.RightClicks;
            _hourStartMiddleClicks = stats.MiddleClicks;
            _hourStartKeystrokes = stats.Keystrokes;
            _hourStartScrollForward = stats.ScrollForward;
            _hourStartScrollBackward = stats.ScrollBackward;
            _currentHour = hour;
        }

        stats.Hours[_currentHour] = new HourlyStats
        {
            DistanceMeters = stats.DistanceMeters - _hourStartDistanceMeters,
            ActiveSeconds = stats.ActiveSeconds - _hourStartActiveSeconds,
            LeftClicks = stats.LeftClicks - _hourStartLeftClicks,
            RightClicks = stats.RightClicks - _hourStartRightClicks,
            MiddleClicks = stats.MiddleClicks - _hourStartMiddleClicks,
            Keystrokes = stats.Keystrokes - _hourStartKeystrokes,
            ScrollForward = stats.ScrollForward - _hourStartScrollForward,
            ScrollBackward = stats.ScrollBackward - _hourStartScrollBackward,
        };

        return stats;
    }

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
            MinimizeToTray();
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

        RenderChart();
    }

    /// <summary>
    /// Zeichnet das Balkendiagramm neu: ein Balken pro Stunde ("Tag"), pro Tag
    /// (Woche/Monat) bzw. pro Monat ("Jahr") - siehe <see cref="ChartSeriesBuilder"/>.
    /// </summary>
    private void RenderChart()
    {
        // Seit v1.1.0 gibt es für jeden Zeitraum ein echtes Diagramm - die
        // beiden Elemente starten in XAML eingeklappt (Grundzustand vor
        // dieser Version) und werden hier dauerhaft sichtbar geschaltet.
        MetricSelector.Visibility = Visibility.Visible;
        ChartBarsPanel.Visibility = Visibility.Visible;

        var today = DateOnly.FromDateTime(DateTime.Now);
        List<ChartDataPoint> series = ChartSeriesBuilder.Build(_appState, today, _selectedPeriod, CaptureLiveToday(), _selectedMetric);

        double max = series.Count > 0 ? series.Max(p => p.Value) : 0;
        if (max <= 0)
        {
            max = 1; // vermeidet Division durch Null, wenn noch nichts erfasst wurde
        }

        ChartBarsPanel.Columns = Math.Max(1, series.Count);
        ChartBarsPanel.Children.Clear();

        const double chartHeight = 92;

        foreach (ChartDataPoint point in series)
        {
            double barHeight = Math.Max(2, point.Value / max * chartHeight);

            var bar = new Border
            {
                Height = barHeight,
                CornerRadius = new CornerRadius(3, 3, 0, 0),
                Background = (Brush)FindResource("AccentGradientBrush"),
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(2, 0, 2, 0),
                ToolTip = point.Tooltip,
            };

            var barHost = new Grid { Height = chartHeight, VerticalAlignment = VerticalAlignment.Bottom };
            barHost.Children.Add(bar);

            var label = new TextBlock
            {
                Text = point.Label,
                FontFamily = (FontFamily)FindResource("UiFont"),
                FontSize = 10,
                Foreground = (Brush)FindResource("TextMutedBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0),
            };

            var cell = new StackPanel();

            // Bei Woche/Jahr (bis zu 12 Balken) passt der genaue Wert zu
            // jedem Balken. Beim Monat (bis zu 31 schmale Balken) würde das
            // bei jedem Balken überlappen - dort wird der Wert deshalb nur
            // an denselben Tagen gezeigt, die ohnehin schon eine Tageszahl
            // darunter tragen (siehe ChartSeriesBuilder), dafür in kompakter
            // Kurzform. Die übrigen Balken bleiben über den Tooltip abrufbar.
            bool isNarrow = series.Count > 12;
            bool showValue = !isNarrow || !string.IsNullOrEmpty(point.Label);

            if (showValue)
            {
                cell.Children.Add(new TextBlock
                {
                    Text = isNarrow
                        ? Formatting.CompactMetricValue(_selectedMetric, point.Value)
                        : Formatting.MetricValue(_selectedMetric, point.Value),
                    FontFamily = (FontFamily)FindResource("UiFont"),
                    FontSize = isNarrow ? 8 : 9,
                    Foreground = (Brush)FindResource("TextPrimaryBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 3),
                });
            }
            else
            {
                // Platzhalter, damit alle Balken trotzdem gleich hoch anfangen.
                cell.Children.Add(new TextBlock { FontSize = isNarrow ? 8 : 9, Text = " " });
            }

            cell.Children.Add(barHost);
            cell.Children.Add(label);

            ChartBarsPanel.Children.Add(cell);
        }
    }

    private void OnMetricChanged(object sender, RoutedEventArgs e)
    {
        // _appState ist erst nach InitializeComponent() gesetzt - das von der
        // XAML-Voreinstellung "IsChecked=True" ausgelöste allererste Checked-
        // Ereignis muss deshalb ignoriert werden, bevor RenderChart() etwas
        // liest, das noch gar nicht existiert.
        if (_appState is null || sender is not RadioButton { IsChecked: true } selected)
        {
            return;
        }

        _selectedMetric = selected.Name switch
        {
            nameof(MetricClicks) => ChartMetric.Clicks,
            nameof(MetricKeystrokes) => ChartMetric.Keystrokes,
            nameof(MetricSpeed) => ChartMetric.Speed,
            _ => ChartMetric.Distance,
        };

        RenderChart();
    }

    private void OnPeriodChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { IsChecked: true } selected || ChartBarsPanel is null)
        {
            return;
        }

        _selectedPeriod = selected.Name switch
        {
            nameof(PeriodWeek) => DashboardPeriod.Week,
            nameof(PeriodMonth) => DashboardPeriod.Month,
            nameof(PeriodYear) => DashboardPeriod.Year,
            _ => DashboardPeriod.Day,
        };

        var today = DateOnly.FromDateTime(DateTime.Now);
        PeriodRangeText.Text = _selectedPeriod == DashboardPeriod.Day
            ? string.Empty
            : FormatRange(PeriodAggregator.GetStart(_selectedPeriod, today), today);

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
        new SettingsWindow(_dataStore.FilePath, _appState) { Owner = this }.ShowDialog();
    }

    private void OnNuvoraClick(object sender, MouseButtonEventArgs e)
    {
        new ContactWindow { Owner = this }.ShowDialog();
    }
}
