using System.Windows;

namespace MouseMeter.Views;

/// <summary>Erklärt in Klartext, welche Daten MouseMeter erfasst und welche ausdrücklich nicht (Gesamtkonzept Abschnitt 30).</summary>
public partial class PrivacyWindow : Window
{
    public PrivacyWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
