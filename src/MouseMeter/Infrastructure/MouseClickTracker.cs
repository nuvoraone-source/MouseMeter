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
