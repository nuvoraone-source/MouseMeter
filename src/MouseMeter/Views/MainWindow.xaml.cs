using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MouseMeter.Infrastructure;

namespace MouseMeter.Views;

/// <summary>
/// Hauptfenster von MouseMeter: Kopfbereich, Zeitraum-Auswahl, Dashboard mit
/// den neun Live-Kacheln, Diagrammfläche und Statusleiste.
///
/// Die Kacheln zeigen in dieser Version ausschließlich Nullwerte - die
/// eigentliche Erfassung von Mausbewegung, Klicks und Tastatur folgt ab
/// v0.3.0 (siehe Gesamtkonzept Abschnitt 36).
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        WindowTheme.ApplyDarkTitleBar(this);

        Title = $"{AppInfo.Name} {AppInfo.DisplayVersion}";
        VersionText.Text = AppInfo.DisplayVersion;

        // Die Erfassung selbst folgt ab v0.3.0; bis dahin meldet der Status ehrlich "Inaktiv".
        StatusText.Text = "Inaktiv";

        RenderDashboard();
    }

    /// <summary>
    /// Schreibt die aktuellen Messwerte in die Dashboard-Kacheln.
    /// Solange keine echte Erfassung existiert, sind das durchgehend Nullwerte -
    /// aber bereits im endgültigen Zahlenformat (Abschnitt 33 des Konzepts).
    /// </summary>
    private void RenderDashboard()
    {
        DistanceValueText.Text = Formatting.Distance(0);
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
