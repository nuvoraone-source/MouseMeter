using System.Diagnostics;
using System.Windows;

namespace MouseMeter.Views;

/// <summary>
/// Kleines Kontakt-Fenster: zeigt die Support-Adresse von nuvora und bietet
/// an, sie zu kopieren oder das Standard-Mailprogramm mit einer vorbereiteten
/// Nachricht zu öffnen. MouseMeter versendet dabei selbst nichts - das
/// Mailprogramm öffnet nur einen Entwurf, den der Benutzer noch bestätigen muss.
/// </summary>
public partial class ContactWindow : Window
{
    private const string SupportEmail = "nuvoraone@gmx.de";

    public ContactWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnCopyClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(SupportEmail);
            CopyButton.Content = "Kopiert";
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // Die Zwischenablage kann in seltenen Faellen kurzzeitig blockiert sein.
        }
    }

    private void OnOpenMailClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var info = new ProcessStartInfo
            {
                FileName = $"mailto:{SupportEmail}?subject=MouseMeter",
                UseShellExecute = true,
            };
            Process.Start(info);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Kein Standard-Mailprogramm hinterlegt - die Adresse steht bereits sichtbar da.
        }
    }
}
