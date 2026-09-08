using System.Globalization;
using MouseMeter.Infrastructure.Storage;

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

    /// <summary>Formatiert eine Strecke in Kilometern, z. B. "1,25".</summary>
    public static string DistanceKm(double meters) => (meters / 1000).ToString("N2", Culture);

    /// <summary>Formatiert eine Geschwindigkeit in m/s, z. B. "0,82".</summary>
    public static string Speed(double metersPerSecond) => metersPerSecond.ToString("N2", Culture);

    /// <summary>Formatiert einen Zähler ohne Nachkommastellen, z. B. "1.683".</summary>
    public static string Count(long value) => value.ToString("N0", Culture);

    /// <summary>Formatiert einen Wert passend zur jeweiligen Diagramm-Kennzahl, inklusive Einheit.</summary>
    public static string MetricValue(ChartMetric metric, double value) => metric switch
    {
        ChartMetric.Distance => $"{Distance(value)} m",
        ChartMetric.Speed => $"{Speed(value)} m/s",
        _ => Count((long)Math.Round(value)),
    };

    /// <summary>
    /// Kurzform für sehr schmale Diagrammbalken (z. B. Monatsansicht mit bis
    /// zu 31 Balken): gerundet, ohne Tausenderpunkt, mit möglichst kurzer
    /// Einheit - der genaue Wert steht weiterhin im Tooltip. Ab 1000 m wird
    /// auf km umgestellt, damit sich die Beschriftung bei großen Strecken
    /// nicht mit den Nachbarbalken überlappt.
    /// </summary>
    public static string CompactMetricValue(ChartMetric metric, double value) => metric switch
    {
        ChartMetric.Distance when value >= 1000 => $"{(value / 1000).ToString("0.#", Culture)}km",
        ChartMetric.Distance => $"{Math.Round(value):0}m",
        ChartMetric.Speed => value.ToString("0.00", Culture),
        _ => Math.Round(value).ToString("0", Culture),
    };

    /// <summary>
    /// Ausführlicher Tooltip-Text für einen Diagrammwert: bei Strecke werden
    /// Meter und Kilometer übereinander angezeigt, sonst wie <see cref="MetricValue"/>.
    /// </summary>
    public static string TooltipMetricValue(ChartMetric metric, double value) => metric switch
    {
        ChartMetric.Distance => $"{Distance(value)} m\n{DistanceKm(value)} km",
        _ => MetricValue(metric, value),
    };
}
