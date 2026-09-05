namespace MouseMeter.Infrastructure;

/// <summary>
/// Zählt Scrollrad-Ausschläge getrennt nach Richtung: vorwärts (hoch) und
/// rückwärts (runter). Nutzt denselben systemweiten Hook-Mechanismus wie
/// <see cref="MouseClickTracker"/>, aber eine eigene Hook-Instanz.
/// </summary>
public sealed class ScrollTracker : IDisposable
{
    private readonly GlobalMouseHook _hook = new();

    public long ForwardScrolls { get; private set; }
    public long BackwardScrolls { get; private set; }

    /// <summary>Summe aus vorwärts und rückwärts.</summary>
    public long TotalScrolls => ForwardScrolls + BackwardScrolls;

    public ScrollTracker()
    {
        _hook.WheelScrolled += OnWheelScrolled;
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
