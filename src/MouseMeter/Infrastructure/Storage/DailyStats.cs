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
}
