using System.Windows;

namespace MouseMeter.Views;

/// <summary>
/// Einstellungsfenster. In dieser Version nur mit dem Kontaktweg zu nuvora
/// gefüllt - Tracking, Autostart und Datenverwaltung folgen mit v0.10.0
/// (siehe Gesamtkonzept Abschnitt 28).
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnContactClick(object sender, RoutedEventArgs e)
    {
        new ContactWindow { Owner = this }.ShowDialog();
    }
}
