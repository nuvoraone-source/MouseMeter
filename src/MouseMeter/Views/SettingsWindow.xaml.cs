using System.Windows;
using MouseMeter.Infrastructure;

namespace MouseMeter.Views;

/// <summary>
/// Einstellungsfenster: Autostart-Schalter, Datenschutzerklärung und
/// Kontaktweg. Tracking und Datenverwaltung folgen in einer der nächsten
/// Versionen (Gesamtkonzept Abschnitt 28).
/// </summary>
public partial class SettingsWindow : Window
{
    // Verhindert, dass das Setzen der Checkbox beim Öffnen (Zeile unten in
    // OnLoaded) selbst schon als Nutzeraktion behandelt und die Registry
    // unnötig angefasst wird.
    private bool _isInitializing = true;

    public SettingsWindow()
    {
        InitializeComponent();

        AutostartCheckBox.IsChecked = AutostartManager.IsEnabled();
        _isInitializing = false;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnAutostartToggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        try
        {
            AutostartManager.SetEnabled(AutostartCheckBox.IsChecked == true);
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show(
                this,
                "Der Autostart konnte nicht geändert werden (kein Zugriff auf die Registry).",
                "Autostart",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void OnPrivacyClick(object sender, RoutedEventArgs e)
    {
        new PrivacyWindow { Owner = this }.ShowDialog();
    }

    private void OnContactClick(object sender, RoutedEventArgs e)
    {
        new ContactWindow { Owner = this }.ShowDialog();
    }
}
