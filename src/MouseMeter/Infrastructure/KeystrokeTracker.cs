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
    // Virtueller Tastencode der ESC-Taste (Windows Virtual-Key-Code 0x1B) -
    // die einzige Taste, deren Identität eigens gezählt wird (siehe
    // EscKeyPresses unten); alle anderen Tasten fließen ausschließlich in
    // die anonyme Gesamtzahl ein.
    private const int EscVirtualKeyCode = 0x1B;

    private readonly GlobalKeyboardHook _hook = new();
    private readonly HashSet<int> _pressedKeys = new();

    /// <summary>Seit Start dieser Sitzung insgesamt erfasste Tastaturanschläge.</summary>
    public long TotalKeystrokes { get; private set; }

    /// <summary>Davon speziell die ESC-Taste - eigens gezählt, zusätzlich zur Gesamtzahl oben.</summary>
    public long EscKeyPresses { get; private set; }

    public KeystrokeTracker()
    {
        _hook.KeyDown += OnKeyDown;
        _hook.KeyUp += OnKeyUp;
    }

    /// <summary>Setzt die heutigen Zähler auf zuvor gespeicherte Werte, bevor <see cref="Start"/> aufgerufen wird.</summary>
    public void Seed(long keystrokes, long escKeyPresses)
    {
        TotalKeystrokes = keystrokes;
        EscKeyPresses = escKeyPresses;
    }

    /// <summary>Setzt die heutigen Zähler zurück (Tageswechsel, Gesamtkonzept Abschnitt 25).</summary>
    public void ResetForNewDay()
    {
        TotalKeystrokes = 0;
        EscKeyPresses = 0;
    }

    public void Start() => _hook.Start();

    private void OnKeyDown(int vkCode)
    {
        // _pressedKeys enthält den Tastencode nur so lange, wie die Taste
        // physisch unten bleibt - dient ausschließlich der Wiederholungs-
        // Erkennung und wird nicht dauerhaft gespeichert oder ausgewertet.
        if (!_pressedKeys.Add(vkCode))
        {
            return;
        }

        TotalKeystrokes++;
        if (vkCode == EscVirtualKeyCode)
        {
            EscKeyPresses++;
        }
    }

    private void OnKeyUp(int vkCode) => _pressedKeys.Remove(vkCode);

    public void Dispose() => _hook.Dispose();
}
