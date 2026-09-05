using Microsoft.Win32;

namespace MouseMeter.Infrastructure;

/// <summary>
/// Verwaltet den automatischen Start von MouseMeter bei der Windows-Anmeldung
/// (Gesamtkonzept Abschnitt 5) über den Standard-Autostart-Schlüssel im
/// Benutzerprofil - keine Adminrechte nötig, funktioniert auch portabel.
/// </summary>
public static class AutostartManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "MouseMeter";

    public static bool IsEnabled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) is not null;
    }

    public static void SetEnabled(bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        if (enabled)
        {
            string exePath = Environment.ProcessPath ?? Environment.GetCommandLineArgs()[0];
            key.SetValue(ValueName, $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
