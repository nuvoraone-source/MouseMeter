using System.Globalization;
using System.IO;
using System.Text;

namespace MouseMeter.Infrastructure.Storage;

/// <summary>
/// Exportiert die gespeicherten Tageswerte (<see cref="AppState.Days"/>) als
/// CSV-Datei - eigene Sicherung und Weiterverarbeitung der Daten durch den
/// Nutzer, unabhängig vom internen JSON-Format.
/// </summary>
public static class CsvExporter
{
    public static void Export(AppState state, string filePath)
    {
        var csv = new StringBuilder();
        csv.AppendLine(
            "Datum;Distanz (m);Aktivzeit (s);Klicks links;Klicks rechts;Klicks mitte;Tastenanschläge;Scroll vorwärts;Scroll rückwärts");

        foreach ((string day, DailyStats stats) in state.Days.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            csv.AppendLine(string.Join(';',
                day,
                stats.DistanceMeters.ToString(CultureInfo.InvariantCulture),
                stats.ActiveSeconds.ToString(CultureInfo.InvariantCulture),
                stats.LeftClicks,
                stats.RightClicks,
                stats.MiddleClicks,
                stats.Keystrokes,
                stats.ScrollForward,
                stats.ScrollBackward));
        }

        File.WriteAllText(filePath, csv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }
}
