namespace MouseMeter.Infrastructure;

/// <summary>
/// Zählt Mausklicks nach Taste getrennt (Gesamtkonzept Abschnitt 12-15:
/// Links-, Rechts-, Mittelklicks und deren Summe). Nutzt denselben
/// systemweiten Hook-Mechanismus wie <see cref="MouseDistanceTracker"/>,
/// aber eine eigene Hook-Instanz - beide Erfassungen bleiben so unabhängig
/// voneinander und beeinflussen sich nicht gegenseitig.
/// </summary>
public sealed class MouseClickTracker : IDisposable
{
    private readonly GlobalMouseHook _hook = new();

    public long LeftClicks { get; private set; }
    public long RightClicks { get; private set; }
    public long MiddleClicks { get; private set; }

    /// <summary>Summe aus Links-, Rechts- und Mittelklicks.</summary>
    public long TotalClicks => LeftClicks + RightClicks + MiddleClicks;

    public MouseClickTracker()
    {
        _hook.ButtonDown += OnButtonDown;
    }

    /// <summary>Setzt die heutigen Klickzähler auf zuvor gespeicherte Werte, bevor <see cref="Start"/> aufgerufen wird.</summary>
    public void Seed(long leftClicks, long rightClicks, long middleClicks)
    {
        LeftClicks = leftClicks;
        RightClicks = rightClicks;
        MiddleClicks = middleClicks;
    }

    /// <summary>Setzt die heutigen Klickzähler zurück (Tageswechsel, Gesamtkonzept Abschnitt 25).</summary>
    public void ResetForNewDay()
    {
        LeftClicks = 0;
        RightClicks = 0;
        MiddleClicks = 0;
    }

    public void Start() => _hook.Start();

    private void OnButtonDown(MouseButtonKind button)
    {
        switch (button)
        {
            case MouseButtonKind.Left:
                LeftClicks++;
                break;
            case MouseButtonKind.Right:
                RightClicks++;
                break;
            case MouseButtonKind.Middle:
                MiddleClicks++;
                break;
        }
    }

    public void Dispose() => _hook.Dispose();
}
