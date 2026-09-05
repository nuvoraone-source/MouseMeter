namespace MouseMeter.Infrastructure;

/// <summary>
/// Zählt Tastaturanschläge (Gesamtkonzept Abschnitt 16) - ausschließlich die
/// Anzahl, niemals welche Taste oder welcher Text. Windows meldet für eine
/// gehaltene Taste laufend weitere Tastendruck-Ereignisse (Wiederholung);
/// die wird hier herausgerechnet, indem ein Tastencode erst nach dem
/// Loslassen erneut als neuer Anschlag zählt.
/// </summary>
public sealed class KeystrokeTracker : IDisposable
{
    private readonly GlobalKeyboardHook _hook = new();
    private readonly HashSet<int> _pressedKeys = new();

    /// <summary>Seit Start dieser Sitzung insgesamt erfasste Tastaturanschläge.</summary>
    public long TotalKeystrokes { get; private set; }

    public KeystrokeTracker()
    {
        _hook.KeyDown += OnKeyDown;
        _hook.KeyUp += OnKeyUp;
    }

    /// <summary>Setzt den heutigen Zähler auf einen zuvor gespeicherten Wert, bevor <see cref="Start"/> aufgerufen wird.</summary>
    public void Seed(long keystrokes) => TotalKeystrokes = keystrokes;

    /// <summary>Setzt den heutigen Zähler zurück (Tageswechsel, Gesamtkonzept Abschnitt 25).</summary>
    public void ResetForNewDay() => TotalKeystrokes = 0;

    public void Start() => _hook.Start();

    private void OnKeyDown(int vkCode)
    {
        // _pressedKeys enthält den Tastencode nur so lange, wie die Taste
        // physisch unten bleibt - dient ausschließlich der Wiederholungs-
        // Erkennung und wird nicht dauerhaft gespeichert oder ausgewertet.
        if (_pressedKeys.Add(vkCode))
        {
            TotalKeystrokes++;
        }
    }

    private void OnKeyUp(int vkCode) => _pressedKeys.Remove(vkCode);

    public void Dispose() => _hook.Dispose();
}
