namespace MouseMeter.Infrastructure.Storage;

/// <summary>
/// Die gespeicherten Messwerte eines einzelnen Tages (Gesamtkonzept
/// Abschnitt 24). Bildet die Grundlage für die spätere Auswertung nach
/// Tag/Woche/Monat/Jahr - höhere Zeiträume werden aus mehreren
/// <see cref="DailyStats"/> berechnet, nicht separat erfasst (Abschnitt 26).
/// </summary>
public sealed class DailyStats
{
    public double DistanceMeters { get; set; }

    /// <summary>
    /// Tatsächliche Erfassungsdauer an diesem Tag in Sekunden - Grundlage für
    /// die Durchschnittsgeschwindigkeit (Gesamtstrecke ÷ Aktivzeit), auch
    /// über mehrere Programmstarts am selben Tag hinweg.
    /// </summary>
    public double ActiveSeconds { get; set; }

    public long LeftClicks { get; set; }
    public long RightClicks { get; set; }
    public long MiddleClicks { get; set; }
    public long Keystrokes { get; set; }
    public long ScrollForward { get; set; }
    public long ScrollBackward { get; set; }

    /// <summary>
    /// Auf die Stunde des Tages heruntergebrochene Werte (Index 0 = 0-1 Uhr
    /// usw.), seit v1.1.0 zusätzlich zu den Tagessummen oben erfasst - sie
    /// ermöglichen ein Diagramm für "Tag" mit derselben stundengenauen
    /// Auflösung, mit der die Kacheln ohnehin schon laufend aktualisiert
    /// werden. Ältere gespeicherte Tage ohne dieses Feld liefern beim Laden
    /// automatisch 24 Nullwerte statt eines Fehlers.
    /// </summary>
    public HourlyStats[] Hours { get; set; } = new HourlyStats[24];
}

/// <summary>Die Messwerte einer einzelnen Stunde eines Tages, gleiche Felder wie <see cref="DailyStats"/> ohne die Tagessumme selbst.</summary>
public struct HourlyStats
{
    public double DistanceMeters { get; set; }
    public double ActiveSeconds { get; set; }
    public long LeftClicks { get; set; }
    public long RightClicks { get; set; }
    public long MiddleClicks { get; set; }
    public long Keystrokes { get; set; }
    public long ScrollForward { get; set; }
    public long ScrollBackward { get; set; }
}
