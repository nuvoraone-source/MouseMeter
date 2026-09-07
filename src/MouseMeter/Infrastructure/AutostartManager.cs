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

    /// <summary>
    /// Kommandozeilenargument, mit dem MouseMeter beim automatischen
    /// Windows-Start aufgerufen wird. Sagt der Anwendung, direkt in die
    /// Ablage zu starten, statt das Hauptfenster sichtbar zu zeigen - nur
    /// der Startbildschirm bleibt wie gewohnt kurz zu sehen.
    /// </summary>
    public const string StartMinimizedArg = "--autostart";

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
            key.SetValue(ValueName, ExpectedCommandLine());
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }

    /// <summary>
    /// Schreibt den Registry-Eintrag neu, falls Autostart aktiv ist, der
    /// hinterlegte Befehl aber nicht mehr dem aktuellen Pfad oder Argument
    /// entspricht (z. B. nach einem Update aus einer Version ohne
    /// <see cref="StartMinimizedArg"/>, oder nach Verschieben der EXE). So
    /// bleibt der einmalig gesetzte Autostart-Eintrag dauerhaft korrekt,
    /// ohne dass der Nutzer ihn in den Einstellungen neu setzen muss.
    /// </summary>
    public static void RepairIfEnabled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key?.GetValue(ValueName) is not string current)
        {
            return;
        }

        string expected = ExpectedCommandLine();
        if (current != expected)
        {
            key.SetValue(ValueName, expected);
        }
    }

    private static string ExpectedCommandLine()
    {
        string exePath = Environment.ProcessPath ?? Environment.GetCommandLineArgs()[0];
        return $"\"{exePath}\" {StartMinimizedArg}";
    }
}
