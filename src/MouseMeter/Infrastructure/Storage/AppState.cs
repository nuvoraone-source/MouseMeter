namespace MouseMeter.Infrastructure.Storage;

/// <summary>
/// Der gesamte dauerhaft gespeicherte Zustand von MouseMeter: die
/// Tagesstatistiken (Schlüssel im Format "yyyy-MM-dd") sowie der
/// sitzungsübergreifende persönliche Geschwindigkeitsrekord.
///
/// Der Rekord gilt bewusst nicht pro Tag, sondern für immer - siehe
/// Gesamtkonzept Abschnitt 11: "Der Wert soll [...] als persönlicher
/// Geschwindigkeitsrekord gespeichert werden."
/// </summary>
public sealed class AppState
{
    public double AllTimeRecordSpeedMetersPerSecond { get; set; }

    public Dictionary<string, DailyStats> Days { get; set; } = new();

    /// <summary>
    /// Ob der automatische Windows-Start beim allerersten Programmstart schon
    /// eingerichtet wurde. MouseMeter aktiviert den Autostart einmalig von
    /// selbst (Gesamtkonzept Abschnitt 5); die spätere Wahl des Nutzers in
    /// den Einstellungen wird danach nicht mehr überschrieben.
    /// </summary>
    public bool AutostartInitialized { get; set; }

    /// <summary>
    /// Ob MouseMeter neue Versionen automatisch im Hintergrund von GitHub
    /// herunterlädt und installiert (Einstellungen &gt; Updates). Standardmäßig
    /// aktiviert; der Nutzer kann es jederzeit abschalten - siehe <see cref="Infrastructure.UpdateManager"/>.
    /// </summary>
    public bool AutoUpdateEnabled { get; set; } = true;

    /// <summary>Zeitpunkt (UTC) der letzten Update-Prüfung, um nicht bei jedem Start erneut zu prüfen.</summary>
    public DateTime LastUpdateCheckUtc { get; set; }
}
