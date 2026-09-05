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
/// Ab dieser Version (v0.3.0) wird die Mausbewegung tatsächlich erfasst und
/// die zurückgelegte Strecke live berechnet. Geschwindigkeit, Durchschnitt,
/// Rekord, Klicks und Tastaturanschläge folgen in den nächsten Versionen
/// (siehe Gesamtkonzept Abschnitt 36) und zeigen bis dahin weiterhin Null.
/// </summary>
public partial class MainWindow : Window
{
    // Aktualisierungsintervall der Anzeige. Die Erfassung selbst laeuft ueber
    // den systemweiten Hook in Echtzeit - fuer die Oberflaeche reicht ein
    // deutlich selteneres Neuzeichnen, um die UI nicht unnoetig zu belasten
    // (Abschnitt 31, ressourcenschonender Betrieb).
    private static readonly TimeSpan DisplayRefreshInterval = TimeSpan.FromMilliseconds(200);

    private readonly MouseDistanceTracker _distanceTracker = new();
    private readonly DispatcherTimer _displayTimer;

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
    /// Mausstrecke stammt aus der echten Erfassung, alle übrigen Kacheln
    /// zeigen noch Nullwerte, bis ihre Erfassung in einer späteren Version
    /// dazukommt - aber bereits im endgültigen Zahlenformat (Abschnitt 33).
    /// </summary>
    private void RenderDashboard()
    {
        DistanceValueText.Text = Formatting.Distance(_distanceTracker.TotalDistanceMeters);
        SpeedValueText.Text = Formatting.Speed(0);
        AverageValueText.Text = Formatting.Speed(0);
        RecordValueText.Text = Formatting.Speed(0);

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
