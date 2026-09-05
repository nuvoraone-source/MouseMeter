namespace MouseMeter.Infrastructure;

/// <summary>
/// Zählt Scrollrad-Ausschläge getrennt nach Richtung: vorwärts (hoch) und
/// rückwärts (runter). Nutzt denselben systemweiten Hook-Mechanismus wie
/// <see cref="MouseClickTracker"/>, aber eine eigene Hook-Instanz.
/// </summary>
public sealed class ScrollTracker : IDisposable
{
    // Anders als bei der Mausstrecke (Abschnitt 8) gibt es für ein Scrollrad
    // keine über die Bildschirm-DPI exakt bestimmbare physische Strecke - ein
    // "Rastschritt" hat keine genormte Größe. Der Wert hier ist eine
    // plausible Schätzung: typischer Radumfang (~19 mm Durchmesser) geteilt
    // durch eine übliche Anzahl Raststufen pro Umdrehung (~24), gerundet auf
    // 2,5 mm je Ausschlag. Die "Scrollstrecke" ist damit eine Näherung, keine
    // gemessene Größe wie die Mausstrecke.
    private const double MetersPerNotch = 0.0025;

    private readonly GlobalMouseHook _hook = new();

    public long ForwardScrolls { get; private set; }
    public long BackwardScrolls { get; private set; }

    /// <summary>Summe aus vorwärts und rückwärts.</summary>
    public long TotalScrolls => ForwardScrolls + BackwardScrolls;

    /// <summary>
    /// Geschätzte "Scrollstrecke" in Metern - beide Richtungen zählen positiv,
    /// so wie auch Vor- und Zurückbewegen der Maus die Mausstrecke erhöht.
    /// </summary>
    public double DistanceMeters => TotalScrolls * MetersPerNotch;

    public ScrollTracker()
    {
        _hook.WheelScrolled += OnWheelScrolled;
    }

    /// <summary>Setzt die heutigen Zähler auf zuvor gespeicherte Werte, bevor <see cref="Start"/> aufgerufen wird.</summary>
    public void Seed(long forwardScrolls, long backwardScrolls)
    {
        ForwardScrolls = forwardScrolls;
        BackwardScrolls = backwardScrolls;
    }

    /// <summary>Setzt die heutigen Zähler zurück (Tageswechsel, Gesamtkonzept Abschnitt 25).</summary>
    public void ResetForNewDay()
    {
        ForwardScrolls = 0;
        BackwardScrolls = 0;
    }

    public void Start() => _hook.Start();

    private void OnWheelScrolled(int delta)
    {
        if (delta > 0)
        {
            ForwardScrolls++;
        }
        else if (delta < 0)
        {
            BackwardScrolls++;
        }
    }

    public void Dispose() => _hook.Dispose();
}
