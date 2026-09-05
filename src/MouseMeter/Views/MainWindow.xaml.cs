using System.Windows;
using MouseMeter.Infrastructure;

namespace MouseMeter.Views;

/// <summary>
/// Hauptfenster von MouseMeter. In dieser Version das Grundgerüst:
/// Kopfbereich, Inhaltsflaeche und Statusleiste.
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
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            this,
            "Die Einstellungen werden in einer späteren Version ergänzt.",
            "Einstellungen",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
