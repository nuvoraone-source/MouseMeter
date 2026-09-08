using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using MouseMeter.Infrastructure;
using MouseMeter.Infrastructure.Storage;

namespace MouseMeter.Views;

/// <summary>
/// Einstellungsfenster: Autostart-Schalter, Speicherort der Daten,
/// Datenschutzerklärung und Kontaktweg. Weitergehende Tracking- und
/// Datenverwaltungsoptionen (Export, Aufbewahrungsdauer) folgen in einer der
/// nächsten Versionen (Gesamtkonzept Abschnitt 28).
/// </summary>
public partial class SettingsWindow : Window
{
    // Verhindert, dass das Setzen der Checkbox beim Öffnen (Zeile unten in
    // OnLoaded) selbst schon als Nutzeraktion behandelt und die Registry
    // unnötig angefasst wird.
    private bool _isInitializing = true;

    private readonly string _dataFilePath;
    private readonly AppState _appState;

    public SettingsWindow(string dataFilePath, AppState appState)
    {
        _dataFilePath = dataFilePath;
        _appState = appState;

        InitializeComponent();

        AutostartCheckBox.IsChecked = AutostartManager.IsEnabled();
        AutoUpdateCheckBox.IsChecked = _appState.AutoUpdateEnabled;
        _isInitializing = false;

        DataFilePathText.Text = _dataFilePath;
        DataFilePathText.ToolTip = _dataFilePath;
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

    private void OnAutoUpdateToggled(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        // Wird zusammen mit den übrigen Daten vom periodischen Speicher-Timer
        // des Hauptfensters weggeschrieben (dieselbe AppState-Instanz).
        _appState.AutoUpdateEnabled = AutoUpdateCheckBox.IsChecked == true;
    }

    private void OnOpenDataFolderClick(object sender, RoutedEventArgs e)
    {
        try
        {
            // "/select," markiert die Datei im geöffneten Explorer-Fenster
            // direkt, statt nur den Ordner zu öffnen - so findet man sie auf
            // Anhieb, auch wenn dort noch weitere Dateien liegen.
            Process.Start("explorer.exe", $"/select,\"{_dataFilePath}\"");
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            MessageBox.Show(
                this,
                $"Der Ordner konnte nicht geöffnet werden.\n\nPfad: {_dataFilePath}",
                "Daten",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void OnExportCsvClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Daten als CSV exportieren",
            Filter = "CSV-Datei (*.csv)|*.csv",
            FileName = $"mousemeter-export-{DateTime.Now:yyyy-MM-dd}.csv",
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            CsvExporter.Export(_appState, dialog.FileName);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                this,
                $"Der Export ist fehlgeschlagen.\n\nPfad: {dialog.FileName}",
                "Export",
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
