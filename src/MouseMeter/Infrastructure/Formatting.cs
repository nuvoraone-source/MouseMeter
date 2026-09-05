using System.Globalization;

namespace MouseMeter.Infrastructure;

/// <summary>
/// Einheitliche Zahlenformatierung für das Dashboard.
///
/// Messwerte (Strecke, Geschwindigkeit) erhalten immer zwei Nachkommastellen,
/// Zähler (Klicks, Tastaturanschläge) grundsätzlich keine - siehe Gesamtkonzept
/// Abschnitt 33 ("0,00 m" gegenüber "12.458").
///
/// Format ist bewusst fest auf de-DE gesetzt (Komma als Dezimal-, Punkt als
/// Tausendertrennzeichen), unabhängig von der Systemregion des Rechners.
/// </summary>
public static class Formatting
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>Formatiert eine Strecke in Metern, z. B. "152,48".</summary>
    public static string Distance(double meters) => meters.ToString("N2", Culture);

    /// <summary>Formatiert eine Geschwindigkeit in m/s, z. B. "0,82".</summary>
    public static string Speed(double metersPerSecond) => metersPerSecond.ToString("N2", Culture);

    /// <summary>Formatiert einen Zähler ohne Nachkommastellen, z. B. "1.683".</summary>
    public static string Count(long value) => value.ToString("N0", Culture);
}
